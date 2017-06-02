using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoLineColor.Naming
{
    // TODO: Refactor the logic for making names/numbers unique so all strategies can use it.

    struct LineAnalysis
    {
        public int StopCount;
        public List<string> Districts;
        public bool HasNonDistrictStop;
    }

    abstract class NamingStrategyBase : INamingStrategy
    {
        public string GetName(TransportLine transportLine)
        {
            try
            {
                string result = null;

                switch (transportLine.Info.m_transportType)
                {
                    case TransportInfo.TransportType.Bus:
                        result = GetBusLineName(transportLine);
                        break;

                    case TransportInfo.TransportType.Metro:
                        result = GetMetroLineName(transportLine);
                        break;

                    case TransportInfo.TransportType.Train:
                        result = GetTrainLineName(transportLine);
                        break;

                    case TransportInfo.TransportType.Ship:
                        result = GetShipLineName(transportLine);
                        break;

                    case TransportInfo.TransportType.Tram:
                        result = GetTramLineName(transportLine);
                        break;

                    case TransportInfo.TransportType.Monorail:
                        result = GetMonorailLineName(transportLine);
                        break;

                    case TransportInfo.TransportType.CableCar:
                        result = GetCableCarLineName(transportLine);
                        break;

                    case TransportInfo.TransportType.Airplane:
                        result = GetBlimpLineName(transportLine);
                        break;
                }

                return result ?? GetGenericLineName(transportLine);
            }
            catch (Exception e)
            {
                Console.Instance.Error(e.ToString());
                return null;
            }
        }

        protected virtual string GetGenericLineName(TransportLine transportLine)
        {
            return null;
        }

        protected virtual string GetBusLineName(TransportLine transportLine)
        {
            return null;
        }

        protected virtual string GetTrainLineName(TransportLine transportLine)
        {
            return null;
        }

        protected virtual string GetMetroLineName(TransportLine transportLine)
        {
            return null;
        }

        protected virtual string GetCableCarLineName(TransportLine transportLine)
        {
            return null;
        }

        protected virtual string GetTramLineName(TransportLine transportLine)
        {
            return GetBusLineName(transportLine);
        }

        protected virtual string GetShipLineName(TransportLine transportLine)
        {
            return GetTrainLineName(transportLine);
        }

        protected virtual string GetMonorailLineName(TransportLine transportLine)
        {
            return GetTrainLineName(transportLine);
        }

        protected virtual string GetBlimpLineName(TransportLine transportLine)
        {
            return GetShipLineName(transportLine);
        }

        protected static LineAnalysis AnalyzeLine(TransportLine transportLine)
        {
            var theNetManager = Singleton<NetManager>.instance;
            var theDistrictManager = Singleton<DistrictManager>.instance;
            var stop = transportLine.m_stops;
            var firstStop = stop;

            var result = new LineAnalysis
            {
                Districts = new List<string>(),
                HasNonDistrictStop = false,
                StopCount = 0,
            };

            do
            {
                var stopInfo = theNetManager.m_nodes.m_buffer[stop];
                var district = theDistrictManager.GetDistrict(stopInfo.m_position);

                if (district == 0)
                {
                    result.HasNonDistrictStop = true;
                }
                else
                {
                    var districtName = theDistrictManager.GetDistrictName(district).Trim();
                    if (!result.Districts.Contains(districtName))
                    {
                        result.Districts.Add(districtName);
                    }
                }

                stop = TransportLine.GetNextStop(stop);
                result.StopCount++;
            } while (result.StopCount < Constants.MaxLineAnalysisStops && stop != firstStop);

            return result;
        }

        protected static List<string> GetExistingNames()
        {
            var names = new List<string>();
            var theTransportManager = Singleton<TransportManager>.instance;
            var theInstanceManager = Singleton<InstanceManager>.instance;
            var lines = theTransportManager.m_lines.m_buffer;
            for (ushort lineIndex = 0; lineIndex < lines.Length - 1; lineIndex++)
            {
                if (lines[lineIndex].HasCustomName())
                {
                    string name = theTransportManager.GetLineName(lineIndex);
                    if (!String.IsNullOrEmpty(name))
                    {
                        names.Add(name);
                    }
                }
            }
            return names;
        }

        protected static string GetInitials(string words)
        {
            string initials = words[0].ToString();
            for (int i = 0; i < words.Length - 1; i++)
            {
                if (words[i] == ' ')
                {
                    initials += words[i + 1];
                }
            }
            return initials;
        }

        protected static string FirstWord(string words)
        {
            var pos = words.IndexOf(' ');
            return pos >= 0 ? words.Substring(0, pos) : words;
        }
    }
}
