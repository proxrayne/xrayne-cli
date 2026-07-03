namespace Cli.Output;

public sealed class CliConsole : ICliConsole
{
    public void Success(string message)
    {
        WriteLine(ConsoleColor.Green, $"[OK] {message}");
    }

    public void Error(string message)
    {
        WriteLine(ConsoleColor.Red, $"[ERROR] {message}");
    }

    public void Header(string title)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.Green, title);
        WriteLine(ConsoleColor.DarkGray, new string('=', title.Length));
    }

    public void Section(string title)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.Cyan, title);
    }

    public void Value(string label, string value)
    {
        Write(ConsoleColor.DarkGray, $"  {label}: ");
        Console.WriteLine(value);
    }

    public void Command(string command)
    {
        Write(ConsoleColor.DarkGray, "  > ");
        WriteLine(ConsoleColor.White, command);
    }

    public void Warning(string message)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.Yellow, $"  {message}");
    }

    private static void WriteLine(ConsoleColor color, string message)
    {
        Write(color, message);
        Console.WriteLine();
    }

    private static void Write(ConsoleColor color, string message)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ForegroundColor = previousColor;
    }
}
