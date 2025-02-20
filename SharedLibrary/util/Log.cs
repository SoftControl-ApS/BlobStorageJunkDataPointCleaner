using Figgle;
    using System;
    using System.IO;

    namespace SharedLibrary.util
    {
        public static partial class Util
        {
            static string GetDate => DateTime.Now.ToString() + ": ";
            public static object LockLogFile { get; } = new object();
            private static void WriteToFile(string message)
            {
                lock (LockLogFile)
                {
                    using (StreamWriter writer = new StreamWriter(ApplicationVariables.LogFilePath, true))
                    {
                        writer.WriteLine(message);
                    }
                }
            }

            public static void Log(string title, ConsoleColor color = ConsoleColor.Cyan)
            {
                string message = "\nLog " + GetDate + title;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                // WriteToFile("\tLog : ______________\n");
                WriteToFile(message);
                // WriteToFile("\n______________\n");
            }

            public static void LogSuccess(string title, ConsoleColor color = ConsoleColor.Green)
            {
                string message = "\nSuccess \t" + GetDate + title;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                WriteToFile("\tSuccess: ______________\n");
                WriteToFile(message);
                WriteToFile("\n______________\n");

            }

            public static void Title(string title, ConsoleColor color = ConsoleColor.Magenta)
            {
                var banner = FiggleFonts.Standard.Render(title);
                string message = "\n" + GetDate + banner;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                WriteToFile("\tTitle :______________\n");
                WriteToFile(message);                
                WriteToFile("\n______________\n");

            }

            public static void LogError(string title, ConsoleColor color = ConsoleColor.Red)
            {
                string message = "\nError " + GetDate + title;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                WriteToFile("\tException :______________\n");
                WriteToFile(message);
                WriteToFile("\n______________\n");
            }

            public static void LogError(Exception ex)
            {
                LogError(ex.Message);
            }

            public static void Message(string message)
            {
                string logMessage = "\nMessage " + GetDate + message;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(logMessage);
                Console.ResetColor();
                WriteToFile("\tMessage : ______________\n");
                WriteToFile(logMessage);
                WriteToFile("\n______________\n");
            }
        }
    }