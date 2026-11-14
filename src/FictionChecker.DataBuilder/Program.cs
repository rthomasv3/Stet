// Turns the recorded sources (see SOURCES.md) into the compact data files embedded in the library.
//
//   ngram     stream Google Books fiction 1-grams into downloads/ngram/fiction-1grams.tsv (run once, ~1 GB)
//   lexicon   build src/FictionChecker/Data/lexicon.bin from SCOWL and the 1-gram table
//
// Raw downloads go to the ignored downloads/ folder and are never committed.
using FictionChecker.DataBuilder;

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
    case "lexicon":
        {
            if (!File.Exists(ngramTable))
            {
                Console.Error.WriteLine("Run the 'ngram' command first.");
                return 1;
            }

            List<LexiconEntry> entries = await LexiconBuilder.BuildAsync(downloads, ngramTable, Path.Combine(root, "src", "FictionChecker.DataBuilder", "Lists"));
            byte[] data = LexiconWriter.Write(entries, Sources.DialectNames.Length);
            string output = Path.Combine(root, "src", "FictionChecker", "Data", "lexicon.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllBytes(output, data);
            int allDialects = entries.Count(e => e.Dialects == 0b11111);
            Console.WriteLine($"Wrote {entries.Count:N0} words ({allDialects:N0} in every dialect, {entries.Count(e => e.Count == 0):N0} without a count, {entries.Count(e => e.NoSuggest):N0} never suggested, {entries.Count(e => e.LargeOnly):N0} rarer), {data.Length:N0} bytes.");
            return 0;
        }

    default:
        Console.WriteLine("Commands: ngram, lexicon");
        return command == "help" ? 0 : 1;
}

static string FindRoot()
{
    string? dir = Directory.GetCurrentDirectory();
    while (dir is not null && !File.Exists(Path.Combine(dir, "FictionChecker.slnx")))
    {
        dir = Path.GetDirectoryName(dir);
    }

    return dir ?? throw new DirectoryNotFoundException("Run from inside the repository.");
}
