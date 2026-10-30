using Spectre.Console;

namespace CalavHashScanner.utils;

public class Logging
{
    string File;    
    
    public enum LogLevel
    {
        INFO,
        WARNING,
        ERROR,
        EXCEPTION,
        FATAL,
        DEBUG,

    };

    public Logging(string file)
    {
        File = file;
    }

    public static string LogLevelToString(LogLevel level)
    {
        return level switch
        {
            LogLevel.INFO => "[blue]<INFO>[/]",
            LogLevel.WARNING => "[yellow]<WARN>[/]",
            LogLevel.ERROR => "[red]<ERROR>[/]",
            LogLevel.EXCEPTION => "[red bold]<EXC>[/]",
            LogLevel.FATAL => "[red italic]<FATAL>[/]",
            LogLevel.DEBUG => "[purple]<DEBUG>[/]",
            _ => "???",
        };

    }

    public void Log(LogLevel level, string message)
    {
        AnsiConsole.MarkupLine($"{LogLevelToString(level)} - {message}");
    }
}
