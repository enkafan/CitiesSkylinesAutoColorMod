using System;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Plugins;
using Random = UnityEngine.Random;

namespace AutoLineColor.Naming
{
    internal class LondonNamingStrategy : NamingStrategyBase
    {
        private static string[] _trains =
        {
            "{0}",
            "{0} Service",
            "{0} Rail",
            "{0} Railway",
            "{0} Flyer",
            "{0} Zephyr",
            "{0} Rocket",
            "{0} Arrow",
            "{0} Special",
            "Spirit of {0}",
            "Pride of {0}",
        };



        private static List<string> GetNumbers(List<string> names)
        {
            var numbers = new List<string>();
            foreach (var name in names)
            {
                numbers.Add(FirstWord(name));
            }
            return numbers;
        }



        private static string TryBakerlooify(string word1, string word2)
        {
            int offset1 = Math.Min(word1.Length - 1, Math.Max(word1.Length / 2, 4));
            int offset2 = word2.Length / 4;
            int length2 = Math.Max(word2.Length / 2, 3);

            string substring2 = word2.Substring(offset2, length2);

            for (int offset = offset1; offset < word1.Length; offset++)
            {
                if (substring2.IndexOf(word1[offset]) >= 0)
                {
                    return word1.Substring(0, offset) + word2.Substring(offset2 + substring2.IndexOf(word1[offset]));
                }
            }
            return null;
        }

        /*
         * Bus line numbers are based on district:
         *
         * Given districts "Hamilton Park", "Ivy Square", "King District" in a city called "Springwood", bus line names look like:
         *
         * HP43 Local
         * 22 Hamilton Park
         * 345 Ivy to King Express
         * 9 Hamilton, Ivy and King
         * 6 Springwood Express
         */

        protected override string GetBusLineName(TransportLine transportLine)
        {
            var analysis = AnalyzeLine(transportLine);
            string prefix = null;
            int number;
            string name = null;
            string suffix = null;
            var existingNames = GetExistingNames();
            var existingNumbers = GetNumbers(existingNames);

            // Work out the bus number (and prefix)
            if (!analysis.HasNonDistrictStop && analysis.Districts.Count == 1)
            {
                /* District Initials */
                prefix = GetInitials(analysis.Districts[0]);
                number = 0;
                string prefixed_number;
                do
                {
                    number++;
                    prefixed_number = String.Format("{0}{1}", prefix, number);
                } while (existingNumbers.Contains(prefixed_number));
            }
            else
            {
                int step;
                if (analysis.StopCount < 15)
                {
                    number = Random.Range(100, 900);
                    step = Random.Range(7, 20);
                }
                else if (analysis.StopCount < 30)
                {
                    number = Random.Range(20, 100);
                    step = Random.Range(2, 10);
                }
                else
                {
                    number = Random.Range(1, 20);
                    step = Random.Range(1, 4);
                }
                while (existingNumbers.Contains(number.ToString()))
                {
                    number += step;
                }
            }

            // Work out the bus name
            switch (analysis.Districts.Count)
            {
                case 1:
                    name = analysis.HasNonDistrictStop ? analysis.Districts[0] : "Local";
                    break;

                case 2:
                    name = String.Format("{0} to {1}", FirstWord(analysis.Districts[0]), FirstWord(analysis.Districts[1]));
                    break;

                case 3:
                    name = String.Format("{0}, {1} and {2}",
                        FirstWord(analysis.Districts[0]), FirstWord(analysis.Districts[1]), FirstWord(analysis.Districts[2]));
                    break;

                default:
                    var theSimulationManager = Singleton<SimulationManager>.instance;
                    name = theSimulationManager.m_metaData.m_CityName;
                    break;
            }

            if (analysis.StopCount <= 4)
            {
                suffix = "Express";
            }

            string lineName = String.Format("{0}{1}", prefix ?? "", number);
            if (!String.IsNullOrEmpty(name))
            {
                lineName += " " + name;
            }
            if (!String.IsNullOrEmpty(suffix))
            {
                lineName += " " + suffix;
            }
            return lineName;
        }

        /*
         * Metro line names are based on district, with generic names from a list thrown in.
         *
         * Given districts "Manor Park", "Ivy Square", "Hickory District", metro line names look like:
         *
         * Manor Line
         * Ivy Loop Line
         * Hickory & Ivy Line
         * Hickory, Manor & Ivy Line
         * Foxtrot Line
         *
         * There's also some attempt to "Bakerlooify" line names.  No idea how well that will work.
         */

        protected override string GetMetroLineName(TransportLine transportLine)
        {
            var analysis = AnalyzeLine(transportLine);
            string name = null;
            var districtFirstNames = analysis.Districts.Select(FirstWord).ToList();
            var existingNames = GetExistingNames();
            int count = 0;

            switch (analysis.Districts.Count)
            {
                case 0:
                    // empty
                    break;

                case 1:
                    name = analysis.Districts[0];
                    break;

                case 2:
                    if (districtFirstNames[0].Equals(districtFirstNames[1]))
                    {
                        name = districtFirstNames[0];
                    }
                    else
                    {
                        name = TryBakerlooify(districtFirstNames[0], districtFirstNames[1]) ??
                            TryBakerlooify(districtFirstNames[1], districtFirstNames[0]) ??
                            String.Format("{0} & {1}", districtFirstNames[0], districtFirstNames[1]);
                    }
                    break;

                default:
                    int totalLength = districtFirstNames.Sum(d => d.Length);
                    if (totalLength < 20)
                    {
                        var districtFirstNamesArray = districtFirstNames.ToArray();
                        name = String.Format("{0} & {1}",
                            String.Join(", ", districtFirstNamesArray, 0, districtFirstNamesArray.Length - 1),
                            districtFirstNamesArray[districtFirstNamesArray.Length - 1]);
                    }
                    break;
            }

            var lineName = name == null ? "Metro Line" : String.Format("{0} Line", name);
            while (name == null || existingNames.Contains(lineName))
            {
                name = GenericNames.GetGenericName(count / 2);
                lineName = String.Format("{0} Line", name);
                count++;
            }
            return lineName;
        }

        /*
         * Train line names are based on the British designations, with some liberties taken.
         *
         * The format is AXNN Name:
         *
         * A is the number of districts the train stops at.
         * X is the first letter of the last district, or X if the train stops outside of a district.
         * NN are random digits.
         *
         * The name is based on the district names.
         */

        protected override string GetTrainLineName(TransportLine transportLine)
        {
            var analysis = AnalyzeLine(transportLine);
            string ident = null;
            int number = Random.Range(1, 90);
            string name = null;
            var districtFirstNames = analysis.Districts.Select(FirstWord).ToList();
            var existingNames = GetExistingNames();
            var existingNumbers = GetNumbers(existingNames);

            var lastDistrictName = analysis.Districts.LastOrDefault();
            if (String.IsNullOrEmpty(lastDistrictName))
            {
                lastDistrictName = "Z";
            }

            ident = String.Format("{0}{1}", analysis.Districts.Count, analysis.HasNonDistrictStop ? "X" : lastDistrictName.Substring(0, 1));

            switch (analysis.Districts.Count)
            {
                case 0:
                    var theSimulationManager = Singleton<SimulationManager>.instance;
                    name = String.Format(_trains[Random.Range(0, _trains.Length)], theSimulationManager.m_metaData.m_CityName);
                    break;

                case 1:
                    name = String.Format(_trains[Random.Range(0, _trains.Length)], analysis.Districts[0]);
                    break;

                case 2:
                    if (districtFirstNames[0].Equals(districtFirstNames[1]))
                    {
                        name = districtFirstNames[0];
                    }
                    else if (analysis.StopCount == 2)
                    {
                        name = String.Format("{0} {1} Shuttle", districtFirstNames[0], districtFirstNames[1]);
                    }
                    else if (!analysis.HasNonDistrictStop)
                    {
                        name = String.Format("{0} {1} Express", districtFirstNames[0], districtFirstNames[1]);
                    }
                    else
                    {
                        name = String.Format("{0} via {1}", districtFirstNames[0], districtFirstNames[1]);
                    }
                    break;

                default:
                    int totalLength = districtFirstNames.Sum(d => d.Length);
                    if (totalLength < 15)
                    {
                        var districtFirstNamesArray = districtFirstNames.ToArray();
                        name = String.Format("{0} and {1} via {2}",
                            String.Join(", ", districtFirstNamesArray, 0, districtFirstNamesArray.Length - 2),
                            districtFirstNamesArray[districtFirstNamesArray.Length - 1],
                            districtFirstNamesArray[districtFirstNamesArray.Length - 2]);
                    }
                    else
                    {
                        name = String.Format(_trains[Random.Range(0, _trains.Length)], analysis.Districts[0]);
                    }
                    break;
            }

            var lineNumber = String.Format("{0}{1:00}", ident, number);
            while (existingNumbers.Contains(lineNumber))
            {
                number++;
                lineNumber = String.Format("{0}{1:00}", ident, number);
            }
            return String.Format("{0} {1}", lineNumber, name);
        }
    }
}
