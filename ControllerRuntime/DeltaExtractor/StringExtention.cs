using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIAS.Framework.DeltaExtractor
{
    public static class StringExtention
    {
        public static string RemoveQuotes(this string value)
        {
            if (String.IsNullOrEmpty(value))
                return value;

            return value.Replace("[", String.Empty).Replace("]", String.Empty);
        }
        public static string AddQuotes(this string value)
        {
            if (String.IsNullOrEmpty(value)
                || value.StartsWith("["))
                return value;

            return $"[{value}]";
        }
    }
}
