// Calav is licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Text;
using Spectre.Console;
using CalavHashScanner.Utils;

namespace CalavHashScanner.Services
{
    public static class CacheManager
    {
        public static async Task EnsureHashListUpToDateAsync(string url, string cacheListPath, string cacheHashPath)
        {
            using var client = new HttpClient();

            bool cacheExists = File.Exists(cacheListPath) && File.Exists(cacheHashPath);

            if (!cacheExists)
            {
                AnsiConsole.MarkupLine("[yellow]No cache found. Downloading hash list for the first time...[/]");
                string text = await client.GetStringAsync(url);
                File.WriteAllText(cacheListPath, text, Encoding.UTF8);

                string hash = HashUtils.ComputeSha256String(text);
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
            string remoteHash = HashUtils.ComputeSha256String(remoteText);
            if (Program.debugMode) {
                AnsiConsole.MarkupLine($"[bold yellow]The hash of the remote is: {remoteHash}[/]");
                AnsiConsole.MarkupLine($"[bold yellow]The hash of local hashlist is {storedHash}[/]");
            }
            if (!string.Equals(storedHash, remoteHash, StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[yellow]The remote hashlist has changed, updating cache...[/]");

                File.WriteAllText(cacheListPath, remoteText, Encoding.UTF8);
                File.WriteAllText(cacheHashPath, remoteHash, Encoding.UTF8);
                AnsiConsole.MarkupLine("[green]Cache updated.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]Cache is up to date.[/]");
            }
        }

        public static HashSet<string> LoadKnownBadHashes(string path)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var reader = new StreamReader(path, Encoding.UTF8);
            string? line;
            Stopwatch Start = Stopwatch.StartNew();
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
            Start.Stop();
            long elapsedMs = Start.ElapsedMilliseconds;
            AnsiConsole.MarkupLine($"[green]Loaded {set.Count} known-bad hashes in {elapsedMs} ms.[/]");

            return set;
        }
    }
}
