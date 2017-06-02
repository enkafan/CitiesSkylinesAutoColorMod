using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Plugins;
using Random = UnityEngine.Random;

namespace AutoLineColor.Naming
{
    internal class DistrictNamingStrategy : NamingStrategyBase
    {
        private static Console logger = Console.Instance;

        // todo could this be localized?

        protected override string GetTrainLineName(TransportLine transportLine)
        {
            var analysis = AnalyzeLine(transportLine);
            var lineNumber = transportLine.m_lineNumber;
            var districts = analysis.Districts;
            var stopCount = analysis.StopCount;

            if (districts.Count == 1)
            {
                if (districts[0] == string.Empty)
                {
                    return string.Format("#{0} {1} Shuttle", lineNumber, districts[0]);
                }

                var rnd = Random.value;
                if (rnd <= .33f)
                {
                    return string.Format("#{0} {1} Limited", lineNumber, districts[0]);
                }

                if (rnd <= .66f)
                {
                    return string.Format("#{0} {1} Service", lineNumber, districts[0]);
                }

                return string.Format("#{0} {1} Shuttle", lineNumber, districts[0]);
            }
            if (districts.Count == 2)
            {
                if (string.IsNullOrEmpty(districts[0]) || string.IsNullOrEmpty(districts[1]))
                {
                    return string.Format("#{0} {1} Shuttle", lineNumber, districts[0]);
                }

                var rnd = Random.value;
                if (rnd <= .33f)
                {
                    return string.Format("#{0} {1}&{2}", lineNumber, districts[0].Substring(0, 1),
                        districts[1].Substring(0, 1));
                }
                if (rnd <= .5)
                {
                    return string.Format("#{0} {1} Zephr", lineNumber, districts[0].Substring(0, 1));
                }
                if (rnd <= .7)
                {
                    return string.Format("#{0} {1} Flyer", lineNumber, districts[0].Substring(0, 1));
                }
                return string.Format("#{0} {1} & {2}", lineNumber, districts[0], districts[1]);
            }

            return string.Format("#{0} Unlimited", lineNumber);
        }

        protected override string GetBusLineName(TransportLine transportLine)
        {
            var analysis = AnalyzeLine(transportLine);
            var lineNumber = transportLine.m_lineNumber;
            var districts = analysis.Districts;
            var stopCount = analysis.StopCount;

            if (districts.Count == 1)
            {
                if (string.IsNullOrEmpty(districts[0]))
                {
                    return string.Format("#{0} Line", lineNumber);
                }

                return string.Format("#{0} {1} Local", lineNumber, districts[0]);
            }

            if (districts.Count == 2 && string.IsNullOrEmpty(districts[0]) && string.IsNullOrEmpty(districts[1]))
                return string.Format("#{0} Line", lineNumber);


            if (districts.Count == 2 && stopCount <= 4)
                return string.Format("#{0} {1} / {2} Express", lineNumber, districts[0], districts[1]);

            if (districts.Count == 2)
                return string.Format("#{0} {1} / {2} Line", lineNumber, districts[0], districts[1]);

            return string.Format("#{0} Line", lineNumber);
        }

        protected override string GetMetroLineName(TransportLine transportLine)
        {
            return GetBusLineName(transportLine);
        }

        protected override string GetGenericLineName(TransportLine transportLine)
        {
            return string.Format("#{0} Line", transportLine.m_lineNumber);
        }
    }
}