// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;
using CalavHashScanner.Services;

namespace CalavHashScanner
{
	class Program
	{
		public static bool debugMode;

		// Remote hash list URL
		private const string HashListUrl =
			"https://raw.githubusercontent.com/romainmarcoux/malicious-hash/refs/heads/main/full-hash-sha256-aa.txt";

		static async Task Main(string[] args)
		{
			DateTime firstCommit = new DateTime(2026, 6, 5);
			DateTime today = DateTime.Today;
			TimeSpan difference = today - firstCommit;
			int daysSince = difference.Days;

			AnsiConsole.MarkupLine("[bold cyan]=== Calav Hash Scanner ===[/]");
			AnsiConsole.MarkupLine("Hash-based, cache-aware, multithreaded scanner.\n");

			if (args.Length == 0)
			{
				AnsiConsole.MarkupLine("[bold yellow]Usage:[/] CalavHashScanner <directory-to-scan>");
				return;
			}

			foreach (string arg in args)
			{
				if (arg == "--stats" || arg == "-s")
				{
					AnsiConsole.MarkupLine($"[cyan]CalAV has been fighting threats for:[/] [green]{daysSince}[/][cyan] days![/]");
					return;
				}
				if (arg == "--help" || arg == "-h")
				{
					AnsiConsole.MarkupLine("[bold yellow]Usage:[/] CalavHashScanner <directory-to-scan>");
					AnsiConsole.MarkupLine("[bold yellow]Options:[/]");
					AnsiConsole.MarkupLine("  [green]-s, --stats[/]  Show the number of days since the first commit.");
					AnsiConsole.MarkupLine("  [green]-h, --help[/]   Show this help message.");
					AnsiConsole.MarkupLine("  [green]-l, --license[/] Show license information.");
					return;
				}
				if (arg == "--license" || arg == "-l")
				{
					AnsiConsole.MarkupLine("[bold yellow]Calav is licensed under the MIT License.[/]");
					AnsiConsole.MarkupLine("See the LICENSE file in the project root or repository for full license information.");
					return;
				}
				if (arg == "--debug" || arg == "-d")
				{
					AnsiConsole.MarkupLine("[bold cyan]DEBUG MODE ACTIVATED[/]");
					debugMode = true;
				}
			}

			string directory = args[0];

			if (!Directory.Exists(directory))
			{
				AnsiConsole.MarkupLine($"[red]Directory not found:[/] {directory}");
				return;
			}

			string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string baseDir = Path.Combine(home, "calav", "hashing");
			Directory.CreateDirectory(baseDir);

			string cacheListPath = Path.Combine(baseDir, "hashes.txt");
			string cacheHashPath = Path.Combine(baseDir, "cachehash.txt");

			try
			{
				await CacheManager.EnsureHashListUpToDateAsync(HashListUrl, cacheListPath, cacheHashPath);

				var knownBad = CacheManager.LoadKnownBadHashes(cacheListPath);
				AnsiConsole.MarkupLine($"\nLoaded [green]{knownBad.Count}[/] known-bad hashes from cache.");
				AnsiConsole.MarkupLine($"Cache directory: [blue]{baseDir}[/]\n");

				Scanner.ScanDirectoryMultithreaded(directory, knownBad);
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine("[red bold]!!! AN EXCEPTION HAS BEEN THROWN !!![/]");
				AnsiConsole.WriteException(ex);
				AnsiConsole.MarkupLine("[red bold]!!! END OF EXCEPTION !!![/]");
				AnsiConsole.MarkupLine("\n[red]An error occurred during execution. Please check the details above.[/]");
			}
		}
	}
}
