// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.

using Spectre.Console;
using CalavHashScanner.Services;
using CalavHashScanner.utils;
namespace CalavHashScanner;

static class Program
{
    public static bool debugMode;
    // Remote hash list URL
    private const string HashListUrl =
        "https://raw.githubusercontent.com/romainmarcoux/malicious-hash/refs/heads/main/full-hash-sha256-aa.txt";

    /// <summary>
    /// The main entrypoint to the program... DUH!
    /// </summary>
    static async Task Main(string[] args)
    {
        Logging logger = new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/calav");


        DateTime firstCommit = new(2026, 6, 5);
        DateTime today = DateTime.Today;
        TimeSpan difference = today - firstCommit;
        int daysSince = difference.Days;

        AnsiConsole.MarkupLine("[bold green]=== Calav Hash Scanner ===[/]");
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
                AnsiConsole.MarkupLine("[bold cyan]Verbose/Debug mode active! Logs will be messier but better for debugging![/]");
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
        string baseDir = Path.Combine(home, "calav", "cache", "hashes");
        Directory.CreateDirectory(baseDir);

        string cacheListPath = Path.Combine(baseDir, "hashes.cache");
        string cacheHashPath = Path.Combine(baseDir, "cachehash.cache");

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
            CrashReport c = new("[red bold]Fatal exception![/]", ex);
            c.Render();
            // below line is commented out as CrashReport.Save is unimplimented and currently throws NotImplimentedException.
            //c.Save(cacheListPath);
            Environment.Exit(1);
        }
    }
}
