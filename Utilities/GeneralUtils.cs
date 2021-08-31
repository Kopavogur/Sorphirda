using System;

namespace AutoComplete.Utilities
{
    public class GeneralUtils
    {
        public static (string, string) SplitOnFirstSpace(string input)
        {
            string[] parts = input.Split(' ', StringSplitOptions.TrimEntries);
            string rest = string.Join(" ", parts, 1, parts.Length - 1);
            return (parts[0], rest);
        }
    }
}
