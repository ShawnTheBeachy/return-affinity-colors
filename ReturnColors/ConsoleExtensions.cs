namespace ReturnColors;

internal static class ConsoleExtensions
{
    extension(Console)
    {
        private static void ColoredLine(string? value, ConsoleColor color)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ForegroundColor = previousColor;
        }

        public static void GreenLine(string? value) => ColoredLine(value, ConsoleColor.Green);

        public static void RedLine(string? value) => ColoredLine(value, ConsoleColor.Red);

        public static void YellowLine(string? value) => ColoredLine(value, ConsoleColor.Yellow);
    }
}
