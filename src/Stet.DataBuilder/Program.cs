// Turns the recorded sources (see SOURCES.md) into the compact data files embedded in the library.
//
//   ngram     stream Google Books fiction 1-grams into downloads/ngram/fiction-1grams.tsv (run once, ~1 GB)
//   bigrams   stream Google Books fiction 2-grams (47 files, ~31 GB) into downloads/ngram/2grams/ (run once)
//   wordinfo  build src/Stet/Data/wordinfo.bin (needs lexicon.bin, the 1-gram table and the 2-gram files)
//   bigrams-closed  second pass over the 2-grams: word types beside each function word (run once)
//   bigrams-all     another pass: keep every common pair of plain tokens as a local copy (run once, a few GB)
//   bigrams-more    neighbours of the confusable words in Lists/focus-more.txt, from the local copy (seconds)
//   bigrams-tagged  third pass over the 2-grams: what follows the 's that means "is" (run once)
//   confusables  build src/Stet/Data/confusables.bin (needs lexicon.bin and the 2-gram files)
//   lexicon   build src/Stet/Data/lexicon.bin from SCOWL and the 1-gram table
//
// Raw downloads go to the ignored downloads/ folder and are never committed.
using Stet.DataBuilder;

string root = FindRoot();
string downloads = Path.Combine(root, "downloads");
string ngramTable = Path.Combine(downloads, "ngram", "fiction-1grams.tsv");
string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "ngram":
        Directory.CreateDirectory(Path.GetDirectoryName(ngramTable)!);
        using (var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(90)))
        {
            await NgramExtractor.RunAsync(Sources.NgramFiction1Grams, ngramTable, timeout.Token);
        }

        return 0;
    case "bigrams":
        {
            var focus = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in File.ReadLines(Path.Combine(root, "src", "Stet.DataBuilder", "Lists", "focus.txt")))
            {
                if (!line.StartsWith('#'))
                {
                    focus.UnionWith(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromHours(3));
            await BigramExtractor.RunAsync(Path.Combine(downloads, "ngram", "2grams"), focus, false, args.Length > 1 ? int.Parse(args[1]) : 12, timeout.Token);
            return 0;
        }

    case "bigrams-all":
        {
            // One pass that keeps every pair of plain tokens seen 40 times or more since 1950 (a few GB, in the
            // ignored downloads folder). Word lists added later are then filled from this copy in seconds.
            using var timeout = new CancellationTokenSource(TimeSpan.FromHours(3));
            await BigramExtractor.RunAsync(Path.Combine(downloads, "ngram", "2grams-all"), new HashSet<string>(StringComparer.Ordinal), false, args.Length > 1 ? int.Parse(args[1]) : 12, timeout.Token, keepAll: true);
            return 0;
        }

    case "bigrams-more":
        {
            // Neighbours of the confusable words added after the first pass, taken from the local copy.
            var more = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in File.ReadLines(Path.Combine(root, "src", "Stet.DataBuilder", "Lists", "focus-more.txt")))
            {
                if (!line.StartsWith('#'))
                {
                    more.UnionWith(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
            }

            string all = Path.Combine(downloads, "ngram", "2grams-all");
            if (!Directory.Exists(all) || Directory.GetFiles(all, "*.tsv").Length < BigramExtractor.Files)
            {
                Console.Error.WriteLine("Run the 'bigrams-all' command first.");
                return 1;
            }

            string target = Path.Combine(downloads, "ngram", "2grams-more");
            Directory.CreateDirectory(target);
            long kept = 0;
            using (var writer = new StreamWriter(Path.Combine(target, "more.tsv.part")))
            {
                foreach (string file in Directory.GetFiles(all, "*.tsv").Order(StringComparer.Ordinal))
                {
                    foreach (string line in File.ReadLines(file))
                    {
                        int firstTab = line.IndexOf('\t');
                        int secondTab = line.IndexOf('\t', firstTab + 1);
                        if (firstTab <= 0 || secondTab <= 0)
                        {
                            continue;
                        }

                        if (more.Contains(line[..firstTab].ToLowerInvariant()) || more.Contains(line[(firstTab + 1)..secondTab].ToLowerInvariant()))
                        {
                            writer.WriteLine(line);
                            kept++;
                        }
                    }
                }
            }

            foreach (string old in Directory.GetFiles(target, "*.tsv").Concat(Directory.GetFiles(target, "*.sha256")))
            {
                File.Delete(old);
            }

            File.Move(Path.Combine(target, "more.tsv.part"), Path.Combine(target, "more.tsv"));
            Console.WriteLine($"Kept {kept:N0} pairs for {more.Count} words.");
            return 0;
        }

    case "bigrams-closed":
        {
            // The function words, from the list the library embeds.
            var closed = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in LexiconBuilder.ReadList(Path.Combine(root, "src", "Stet", "Data", "Lists", "closed.txt")))
            {
                closed.Add(line.Split(' ')[0]);
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromHours(3));
            await BigramExtractor.RunAsync(Path.Combine(downloads, "ngram", "2grams-closed"), closed, true, args.Length > 1 ? int.Parse(args[1]) : 12, timeout.Token);
            return 0;
        }

    case "bigrams-tagged":
        {
            // What follows the 's that stands for "is" or "has", which the plain counts mix with the 's of "John's".
            using var timeout = new CancellationTokenSource(TimeSpan.FromHours(3));
            await BigramExtractor.RunAsync(Path.Combine(downloads, "ngram", "2grams-tagged"), new HashSet<string>(StringComparer.Ordinal), false, args.Length > 1 ? int.Parse(args[1]) : 12, timeout.Token, ["'s_VERB"]);
            return 0;
        }

    case "lexicon":
        {
            if (!File.Exists(ngramTable))
            {
                Console.Error.WriteLine("Run the 'ngram' command first.");
                return 1;
            }

            List<LexiconEntry> entries = await LexiconBuilder.BuildAsync(downloads, ngramTable, Path.Combine(root, "src", "Stet.DataBuilder", "Lists"));
            byte[] data = LexiconWriter.Write(entries, Sources.DialectNames.Length);
            string output = Path.Combine(root, "src", "Stet", "Data", "lexicon.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllBytes(output, data);
            int allDialects = entries.Count(e => e.Dialects == 0b11111);
            Console.WriteLine($"Wrote {entries.Count:N0} words ({allDialects:N0} in every dialect, {entries.Count(e => e.Count == 0):N0} without a count, {entries.Count(e => e.NoSuggest):N0} never suggested, {entries.Count(e => e.LargeOnly):N0} rarer), {data.Length:N0} bytes.");
            return 0;
        }

    case "wordinfo":
        {
            string data = Path.Combine(root, "src", "Stet", "Data");
            var lexicon = Stet.Spelling.Lexicon.Read(File.ReadAllBytes(Path.Combine(data, "lexicon.bin")));
            string cmudict = await LexiconBuilder.FetchAsync(Sources.CmuDict, Path.Combine(downloads, "cmudict"));
            var builder = new WordInfoBuilder(lexicon);
            byte[] bytes = builder.Build(ngramTable, Path.Combine(downloads, "ngram", "2grams"), Path.Combine(downloads, "ngram", "2grams-closed"), cmudict, Path.Combine(root, "src", "Stet.DataBuilder", "Lists", "irregular.txt"));
            File.WriteAllBytes(Path.Combine(data, "wordinfo.bin"), bytes);
            File.WriteAllLines(Path.Combine(downloads, "wordinfo-links.local.tsv"), builder.Links.Select(l => $"{l.Word}\t{l.Base}\t{l.Forms}"));
            foreach ((string name, long value) in builder.Stats)
            {
                Console.WriteLine($"  {name}: {value:N0}");
            }

            Console.WriteLine($"Wrote wordinfo.bin, {bytes.Length:N0} bytes.");
            return 0;
        }

    case "confusables":
        {
            string data = Path.Combine(root, "src", "Stet", "Data");
            var lexicon = Stet.Spelling.Lexicon.Read(File.ReadAllBytes(Path.Combine(data, "lexicon.bin")));
            var stats = new List<(string Name, long Value)>();
            // Neighbours seen fewer than 200 times with a whole group are left out: measured on the evaluation
            // books, keeping them (2.4 MB against 1.4 MB) catches no more errors and raises no fewer false alarms.
            long minTotal = args.Length > 1 ? long.Parse(args[1]) : 200;
            var firstPass = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in File.ReadLines(Path.Combine(root, "src", "Stet.DataBuilder", "Lists", "focus.txt")))
            {
                if (!line.StartsWith('#'))
                {
                    firstPass.UnionWith(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
            }

            byte[] bytes = ConfusableBuilder.Build(lexicon, Path.Combine(downloads, "ngram", "2grams"), Path.Combine(downloads, "ngram", "2grams-tagged"), Path.Combine(downloads, "ngram", "2grams-more"), firstPass, minTotal, stats);
            File.WriteAllBytes(args.Length > 2 ? args[2] : Path.Combine(data, "confusables.bin"), bytes);
            File.WriteAllLines(Path.Combine(downloads, "confusables-stats.local.tsv"), stats.Select(s => $"{s.Name}\t{s.Value}"));
            Console.WriteLine($"Wrote confusables.bin, {bytes.Length:N0} bytes, {stats[^1].Value:N0} neighbour entries.");
            return 0;
        }

    default:
        Console.WriteLine("Commands: ngram, bigrams, lexicon, wordinfo");
        return command == "help" ? 0 : 1;
}

static string FindRoot()
{
    string? dir = Directory.GetCurrentDirectory();
    while (dir is not null && !File.Exists(Path.Combine(dir, "Stet.slnx")))
    {
        dir = Path.GetDirectoryName(dir);
    }

    return dir ?? throw new DirectoryNotFoundException("Run from inside the repository.");
}
