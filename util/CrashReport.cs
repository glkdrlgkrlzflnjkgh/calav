using Spectre.Console;

namespace CalavHashScanner.utils;

/// <summary>
/// Defines the data structure for a crash report,
/// and allows for printing it to the console (see the Render method for more information)
/// </summary>
public class CrashReport(string message, Exception exception)
{
    readonly string Message = message;
    public Exception Exc = exception;

    /// <summary>
    /// A method to render a crash report to the screeen.
    /// Takes no arguments and has no overloads.
    /// (Could throw if you insert markup into the message, even unintentionally. So wrap in try/catch if possible...)
    /// </summary>
    public void Render()
    {
        AnsiConsole.MarkupLine("---- BEGIN CRASH REPORT ----");
        AnsiConsole.MarkupLine(Message);
        AnsiConsole.MarkupLine("Details of the error, including stacktrace.");
        Console.WriteLine($"Exception Message: {Exc.Message}");
        Console.WriteLine($"Exception StackTrace: {Exc.StackTrace}");
        Console.WriteLine($"Exception Source: {Exc.Source}");
        if (Exc.HelpLink != null)
        {
            AnsiConsole.MarkupLine($"The exception has a help link, and it is [blue]{Exc.HelpLink}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("The exception does not have a help link.");
        }
        AnsiConsole.MarkupLine("--- END CRASH REPORT ----");
    }
    /// <summary>
    /// Function to save a crash report to a .txt file at the target path.
    /// </summary>
    /// <param name="path"></param>
    public void Save(string path)
    {
        using StreamWriter writer = new(path, false, System.Text.Encoding.UTF8);

        writer.WriteLine("---- BEGIN CRASH REPORT ----");
        writer.WriteLine(Message);
        writer.WriteLine("Details of the error, including stacktrace.");

        writer.WriteLine($"Exception Message: {Exc.Message}");
        writer.WriteLine($"Exception StackTrace: {Exc.StackTrace}");
        writer.WriteLine($"Exception Source: {Exc.Source}");

        if (Exc.HelpLink != null)
        {
            writer.WriteLine($"Help Link: {Exc.HelpLink}");
        }
        else
        {
            writer.WriteLine("The exception does not have a help link.");
        }

        writer.WriteLine("---- END CRASH REPORT ----");
    }
}