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
        public static void Log(string title, ConsoleColor color = ConsoleColor.DarkMagenta)
        {
            var banner = FiggleFonts.Standard.Render(title);
            Console.ForegroundColor = color;
            Console.WriteLine(banner);
            Console.ResetColor();
        }

        public static void LogError(string title, ConsoleColor color = ConsoleColor.DarkMagenta)
        {
            var banner = FiggleFonts.Standard.Render(title);
            Console.ForegroundColor = color;
            Console.WriteLine(banner);
            Console.ResetColor();
        }

        public static void Message(string message)
        {
            //var banner = FiggleFonts.Alligator2.Render(message);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
