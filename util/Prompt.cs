using Spectre.Console;
namespace CalavScanner.utils;

public class Prompt
{
    string Question;
    public Prompt(string question = "Are You Sure?")
    {
        Question = question;
    }

    public string Ask()
    {
        AnsiConsole.Markup($"[blue]{Question} [/]");
        string? resp = Console.ReadLine();
        return resp ?? string.Empty;
    }
}
