// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.
using Spectre.Console;
using CalavHashScanner.Utils;
namespace CalavHashScanner.Services
{
    public static class Scanner
    {
		public static void ScanDirectoryMultithreaded(string directory, HashSet<string> knownBad)
        {
            AnsiConsole.MarkupLine($"Scanning directory (multithreaded): [blue]{directory}[/]\n");
			
            List<string> files = new List<string>();
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
            AnsiConsole.MarkupLine($"Found [green]{total}[/] files.\n");

            if (total == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Nothing to scan.[/]");
                return;
            }

            int processed = 0;
            int threats = 0;
			int maxParallel = Environment.ProcessorCount;
            object lockObj = new object();
            List<string> threatPaths = new List<string>();
			AnsiConsole.MarkupLine($"Using [blue]{maxParallel.ToString()}[/] threads!");
            AnsiConsole.Progress()
                .AutoClear(true)
                .Columns(
                    new TaskDescriptionColumn(),
					new PercentageColumn(),
					new SpinnerColumn(),
					new RemainingTimeColumn(),
					new ElapsedTimeColumn()
                    )
                .Start(ctx =>
                {
                    var task = ctx.AddTask("[green]Scanning files:[/]", maxValue: total);
					

                    Parallel.ForEach(
                        files,
                        new ParallelOptions { MaxDegreeOfParallelism = maxParallel },
                        file =>
                        {
                            try
                            {
                                string hash = HashUtils.ComputeSha256File(file);

                                if (knownBad.Contains(hash))
                                {
                                    lock (lockObj)
                                    {
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
                AnsiConsole.MarkupLine("\n[red]Please take appropriate action to investigate and mitigate these threats![/]");
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
