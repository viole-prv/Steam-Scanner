using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SteamScanner
{
    public partial class Helper
    {
        public static decimal? ToPrice(string _)
        {
            string[] Split = _.Split(' ');

            if (Split.Length > 1)
            {
                string Last = Split.LastOrDefault();
                string First = Split.FirstOrDefault();

                if (!string.IsNullOrEmpty(Last) && !string.IsNullOrEmpty(First))
                {
                    if (decimal.TryParse(First, NumberStyles.Currency,

                        Last == "USD" ? CultureInfo.GetCultureInfo("en-US") :
                        Last == "pуб." ? CultureInfo.GetCultureInfo("ru-RU") :
                        Last == "TL" ? CultureInfo.GetCultureInfo("tr-TR") :

                        CultureInfo.CurrentCulture, out decimal Price))
                    {
                        return Math.Ceiling(Price * 100);
                    }
                }
            }

            return null;
        }

        public static (uint ID, string Type) ToAnalyze(string _)
        {
            var Match = Regex.Match(_, @"https:\/\/store\.steampowered\.com\/(sub|bundle|app)\/([0-9]+)\/");

            if (Match.Success &&
                Match.Groups[1].Success &&
                Match.Groups[2].Success)
            {
                if (uint.TryParse(Match.Groups[2].Value, NumberStyles.Integer, CultureInfo.CurrentCulture, out uint AppID))
                {
                    return (AppID, Match.Groups[1].Value);
                }
            }

            return (0, string.Empty);
        }

        public static string ToSummary(decimal _, bool Space = false)
        {
            return $"{(_ >= 0 ? _ == 0 ? "" : $"{(Space ? " " : "")}▲ " : $"{(Space ? " " : "")}▼ ")}{Math.Abs(_)}%";
        }

        public static byte ToAverage(int Count)
        {
            switch (Count)
            {
                case 5:
                case 6:
                    return 3;

                case 7:
                case 8:
                    return 4;

                case 9:
                case 10:
                    return 5;

                case 11:
                case 12:
                    return 6;

                case 13:
                case 14:
                    return 7;

                case 15:
                case 16:
                    return 8;

                case 17:
                case 18:
                    return 9;

                case 19:
                case 20:
                    return 10;

                default:
                    return 0;
            }
        }
    }
}
