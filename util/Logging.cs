// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.
using Spectre.Console;

namespace CalavHashScanner.utils;

public partial class Logging {
    string File;

    public Logging(string file) {
        File = file;
    }

    public static string LogLevelToString(LogLevel level) {
        return level switch {
            LogLevel.INFO => "[blue]<INFO>[/]",
            LogLevel.WARNING => "[yellow]<WARN>[/]",
            LogLevel.ERROR => "[red]<ERROR>[/]",
            LogLevel.EXCEPTION => "[red bold]<EXC>[/]",
            LogLevel.FATAL => "[red italic]<FATAL>[/]",
            LogLevel.DEBUG => "[purple]<DEBUG>[/]",
            _ => "???",
        };

    }

    public void Log(LogLevel level, string message) {
        AnsiConsole.MarkupLine($"{LogLevelToString(level)} - {message}");
    }
}
