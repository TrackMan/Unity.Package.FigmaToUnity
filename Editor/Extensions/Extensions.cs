using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Figma
{
    using Internals;

    [DebuggerStepThrough]
    internal static class Extensions
    {
        #region Const
        static readonly string[] unitsMap = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        static readonly string[] tensMap = { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        #endregion

        #region Methods
        internal static bool IsEmptyOrTrue(this bool? value) => !value.HasValue || value.Value;
        internal static bool HasValueAndTrue(this bool? value) => value == true;
        internal static bool HasValueAndFalse(this bool? value) => value == false;
        internal static bool HasPositive(this double? value) => value is > 0;
        internal static bool IsValue<T>(this T? source, T dest) where T : struct, Enum => source.HasValue && source.Value.Equals(dest);
        internal static int ToBit(this bool value) => value ? 1 : 0;

        internal static IEnumerable<T> IndexRedundantNames<T>(this IReadOnlyList<T> items, Func<T, string> getName, Action<T, string> setName, Func<int, string> postfixConverter)
        {
            foreach (IGrouping<string, T> group in items.GroupBy(getName).Where(y => y.Count() > 1))
            {
                int i = 0;
                foreach (T item in group)
                    setName(item, postfixConverter(i++));
            }

            return items;
        }
        internal static string NumberToWords(this int number)
        {
            switch (number)
            {
                case 0:
                    return unitsMap[0];

                case < 0:
                    return $"minus-{NumberToWords(Math.Abs(number))}";
            }

            string words = string.Empty;

            if (number / 1000000 > 0)
            {
                words += $"{NumberToWords(number / 1000000)}-million ";
                number %= 1000000;
            }

            if (number / 1000 > 0)
            {
                words += $"{NumberToWords(number / 1000)}-thousand ";
                number %= 1000;
            }

            if (number / 100 > 0)
            {
                words += $"{NumberToWords(number / 100)}-hundred ";
                number %= 100;
            }

            if (number <= 0)
                return words;

            if (words != string.Empty)
                words += "and-";
            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if (number % 10 > 0)
                    words += $"-{unitsMap[number % 10]}";
            }

            return words;
        }
        internal static RGBA GetAverageColor(this IEnumerable<RGBA> colors)
        {
            RGBA avgColor = new();
            int count = 0;

            foreach (RGBA color in colors)
            {
                avgColor.r += color.r;
                avgColor.g += color.g;
                avgColor.b += color.b;
                count++;
            }

            if (count == 0) return avgColor;

            avgColor.r /= count;
            avgColor.g /= count;
            avgColor.b /= count;
            return avgColor;
        }
        #endregion
    }
}