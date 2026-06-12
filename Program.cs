using System.Security.Cryptography;
using System.Text;

using Spectre.Console;

namespace CalavHashScanner
{
    class Program
    {
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
            }

            string directory = args[0];

            if (!Directory.Exists(directory))
            {
                AnsiConsole.MarkupLine($"[red]Directory not found:[/] {directory}");
                return;
            }

            // Base cache directory: ~/calav/hashing
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string baseDir = Path.Combine(home, "calav", "hashing");
            Directory.CreateDirectory(baseDir);

            string cacheListPath = Path.Combine(baseDir, "hashes.txt");
            string cacheHashPath = Path.Combine(baseDir, "cachehash.txt");

            try
            {
                await EnsureHashListUpToDateAsync(HashListUrl, cacheListPath, cacheHashPath);

                var knownBad = LoadKnownBadHashes(cacheListPath);
                AnsiConsole.MarkupLine($"\nLoaded [green]{knownBad.Count}[/] known-bad hashes from cache.");
                AnsiConsole.MarkupLine($"Cache directory: [blue]{baseDir}[/]\n");

                ScanDirectoryMultithreaded(directory, knownBad);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red bold]!!! AN EXCEPTION HAS BEEN THROWN !!![/]");
                AnsiConsole.WriteException(ex);
                AnsiConsole.MarkupLine("[red bold]!!! END OF EXCEPTION !!![/]");
                AnsiConsole.MarkupLine("\n[red]An error occurred during execution. Please check the details above.[/]");
            }


        }
        // ---------- Cache management ----------

        private static async Task EnsureHashListUpToDateAsync(string url, string cacheListPath, string cacheHashPath)
        {
            using var client = new HttpClient();

            bool cacheExists = File.Exists(cacheListPath) && File.Exists(cacheHashPath);

            if (!cacheExists)
            {
                AnsiConsole.MarkupLine("[yellow]No cache found. Downloading hash list for the first time...[/]");
                string text = await client.GetStringAsync(url);
                File.WriteAllText(cacheListPath, text, Encoding.UTF8);

                string hash = ComputeSha256String(text);
                File.WriteAllText(cacheHashPath, hash, Encoding.UTF8);

                AnsiConsole.MarkupLine("[green]Initial cache created.[/]");
                return;
            }

            AnsiConsole.MarkupLine("Cache found. Checking for updates...");

            string storedHash = File.ReadAllText(cacheHashPath).Trim();
            string remoteText;
            try
            {
                remoteText = await client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching remote hash list:[/] {ex.Message}");
                return;
            }
            string remoteHash = ComputeSha256String(remoteText);

            if (!string.Equals(storedHash, remoteHash, StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[yellow]Remote hash list has changed. Updating cache...[/]");
                File.WriteAllText(cacheListPath, remoteText, Encoding.UTF8);
                File.WriteAllText(cacheHashPath, remoteHash, Encoding.UTF8);
                AnsiConsole.MarkupLine("[green]Cache updated.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]Cache is up to date.[/]");
            }
        }

        // ---------- Hash list loading ----------
        /// <summary>
        /// Loads known-bad hashes from the cache file into a HashSet for fast lookup,
        /// Uses case-insensitive comparison and ignores empty lines and comments (lines starting with #).
        /// Assumes each valid hash is a 64-character SHA256 hex string.
        /// IMPORTANT: The hash list file is expected to be well-formed. Malformed lines are ignored!
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static HashSet<string> LoadKnownBadHashes(string path)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var reader = new StreamReader(path, Encoding.UTF8);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue;
                
                if (line.Length == 64) // SHA256 hex length
                    set.Add(line);
                else
                    AnsiConsole.MarkupLine($"[yellow]Warning: Ignoring malformed line in hash list:[/] {line}");
            }

            return set;
        }

        // ---------- Directory scanning (multithreaded + Spectre progress) ----------

        private static void ScanDirectoryMultithreaded(string directory, HashSet<string> knownBad)
        {
            AnsiConsole.MarkupLine($"Scanning directory (multithreaded): [blue]{directory}[/]\n");

            var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();
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
            object lockObj = new object();
            List<string> threatPaths = new List<string>();

            AnsiConsole.Progress()
                .AutoClear(true)
                .Columns(
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new TaskDescriptionColumn())
                .Start(ctx =>
                {
                    var task = ctx.AddTask("[green]Scanning files[/]", maxValue: total);

                    Parallel.ForEach(
                        files,
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                        file =>
                        {
                            try
                            {
                                string hash = ComputeSha256File(file);

                                if (knownBad.Contains(hash))
                                {
                                    lock (lockObj)
                                    {
                                        threats++;
                                        threatPaths.Add(file);
                                    }
                                }
                            }
                            catch
                            {
                                AnsiConsole.MarkupLine($"[red]Error processing file:[/] {file}");
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
                AnsiConsole.MarkupLine("\n[bold red] !!!! THREATS FOUND !!!![/]");
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

        // ---------- Hash helpers ----------

        private static string ComputeSha256File(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(stream);
            return BytesToHex(hashBytes);
        }

        private static string ComputeSha256String(string text)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = sha.ComputeHash(bytes);
            return BytesToHex(hashBytes);
        }

        private static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
