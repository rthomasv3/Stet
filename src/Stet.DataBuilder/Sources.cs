namespace Stet.DataBuilder;

/// <summary>One downloadable source file, pinned by version and checksum so the data can be rebuilt exactly.</summary>
internal sealed record SourceFile(string Name, string Url, string Sha256);

/// <summary>Every source the builder reads. Each is also recorded in SOURCES.md with its license.</summary>
internal static class Sources
{
    public const string ScowlVersion = "2026.02.25";

    private const string ScowlBase = "https://github.com/en-wl/wordlist/releases/download/rel-" + ScowlVersion + "/";

    /// <summary>SCOWL Hunspell dictionaries, normal size, one per dialect. Order matches <see cref="DialectNames"/>.</summary>
    public static readonly SourceFile[] Scowl =
    [
        new("hunspell-en_US-" + ScowlVersion + ".zip", ScowlBase + "hunspell-en_US-" + ScowlVersion + ".zip", "ac8e73310e951d88c52c2cf2ba54ceaca34f8486a81630ac8a75dc5f931179f9"),
        new("hunspell-en_GB-ise-" + ScowlVersion + ".zip", ScowlBase + "hunspell-en_GB-ise-" + ScowlVersion + ".zip", "d6fbb91ae7824c52fb02f74d7bc2cd9092f130faec60f42326a59437fa7247a3"),
        new("hunspell-en_GB-ize-" + ScowlVersion + ".zip", ScowlBase + "hunspell-en_GB-ize-" + ScowlVersion + ".zip", "c5bdd92fc1e21da7503939fe4139f36f28f01c18ac35e2fc90b526a1b7bfb099"),
        new("hunspell-en_CA-" + ScowlVersion + ".zip", ScowlBase + "hunspell-en_CA-" + ScowlVersion + ".zip", "29fe0050408c35614f22bea84338b4256962c89f5ef25dac5038325bcb86f424"),
        new("hunspell-en_AU-" + ScowlVersion + ".zip", ScowlBase + "hunspell-en_AU-" + ScowlVersion + ".zip", "f85471593b9744f64298c02d07fb62d58926bf81fb5e4552e2bac645bfa401e0"),
    ];

    /// <summary>
    /// SCOWL large dictionaries. They add rarer words (shorn, hewn, rainforest) that fiction uses freely.
    /// British large is one dictionary covering both -ise and -ize spellings.
    /// </summary>
    public static readonly SourceFile ScowlLargeUS = Large("en_US", "06ab5a2a12c29033f100988d3b0a5e53dcad40bf2473ccb78270619d1da99321");
    public static readonly SourceFile ScowlLargeGB = Large("en_GB", "704a49438a3a9d4f11cd76db433a6b230b9ce8770ee13fa28700052c4517ddc1");
    public static readonly SourceFile ScowlLargeCA = Large("en_CA", "b4b7bf4a7885de1c35fa1b3aa68317209afa58f04d60e8a07e124693485c59e3");
    public static readonly SourceFile ScowlLargeAU = Large("en_AU", "46a265883d1d01c889b7173ba1cabae0ca915aa403d6a2f93cee4cc661087db4");

    /// <summary>Dialects in the order of their bits in the lexicon.</summary>
    public static readonly string[] DialectNames = ["en_US", "en_GB-ise", "en_GB-ize", "en_CA", "en_AU"];

    private static SourceFile Large(string dialect, string sha256)
    {
        string name = "hunspell-" + dialect + "-large-" + ScowlVersion + ".zip";
        return new SourceFile(name, ScowlBase + name, sha256);
    }

    /// <summary>Google Books Ngram, English Fiction, version 20200217, 1-grams (about 0.94 GB, streamed).</summary>
    public static readonly SourceFile NgramFiction1Grams = new(
        "eng-fiction-1-00000-of-00001.gz",
        "http://storage.googleapis.com/books/ngrams/books/20200217/eng-fiction/1-00000-of-00001.gz",
        "8640fb65e15484592665237df129de132032e25354d5f984f61c32100d901461");

    /// <summary>The CMU Pronouncing Dictionary, at a fixed commit of its repository.</summary>
    public static readonly SourceFile CmuDict = new(
        "cmudict.dict",
        "https://raw.githubusercontent.com/cmusphinx/cmudict/74790861f652b15e4ac49015a90074ad62a27690/cmudict.dict",
        "81917843c7f44ce2b094ac63873c2c7a4cf802040792c455ba3ca406891c3d22");
}
