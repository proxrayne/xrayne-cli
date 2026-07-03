namespace Cli.Output;

public interface ICliConsole
{
    void Success(string message);

    void Error(string message);

    void Header(string title);

    void Section(string title);

    void Value(string label, string value);

    void Command(string command);

    void Warning(string message);
}
