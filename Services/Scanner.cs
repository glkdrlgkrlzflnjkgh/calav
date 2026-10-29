// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.
using Spectre.Console;
using CalavHashScanner.Utils;
using System.IO;
using System.Diagnostics;

namespace CalavHashScanner.Services
{
    public static class Scanner
    {
		//TODO: Improve speed of enumeration
		public static void ScanDirectoryMultithreaded(string directory, HashSet<string> knownBad)
        {
            AnsiConsole.MarkupLine($"Scanning directory (multithreaded): [blue]{directory}[/]\n");
			
            List<string> files = new List<string>();
			Stopwatch enumsw = Stopwatch.StartNew();
			
			try
			{
				files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine("[red]Error enumerating files![/]");
				AnsiConsole.WriteException(ex);
				return;

			}
            AnsiConsole.MarkupLine("[green]Enumerating files...[/]");
            int total = files.Count;
            enumsw.Stop();
			AnsiConsole.MarkupLine($"Found [green]{total}[/] files, taking {(int)enumsw.ElapsedMilliseconds}ms!\n");

            if (total == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Nothing here![/]");
                return;
            }

            int processed = 0;
            int threats = 0;
			int uiInterval = 5;
			int maxParallel = Environment.ProcessorCount;
			if (maxParallel == 1)
			{
				AnsiConsole.MarkupLine("[yellow]WARNING: You only have ONE THREAD! The scan will be single-threaded and will be VERY SLOW!![/]");
			}
            object lockObj = new object();
            List<string> threatPaths = new List<string>();
			AnsiConsole.MarkupLine($"Using [blue]{maxParallel}[/] threads!");
            AnsiConsole.Progress()
                .AutoClear(true)
                .Columns(
                    new TaskDescriptionColumn(),
					new PercentageColumn(),
					new SpinnerColumn()
                    )
                .Start(ctx =>
                {
                    var task = ctx.AddTask("[green]Scanning files:[/]", maxValue: total);
					

                    Parallel.ForEach(
                        files,
                        new ParallelOptions { MaxDegreeOfParallelism = maxParallel, TaskScheduler = TaskScheduler.Default }, //FIXME: If the builtin scheduler does NOT impliment work-stealing, we should switch to a library that provides a scheduler that does.
                        file =>
                        {
                            try
                            {
                                string hash = HashUtils.ComputeSha256File(file);

                                if (knownBad.Contains(hash))
                                {
                                    lock (lockObj)
                                    {
                                        AnsiConsole.MarkupLine($"[red]THREAT FOUND! ({Markup.Escape(Path.GetFileName(file))}) (known-bad hash: {hash})[/]");
										threats++;
                                        threatPaths.Add(file);
                                    }
								}
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error processing file:[/] {file} Exception: {ex.Message}");
                                return;
                            }
                            finally
                            {
                                int done = Interlocked.Increment(ref processed);
                                task.Increment(1);
									task.Description = $"Scanning... ({processed}/{total}) ({Markup.Escape(Path.GetFileName(file))})";
                            }
                        });
                });

            AnsiConsole.MarkupLine("\n[bold cyan]=== Scan Complete ===[/]");
            AnsiConsole.MarkupLine($"Files scanned : [green]{total}[/]");
            AnsiConsole.MarkupLine($"Threats found : [red]{threats}[/]");

            if (threats > 0)
            {
                AnsiConsole.MarkupLine("\n[bold red]!!!! THREATS FOUND !!!![/]");
                AnsiConsole.MarkupLine("[yellow]Please review the following file paths for potential threats:[/]");

                foreach (var t in threatPaths)
                    AnsiConsole.MarkupLine($" - [bold red]{t}[/]");
                AnsiConsole.MarkupLine("[red]Note that these may be false positives.[/]");
                
            }
            else
            {
                AnsiConsole.MarkupLine("[green]No known threats detected! :)[/]");
                AnsiConsole.MarkupLine("[green]However, always stay vigilant and keep your software up to date![/]");
                AnsiConsole.MarkupLine("[green]Keep up the good work![/]");
            }
        }
    }
}
