using System;
using System.Collections.Generic;

namespace SteamScanner
{
    public partial class Program
    {
        public class Support
        {
            public static int Table<T>(string Start, List<T> Selection, bool Exit = true, int Position = 0)
            {
                Console.CursorVisible = false;

                const byte O = 1;
                const byte L = 8;

                int Index = 0;

                do
                {
                    for (int i = 0; i < Selection.Count; i++)
                    {
                        Console.SetCursorPosition(L, Position + (i / O));

                        if (i == Index)
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;

                            Console.Write($"{Start} {Selection[i]}");
                        }
                        else
                        {
                            Console.Write(string.Empty.PadLeft(Start.Length + 1) + Selection[i]);
                        }

                        Console.ResetColor();
                    };

                    var Read = Console.ReadKey(true);

                    switch (Read.Key)
                    {
                        case ConsoleKey.UpArrow:
                            if (Index >= O)
                            {
                                Index -= O;
                            }
                            else
                            {
                                Index += Selection.Count - 1;
                            }

                            break;

                        case ConsoleKey.DownArrow:
                            if (Index + O < Selection.Count)
                            {
                                Index += O;
                            }
                            else
                            {
                                Index = 0;
                            }

                            break;

                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.Escape:
                            if (Exit)
                            {
                                return -1;
                            }

                            break;

                        case ConsoleKey.RightArrow:
                        case ConsoleKey.Enter:
                            Console.CursorVisible = true;

                            return Index;
                    }
                }
                while (true);
            }

            public static bool Read(out string Line)
            {
                Line = string.Empty;

                int Index = 0;

                do
                {
                    var Read = Console.ReadKey(true);

                    switch (Read.Key)
                    {
                        case ConsoleKey.Escape:
                            return false;

                        case ConsoleKey.Enter:
                            return true;

                        case ConsoleKey.Backspace:
                            if (Index > 0)
                            {
                                Line = Line.Remove(Line.Length - 1);

                                Console.Write(Read.KeyChar);
                                Console.Write(' ');
                                Console.Write(Read.KeyChar);

                                Index--;
                            }

                            break;

                        default:
                            Line += Read.KeyChar;

                            Console.Write(Read.KeyChar);

                            Index++;

                            break;
                    }
                }
                while (true);
            }
        }
    }
}
