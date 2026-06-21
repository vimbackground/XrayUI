using System;
using System.Collections.Generic;

namespace XrayUI.Services
{
    public static class CustomRuleValueParser
    {
        public static List<string> Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var values = new List<string>();
            // Commas and semicolons are valid inside match values (for example,
            // regexp quantifiers and Windows paths), so only line breaks delimit values.
            foreach (var part in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Length == 0 || !seen.Add(part))
                {
                    continue;
                }

                values.Add(part);
            }

            return values;
        }
    }
}
