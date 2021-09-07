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

        public Dictionary<string, List<AreaSchedule>> AreaScheduleGreyListDictionary { get; init; }

        public Dictionary<string, List<AreaSchedule>> AreaScheduleBlueListDictionary { get; init; }

        public Dictionary<string, Area> AreaDictionary { get; init; }

        public DisposalModel(string excelFile)
        {
            AddressDictionary = new();
            AreaScheduleGreyListDictionary = new();
            AreaScheduleBlueListDictionary = new();
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

        public List<AreaSchedule> GetBlueScheduleListFromArea(string area, bool showFuture = true)
        {
            return GetScheduleListFromArea(area, AreaScheduleBlueListDictionary, showFuture);
        }

        public List<AreaSchedule> GetGreyScheduleListFromArea(string area, bool showFuture = true)
        {
            return GetScheduleListFromArea(area, AreaScheduleGreyListDictionary, showFuture);
        }

        private List<AreaSchedule> GetScheduleListFromArea(string area, Dictionary<string, List<AreaSchedule>> areaScheduleListDictionary, bool showFuture)
        {
            List<AreaSchedule> scheduleList = new();
            if (area is not null && areaScheduleListDictionary.ContainsKey(area)) {
                if (showFuture)
                {
                    DateTime now = DateTime.Now;
                    foreach (AreaSchedule schedule in areaScheduleListDictionary[area])
                    {
                        if (schedule.Dags_Fra > now || (schedule.Dags_Til is not null && schedule.Dags_Til > now))
                        {
                            scheduleList.Add(schedule);
                        }
                    }
                }
                else
                {
                    scheduleList = areaScheduleListDictionary[area];
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

            // Load Area Grey disposal schedules
            {
                IEnumerable<AreaSchedule> scheduleEnumerator = addressMapper.Fetch<AreaSchedule>("LosunGra");
                foreach (AreaSchedule schedule in scheduleEnumerator)
                {
                    if (!AreaScheduleGreyListDictionary.ContainsKey(schedule.Svaedi))
                    {
                        AreaScheduleGreyListDictionary.Add(schedule.Svaedi, new());
                    }
                    AreaScheduleGreyListDictionary[schedule.Svaedi].Add(schedule);
                }
            }

            // Load Area Blue disposal schedules
            {
                IEnumerable<AreaSchedule> scheduleEnumerator = addressMapper.Fetch<AreaSchedule>("LosunBla");
                foreach (AreaSchedule schedule in scheduleEnumerator)
                {
                    if (!AreaScheduleBlueListDictionary.ContainsKey(schedule.Svaedi))
                    {
                        AreaScheduleBlueListDictionary.Add(schedule.Svaedi, new());
                    }
                    AreaScheduleBlueListDictionary[schedule.Svaedi].Add(schedule);
                }
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
