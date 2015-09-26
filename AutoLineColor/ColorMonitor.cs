using System;
using System.Threading;
using AutoLineColor.Coloring;
using AutoLineColor.Naming;
using ColossalFramework;
using ColossalFramework.Plugins;
using ICities;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AutoLineColor
{
    public class ColorMonitor : ThreadingExtensionBase
    {
        private static DateTimeOffset _nextUpdateTime = DateTimeOffset.Now;
        private bool _initialized;
        private IColorStrategy _colorStrategy;
        private INamingStrategy _namingStrategy;
        private List<Color32> _usedColors;
        private Configuration _config;
        private static Console logger = Console.Instance;

        public override void OnCreated(IThreading threading)
        {
            logger.Message("===============================");
            logger.Message("Initializing auto color monitor");
            logger.Message("Initializing colors");
            RandomColor.Initialize();
            CategorisedColor.Initialize();
            GenericNames.Initialize();

            logger.Message("Loading current config");
            _config = Configuration.Instance;
            _colorStrategy = SetColorStrategy(_config.ColorStrategy);
            _namingStrategy = SetNamingStrategy(_config.NamingStrategy);
            _usedColors = new List<Color32>();

            logger.Message("Found color strategy of " + _config.ColorStrategy);
            logger.Message("Found naming strategy of " + _config.NamingStrategy);

            _initialized = true;

            logger.Message("done creating");
            base.OnCreated(threading);
        }

        private static INamingStrategy SetNamingStrategy(NamingStrategy namingStrategy)
        {
            logger.Message("Naming Strategy: " + namingStrategy.ToString());
            switch (namingStrategy)
            {
                case NamingStrategy.None:
                    return new NoNamingStrategy();
                case NamingStrategy.Districts:
                    return new DistrictNamingStrategy();
                case NamingStrategy.London:
                    return new LondonNamingStrategy();
                default:
                    logger.Error("unknown naming strategy");
                    return new NoNamingStrategy();
            }
        }

        private IColorStrategy SetColorStrategy(ColorStrategy colorStrategy)
        {
            logger.Message("Color Strategy: " + colorStrategy.ToString());
            switch (colorStrategy)
            {
                case ColorStrategy.RandomHue:
                    return new RandomHueStrategy();
                case ColorStrategy.RandomColor:
                    return new RandomColorStrategy();
                case ColorStrategy.CategorisedColor:
                    return new CategorisedColorStrategy();
                default:
                    logger.Error("unknown color strategy");
                    return new RandomHueStrategy();
            }
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {   //Digest changes
            if (_config.UndigestedChanges == true) {
                logger.Message("Applying undigested changes");
                _colorStrategy = SetColorStrategy(_config.ColorStrategy);
                _namingStrategy = SetNamingStrategy(_config.NamingStrategy);
                _config.UndigestedChanges = false;
            }

            if (_initialized == false)
                return;

            // try and limit how often we are scanning for lines. this ain't that important
            if (_nextUpdateTime >= DateTimeOffset.Now)
                return;

            var theTransportManager = Singleton<TransportManager>.instance;
            var lines = theTransportManager.m_lines.m_buffer;

            try
            {
                _nextUpdateTime = DateTimeOffset.Now.AddSeconds(10);
                _usedColors = lines.Where(l => l.IsActive()).Select(l => l.m_color).ToList();

                for (ushort i = 0; i < theTransportManager.m_lines.m_buffer.Length - 1; i++)
                {
                    var transportLine = lines[i];
                    //logger.Message(string.Format("Starting on line {0}", i));

                    if (transportLine.m_flags == TransportLine.Flags.None)
                        continue;

                    if (!transportLine.Complete)
                        continue;

                    // only worry about fully created lines 
                    if (!transportLine.IsActive() || transportLine.HasCustomName() || !transportLine.m_color.IsDefaultColor())
                        continue;

                    logger.Message(string.Format("Working on line {0}", i));

                    var instanceID = new InstanceID();
                    var lineName = theTransportManager.GetLineName(i);
                    var newName = _namingStrategy.GetName(transportLine);

                    if (!transportLine.HasCustomColor() || transportLine.m_color.IsDefaultColor())
                    {
                        var color = _colorStrategy.GetColor(transportLine, _usedColors);
                        
                        logger.Message(string.Format("About to change line color to {0}", color));
                        transportLine.m_color = color;
                        transportLine.m_flags |= TransportLine.Flags.CustomColor;
                        theTransportManager.SetLineColor(i, color);

                        logger.Message(string.Format("Changed line color. '{0}' {1} -> {2}", lineName, theTransportManager.GetLineColor(i), color));
                    }

                    if (string.IsNullOrEmpty(newName) == false &&
                        transportLine.HasCustomName() == false) {
                        logger.Message(string.Format("About to rename line to {0}", newName));
                        
                        transportLine.m_flags |= TransportLine.Flags.CustomName;
                        theTransportManager.SetLineName(i, newName);

                        logger.Message(string.Format("Renamed Line '{0}' -> '{1}'", lineName, newName));
                    }

                    logger.Message(string.Format("Line is now {0} and {1}", 
                        theTransportManager.GetLineName(i),
                        theTransportManager.GetLineColor(i)));

                    lines[i] = transportLine;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
            finally
            {
                Monitor.Exit(Monitor.TryEnter(lines));
            }

        }
    }

    internal static class LineExtensions
    {
        private static Color32 _blackColor = new Color32(0, 0, 0, 0);
        private static Color32 _defaultBusColor = new Color32(44,142,191,255);
        private static Color32 _defaultMetroColor = new Color32(0,184,0,255);
        private static Color32 _defaultTrainColor = new Color32(219,86,0,255);

        public static bool IsDefaultColor(this Color32 color)
        {
            return (color.IsColorEqual(_blackColor) ||
                color.IsColorEqual(_defaultBusColor) ||
                color.IsColorEqual(_defaultMetroColor) ||
                color.IsColorEqual(_defaultTrainColor));
        }

        public static bool IsColorEqual(this Color32 color1, Color32 color2)
        {
            return (color1.r == color2.r && color1.g == color2.g && color1.b == color2.b && color1.a == color2.a);
        }

        public static bool IsActive(this TransportLine transportLine)
        {
            if ((transportLine.m_flags & (TransportLine.Flags.Created | TransportLine.Flags.Hidden)) == 0)
                return false;

            // stations are marked with this flag
            if ((transportLine.m_flags & TransportLine.Flags.Temporary) == TransportLine.Flags.Temporary)
                return false;

            return true;
        }

        public static bool HasCustomColor(this TransportLine transportLine)
        {
            return (transportLine.m_flags & TransportLine.Flags.CustomColor) == TransportLine.Flags.CustomColor;
        }

        public static bool HasCustomName(this TransportLine transportLine)
        {
            return (transportLine.m_flags & TransportLine.Flags.CustomName) == TransportLine.Flags.CustomName;
        }
    }
}