using Figgle;
    using System;
    using System.IO;

    namespace SharedLibrary.util
    {
        public static partial class Util
        {
            static string GetDate => DateTime.Now.ToString() + ": ";

            private static void WriteToFile(string message)
            {
                using (StreamWriter writer = new StreamWriter(ApplicationVariables.LogFilePath, true))
                {
                    writer.WriteLine(message);
                }
            }

            public static void Log(string title, ConsoleColor color = ConsoleColor.Cyan)
            {
                string message = "Log " + GetDate + title;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                WriteToFile("\tLog : ______________\n");
                WriteToFile(message);
                WriteToFile("\n______________\n");

            }

            public static void LogSuccess(string title, ConsoleColor color = ConsoleColor.Green)
            {
                string message = "Success " + GetDate + title;
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
                string message = GetDate + banner;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                WriteToFile("\tTitle :______________\n");
                WriteToFile(message);                
                WriteToFile("\n______________\n");

            }

            public static void LogError(string title, ConsoleColor color = ConsoleColor.Red)
            {
                string message = "Error " + GetDate + title;
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
                string logMessage = "Message " + GetDate + message;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(logMessage);
                Console.ResetColor();
                WriteToFile("\tMessage : ______________\n");
                WriteToFile(logMessage);
                WriteToFile("\n______________\n");
            }
        }
    }