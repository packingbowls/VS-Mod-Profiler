using System;
using System.Threading;

namespace VSModProfiler
{
    public static class UIManager
    {
        public static void DrawHeader(AppConfig config, string subtitle = "Home Dashboard")
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== VS Mod Profiler v{config.AppVersion} | {subtitle} ===");
            Console.ResetColor();

            string activeDisplay = (!string.IsNullOrEmpty(config.ActiveVersion) && !string.IsNullOrEmpty(config.ActiveProfile))
                ? $"{config.ActiveVersion} -> {config.ActiveProfile}"
                : "None";

            Console.Write("Loaded Profile: ");
            Console.ForegroundColor = activeDisplay == "None" ? ConsoleColor.DarkGray : ConsoleColor.Green;
            Console.WriteLine(activeDisplay);
            Console.ResetColor();
            Console.WriteLine(new string('-', 52));
        }

        public static int PromptMenuChoice(int maxOption)
        {
            while (true)
            {
                Console.Write($"\nSelect option (0-{maxOption}): ");
                string? input = Console.ReadLine()?.Trim();

                if (int.TryParse(input, out int choice) && choice >= 0 && choice <= maxOption)
                {
                    return choice;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid input. Enter a number between 0 and {maxOption}.");
                Console.ResetColor();
            }
        }

        public static string PromptNonEmptyString(string promptText)
        {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            while (true)
            {
                Console.Write($"{promptText}: ");
                string? input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Input cannot be empty.");
                    Console.ResetColor();
                    continue;
                }

                if (input.IndexOfAny(invalidChars) >= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Name contains invalid directory characters.");
                    Console.ResetColor();
                    continue;
                }

                return input;
            }
        }

        public static void PlayLoadingAnimation(string actionLabel)
        {
            Console.Write($"\n{actionLabel} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(220);
                Console.Write(". ");
            }
            Console.ResetColor();
            Thread.Sleep(150);
            Console.WriteLine();
        }

        public static void ShowMessage(string message, ConsoleColor color = ConsoleColor.Gray, bool pause = true)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"\n{message}");
            Console.ResetColor();
            if (pause)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
        }
    }
}