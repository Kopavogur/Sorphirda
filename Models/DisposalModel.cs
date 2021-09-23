using AutoComplete.Utilities;
using Ganss.Excel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AutoComplete.Models
{
    public class DisposalModel
    {
        public SortedDictionary<LabelKey, List<Address>> AddressDictionary { get; init; }

        public Dictionary<string, AddressInfo> AddressInfoDictionary { get; init; }

        public Dictionary<string, List<AreaSchedule>> AreaScheduleGreyListDictionary { get; init; }

        public Dictionary<string, List<AreaSchedule>> AreaScheduleBlueListDictionary { get; init; }

        public Dictionary<string, Area> AreaDictionary { get; init; }

        public DisposalModel(string disposalFile, string infoFile)
        {
            AddressDictionary = new();
            AddressInfoDictionary = new();
            AreaScheduleGreyListDictionary = new();
            AreaScheduleBlueListDictionary = new();
            AreaDictionary = new();
            LoadAddressDictionary(disposalFile);
            //LoadAddressInfoDictionary(infoFile);
            AddressInfoDictionary = CsvToDictionary<AddressInfo>(infoFile, "{Heiti_nf} {Husmerking} {Serheiti}");
        }

        public List<LabelValue> AutoCompleteSearch(string term, bool suppressExact = true)
        {
            (string streetKey, string specifierKey) = GeneralUtils.SplitOnFirstSpace(term.ToLower());

            List<LabelKey> keyList = AddressDictionary.Keys.ToList<LabelKey>();
            int pos = keyList.BinarySearch(new(streetKey));

            List<LabelValue> labelValues = new();
            if (pos < 0)
            {
                for (int i = ~pos; i < keyList.Count && keyList[i].Key.StartsWith(streetKey); i++)
                {
                    labelValues.Add(new(keyList[i].Label, null));
                }
            } else {
                bool exactMatch = false;
                List<Address> specifierList = AddressDictionary[new(streetKey)];
                foreach (Address specifier in specifierList)
                {
                    if (specifier.Key.StartsWith(specifierKey)) {
                        string infoKey = streetKey + " " + specifier.Key;
                        AddressInfoDictionary.TryGetValue(infoKey, out AddressInfo info);
                        labelValues.Add(new(keyList[pos].Label + " " + specifier.Stadfang, info));
                        exactMatch = (specifier.Key == specifierKey);
                    }
                }

                // Clear list if there one exact match to prevent recurrence of menu.
                if (labelValues.Count == 1 && exactMatch && suppressExact)
                {
                    labelValues.Clear();
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
                AddressDictionary[key].Add(addr with { Stadfang = specifier });
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

        // Using ExcelMapper for this larger payload runs VERY slow and has a HUGE memory footprint.
        // See the generic CsvToDictionary implementation below that provides a much more clement treatment.
        public void LoadAddressInfoDictionary(string workbookFile)
        {
            {
                ExcelMapper addressMapper = new(workbookFile);
                SortedList<string, string> sample = new();
                IEnumerable<AddressInfo> addressEnumerator = addressMapper.Fetch<AddressInfo>("Stadfangaskra");
                foreach (AddressInfo addr in addressEnumerator)
                {
                    string key = (addr.Heiti_nf + " " + addr.Husmerking).ToLower();
                    if (!AddressInfoDictionary.ContainsKey(key))
                    {
                        AddressInfoDictionary.Add(key, addr with { Heiti_nf = null, Husmerking = null });
                    }
                }
            }
        }

        public Dictionary<string, V> CsvToDictionary<V>(string fileName, string keyFormat, bool duplicateKeyException = false) 
        {
            Dictionary<string, V> dictionary = new();

            // Pattern to split on ; honoring double quotes.
            Regex splitRegex = new("(?:^|;)(\"(?:[^\"])*\"|[^;]*)", RegexOptions.Compiled);

            using (StreamReader reader = new(fileName))
            {
                // Make sure header is readable and obtain.
                string header = reader.ReadLine();
                if (header is null)
                {
                    throw new FormatException("File " + fileName +" does not have a header line.");
                }

                // Set up column name to index mapping.
                string[] headers = SplitCSV(header, splitRegex);
                Dictionary<string, int> headerMap = new(); 
                for (int i = 0; i < headers.Length; i++)
                {
                    headerMap.Add(headers[i].ToLower(), i);
                }

                // Set up Property mapping for type V to PropertyInfo and column index in input data.
                PropertyInfo[] propertyInfo = typeof(V).GetProperties();
                Dictionary<PropertyInfo, int> propertyMap = new();
                Dictionary<PropertyInfo, TypeConverter> propertyConverterMap = new();
                for (int i = 0; i < propertyInfo.Length; i++)
                {
                    PropertyInfo info = propertyInfo[i];
                    string propertyNameLower = info.Name.ToLower();
                    // Only add if column of same name is present in input.
                    if (headerMap.ContainsKey(propertyNameLower))
                    {
                        propertyConverterMap.Add(info, TypeDescriptor.GetConverter(info.PropertyType));
                        propertyMap.Add(info, headerMap[propertyNameLower]);
                    }
                }

                // Find column references in keyFormat.
                Regex columnRefRegex = new(@"\{[^\}]+\}");
                MatchCollection columnMatches = columnRefRegex.Matches(keyFormat);
                Dictionary<string, string> keyVariableMap = new();
                foreach (Match m in columnMatches)
                {
                    keyVariableMap.Add(m.Value.Substring(1, m.Value.Length - 2).ToLower(), m.Value);
                }

                string line;
                while ((line = reader.ReadLine()) is not null)
                {
                    string[] data = SplitCSV(line, splitRegex);

                    // Expand data into keyFormat to produce key. This must lead to a unique key.
                    string key = keyFormat;
                    foreach (string variableKey in keyVariableMap.Keys)
                    {
                        key = key.Replace(keyVariableMap[variableKey], data[headerMap[variableKey]]);
                    }
                    key = Regex.Replace(key.Trim().ToLower(), @"\s+", " ");

                    // Type convert input data strings to appropriate types and add constructed V to Dictionary by key. 
                    object[] values = new object[propertyMap.Count];
                    int propertyPos = 0;
                    foreach (PropertyInfo info in propertyMap.Keys)
                    {
                        values[propertyPos++] = propertyConverterMap[info].ConvertFromString(data[propertyMap[info]]);
                    }

                    // Duplicate keys do not work in Dictionary and do also not provide deterministic lookup results.
                    // Change the keyFormat if the current one is not properly thought out. 
                    if (!dictionary.ContainsKey(key) || duplicateKeyException)
                    {
                        dictionary.Add(key, (V)Activator.CreateInstance(typeof(V), values));
                    }
                }
            }
            return dictionary;
        }

        public static string[] SplitCSV(string line, Regex splitRegEx)
        {
            MatchCollection mc = splitRegEx.Matches(line);
            string[] result = new string[mc.Count];
            for (int i = 0; i < mc.Count; i++)
            {
                result[i] = mc[i].Value.TrimStart(';');
            }
            return result;
        }

        // A few of the record definitions below have strange names to match column headers in Excel file
        public record Address(string Stadfang, string Svaedi)
        {
            public string Key => Stadfang.ToLower();
        }

        public record AddressInfo(string Heiti_nf, string Husmerking, string Hnit, double N_HNIT_WGS84, double E_HNIT_WGS84);

        public record AreaSchedule(
            string Svaedi,
            DateTime Dags_Fra,
            DateTime? Dags_Til
        );

        public record Area(
            string Svaedi,
            string Nafn
        );

        // Record does for some reason not implement IComparable so a proper class is required.
        public class LabelKey : IComparable<LabelKey>
        {
            public string Label { get; init; }
            public string Key { get; init; }

            public LabelKey(string label)
            {
                Label = label;
                Key = label.ToLower();
            }

            public override bool Equals(object other)
            {
                return Equals(other as LabelKey);
            }

            public bool Equals(LabelKey other)
            {
                return other != null && Key.Equals(other.Key);
            }

            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }

            public int CompareTo(LabelKey other)
            {
                return Key.CompareTo(other.Key);
            }
        }

        // jQuery Autocomplete widget is case sensitive on JSON responses hence the lower case names.
        public record LabelValue(
            string label,
            AddressInfo info
        );
    }
}
