using Spectre.Console;

namespace CalavHashScanner.Utils {
	/// <summary>
	/// Defines the data structure for a crash report,
	/// and allows for printing it to the console (see the Render method for more information)
	/// </summary>
	public class CrashReport
	{
		string Message;
		public Exception Exc;
		public CrashReport(string message, Exception exception)
		{
			Message = message;
			Exc = exception;
		}
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
			using (StreamWriter stream = new StreamWriter(path))
			{
				unchecked
				{
					
				}
			}
		}
	}
}