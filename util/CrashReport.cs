using System;
using System.Collections.Generic;
using Spectre.Console;
namespace CalavHashScanner.Utils {
	public class CrashReport
	{
		string Message;
		Exception Exc;
		public CrashReport(string message, Exception exception)
		{
			Message = message;
			Exc = exception;
		}

		public void Render()
		{
			AnsiConsole.MarkupLine("---- BEGIN CRASH REPORT ----");
			AnsiConsole.MarkupLine(Message);
			AnsiConsole.WriteException(Exc);
			AnsiConsole.MarkupLine("--- END CRASH REPORT ----");
		}
	}
}