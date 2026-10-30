// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.
using Spectre.Console;
using CalavHashScanner.Util;
using CalavHashScanner.utils;
using System.Diagnostics;
using CalavHashScanner.CustomSpectre;
using CalavScanner.utils;
namespace CalavHashScanner.Services;

public static class Scanner
{
    /// <summary>
    /// Scans a directory in parallel threads, reporting any threats and providing detailed console output.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="knownBad"></param>
    public static void ScanDirectoryMultithreaded(string directory, HashSet<string> knownBad)
    {
        AnsiConsole.MarkupLine($"Scanning directory (multithreaded): [blue]{directory}[/]\n");

        List<string> files = [];
        Stopwatch enumsw = Stopwatch.StartNew();

        try
        {
            AnsiConsole.Status()
            .Spinner(Spinner.Known.SimpleDotsScrolling)
            .Start("Enumerating files", _ =>
            {
                files = [.. Directory.EnumerateFiles(
                    directory,
                    "*",
                    SearchOption.AllDirectories
                )];
            });
        }
        catch
        {
            AnsiConsole.MarkupLine("[red]Error enumerating files![/]");
            throw; // rethrow, caller should handle.

        }

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

        int maxParallel = Environment.ProcessorCount;
        if (maxParallel < 4 || Program.testCores)
        {
            Prompt p = new("You have less than 4 cores available, and continuing could cause **SEVERE** system slowdowns. Are you sure?");
            string r = p.Ask();
            if (r != "yes")
            {
                return;
            }
        }
        object lockObj = new();
        List<string> threatPaths = [];
        AnsiConsole.MarkupLine($"Using [blue]{maxParallel}[/] threads!");
        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(
                        new PercentageColumn(),
                        new SpinnerColumn(Spinner.Known.SimpleDotsScrolling),
                        new CalavProgressColumn(),
                        new TaskDescriptionColumn()
                )
            .Start(ctx =>
            {
                var task = ctx.AddTask("[green]Scanning files:[/]", maxValue: total);
                task.IsIndeterminate = false;
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
                        }
                        finally
                        {
                            int done = Interlocked.Increment(ref processed);
                            task.Increment(1);
                            lock (lockObj)
                            {
                                task.Description = $"Scanning - ({processed}/{total}) - {Markup.Escape(Path.GetFileName(file))}";
                            }  
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

            foreach (string t in threatPaths)
                AnsiConsole.MarkupLine($" - [bold red]{t}[/]");
            AnsiConsole.MarkupLine("[yellow]Note that these may be false positives.[/]");

        }
        else
        {
            AnsiConsole.MarkupLine("[green]No known threats detected! :)[/]");
            AnsiConsole.MarkupLine("[green]Keep up the good work, but stay vigilant![/]");
        }
    }
}
