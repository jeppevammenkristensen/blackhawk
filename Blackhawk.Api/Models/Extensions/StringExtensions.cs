using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blackhawk.Models.Extensions
{
    public static class StringExtensions
    {
        public static string FirstToUpper(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source;

            return char.ToUpper(source.First()) + new string(source.Skip(1).ToArray());
        }

        public static string SafePropertyName(this string str)
        {
            return _SafePropertyName(str.ToCharArray());
        }

        private static string _SafePropertyName(char[] source)
        {
            bool previousIsWhitespace = false;

            if (source.Length == 0)
                return "_Empty";

            List<char> resultArray = new List<char>();

            if (Char.IsDigit(source[0]))
            {
                resultArray.Add('R');
            }

            foreach (var str in source)
            {
                if (str == ' ')
                {
                    resultArray.Add('_');
                    previousIsWhitespace = true;
                    continue;
                }

                if (previousIsWhitespace && char.IsLower(str))
                {
                    resultArray.Add(char.ToUpper(str));
                }
                else
                {
                    resultArray.Add(str);
                }

                previousIsWhitespace = false;
            }

            return new string(resultArray.ToArray());
        }

        public static string ToTitleCase(this string str)
        {
            var sb = new StringBuilder(str.Length);
            var flag = true;

            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(flag ? char.ToUpper(c) : c);
                    flag = false;
                }
                else
                {
                    flag = true;
                }
            }

            return sb.ToString();
        }
    }
}