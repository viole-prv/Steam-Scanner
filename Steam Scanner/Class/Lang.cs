using System;

namespace SteamScanner
{
    public static class Lang
    {
        public static string Declination(string[] Array, int Count)
        {
            int Index = Lang.Count(Array.Length, Count);

            if (Array.Length >= Index)
            {
                return Array[Index];
            }

            return null;
        }

        public static int Count(int Length, int Count)
        {
            int N = Math.Abs(Count) % 100;

            if (Length == 3)
            {
                if (N > 1)
                {
                    if (N < 5)
                    {
                        return 1;
                    }

                    return 2;
                }
            }
            else if (Length == 2)
            {
                if (N > 1)
                {
                    return 1;
                }
            }

            return 0;
        }
    }
}
