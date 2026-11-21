// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.
using Spectre.Console;
namespace CalavScanner.utils;

public class Prompt {
    string Question;
    public Prompt(string question = "Are You Sure?") {
        Question = question;
    }
    /// <summary>
    /// Asks the user for input. the question argument is defined in the constructor and cannot be changed here.
    /// Will return the user's response or string.empty if the response is null.
    /// </summary>
    /// <returns></returns>
    public string Ask() {
        AnsiConsole.Markup($"[blue]{Question} [/]");
        string? resp = Console.ReadLine();
        return resp ?? string.Empty;
    }
}
