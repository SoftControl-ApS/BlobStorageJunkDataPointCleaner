using Figgle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.util
{
    public static partial class Util
    {
        static string GetDate => DateTime.Now.ToString() + ": ";
        public static void Log(string title, ConsoleColor color = ConsoleColor.Cyan)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("Log " + GetDate + title);
            Console.ResetColor();
        }

        public static void LogSuccess(string title, ConsoleColor color = ConsoleColor.Green)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("Success " + GetDate + title);
            Console.ResetColor();
        }

        public static void Title(string title, ConsoleColor color = ConsoleColor.Magenta)
        {
            var banner = FiggleFonts.Standard.Render(title);
            Console.ForegroundColor = color;
            Console.WriteLine(GetDate + banner);
            Console.ResetColor();
        }

        public static void LogError(string title, ConsoleColor color = ConsoleColor.Red)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("Error " + GetDate + title);
            Console.ResetColor();
        }

        public static void LogError(Exception ex)
        {
            LogError(ex.Message);
        }

        public static void Message(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Message " + GetDate + message);
            Console.ResetColor();
        }
    }
}