using AutoComplete.Utilities;
using Ganss.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoComplete.Models
{
    public class DisposalModel
    {
        public Dictionary<LabelKey, List<Address>> AddressDictionary { get; init; }

        public Dictionary<string, List<AreaSchedule>> AreaScheduleListDictionary { get; init; }

        public Dictionary<string, Area> AreaDictionary { get; init; }

        public DisposalModel(string excelFile)
        {
            AddressDictionary = new();
            AreaScheduleListDictionary = new();
            AreaDictionary = new();
            LoadAddressDictionary(excelFile);
        }

        public List<LabelValue> AutoCompleteSearch(string term)
        {
            (string streetKey, string specifier) = GeneralUtils.SplitOnFirstSpace(term.ToLower());
            var streetMatches = AddressDictionary.Where(p => p.Key.Key.StartsWith(streetKey)).ToList();

            List<LabelValue> labelValues = new();
            if (streetMatches.Count == 1 && streetMatches[0].Key.Key == streetKey)
            {
                var street = streetMatches[0];
                string streetName = street.Key.Label + " ";
                bool exactMatch = false;
                foreach (Address section in street.Value)
                {
                    if (section.Key.StartsWith(specifier))
                    {
                        string address = streetName + section.Stadfang;
                        labelValues.Add(new(address, ""));
                        exactMatch = (section.Key == specifier);
                    }
                }
                // Clear list if there one exact match
                if (labelValues.Count == 1 && exactMatch)
                {
                    labelValues.Clear();
                }
            }
            else if (streetMatches.Count > 0)
            {
                foreach (var street in streetMatches)
                {
                    labelValues.Add(new(street.Key.Label, ""));
                }
            }
            return labelValues;
        }

        public string GetAreaFromAddress(string address)
        {
            string area = null;
            if (!string.IsNullOrWhiteSpace(address))
            {
                (string streetName, string specifier) = GeneralUtils.SplitOnFirstSpace(address);
                LabelKey streetNameKey = new(streetName);
                string specifierKey = specifier.ToLower();
                if (AddressDictionary.ContainsKey(streetNameKey))
                {
                    foreach (Address a in AddressDictionary[streetNameKey])
                    {
                        if (a.Stadfang.ToLower() == specifierKey)
                        {
                            area = a.Svaedi;
                            break;
                        }
                    }
                }
            }
            return area;
        }

        public List<AreaSchedule> GetScheduleListFromArea(string area, bool showFuture = true)
        {
            List<AreaSchedule> scheduleList = new();
            if (area is not null && AreaScheduleListDictionary.ContainsKey(area)) {
                if (showFuture)
                {
                    DateTime now = DateTime.Now;
                    foreach (AreaSchedule schedule in AreaScheduleListDictionary[area])
                    {
                        if (schedule.Dags_Fra > now || (schedule.Dags_Til is not null && schedule.Dags_Til > now))
                        {
                            scheduleList.Add(schedule);
                        }
                    }
                }
                else
                {
                    scheduleList = AreaScheduleListDictionary[area];
                }
            }
            return scheduleList;
        }

        public string GetAreaName(string area)
        {
            if (area is not null && AreaDictionary.ContainsKey(area))
            {
                return AreaDictionary[area].Nafn;
            }
            return null;
        }

        public void LoadAddressDictionary(string workbookFile)
        {
            ExcelMapper addressMapper = new(workbookFile);

            // Load Address to Area mapping
            IEnumerable<Address> addressEnumerator = addressMapper.Fetch<Address>("StadfongSvaedi");
            foreach (Address addr in addressEnumerator)
            {
                (string streetName, string specifier) = GeneralUtils.SplitOnFirstSpace(addr.Stadfang);
                LabelKey key = new(streetName);
                if (!AddressDictionary.ContainsKey(key)) 
                { 
                    AddressDictionary.Add(key, new());
                }
                AddressDictionary[key].Add(new(specifier, addr.Svaedi));
            }

            // Load Area disposal schedules
            IEnumerable<AreaSchedule> scheduleEnumerator = addressMapper.Fetch<AreaSchedule>("Losun");
            foreach (AreaSchedule schedule in scheduleEnumerator)
            {
                if (!AreaScheduleListDictionary.ContainsKey(schedule.Svaedi))
                {
                    AreaScheduleListDictionary.Add(schedule.Svaedi, new());
                }
                AreaScheduleListDictionary[schedule.Svaedi].Add(schedule);
            }

            // Load Area Name mapping
            IEnumerable<Area> areaEnumerator = addressMapper.Fetch<Area>("SvaediNofn");
            foreach (Area area in areaEnumerator)
            {
                AreaDictionary.Add(area.Svaedi, area);
            }
        }

        // A few of the record definitions below have strange names to match column headers in Excel file
        public record Address(string Stadfang, string Svaedi)
        {
            public string Key => Stadfang.ToLower();
        }

        public record AreaSchedule(
            string Svaedi,
            DateTime Dags_Fra,
            DateTime? Dags_Til
        );

        public record Area(
            string Svaedi,
            string Nafn
        );

        public sealed record LabelKey(string Label)
        {
            public string Key => Label.ToLower();

            public bool Equals(LabelKey other)
            {
                return other != null && Key.Equals(other.Key);
            }

            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }
        }

        public record LabelValue(
            string Label,
            string Value
        );
    }
}
