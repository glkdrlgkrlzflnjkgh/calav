// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.
using Spectre.Console;

namespace CalavHashScanner.utils;

/// <summary>
/// Defines the data structure for a crash report,
/// and impliments printing it to the console and saving it to a file.
/// </summary>
public class CrashReport(string message, Exception exception) {
    readonly string Message = message;
    public Exception Exc = exception;

    /// <summary>
    /// A method to render a crash report to the screeen.
    /// Takes no arguments and has no overloads.
    /// (Could throw if you insert markup into the message, even unintentionally. So wrap in try/catch if possible...)
    /// </summary>
    public void Render() {
        AnsiConsole.MarkupLine("---- BEGIN CRASH REPORT ----");
        AnsiConsole.MarkupLine(Message);
        AnsiConsole.MarkupLine("Details of the error, including stacktrace.");
        Console.WriteLine($"Exception Message: {Exc.Message}");
        Console.WriteLine($"Exception StackTrace: {Exc.StackTrace}");
        Console.WriteLine($"Exception Source: {Exc.Source}");
        AnsiConsole.MarkupLine("--- END CRASH REPORT ----");
    }
    /// <summary>
    /// Function to save a crash report to a .txt file at the target path.
    /// Has the possibility to throw. So wrap in a try/catch if possible...
    /// </summary>
    /// <param name="path"></param>
    public void Save(string path) {
        using StreamWriter writer = new(path, false, System.Text.Encoding.UTF8);

        writer.WriteLine("---- BEGIN CRASH REPORT ----");
        writer.WriteLine(Message);
        writer.WriteLine("Details of the error, including stacktrace.");

        writer.WriteLine($"Exception Message: {Exc.Message}");
        writer.WriteLine($"Exception StackTrace: {Exc.StackTrace}");
        writer.WriteLine($"Exception Source: {Exc.Source}");
        writer.WriteLine("---- END CRASH REPORT ----");
    }
}