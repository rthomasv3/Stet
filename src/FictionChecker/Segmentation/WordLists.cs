using System.Collections.Frozen;

namespace FictionChecker.Segmentation;

/// <summary>
/// Small hand-written lists of closed-class facts the splitters need. All lowercase, apostrophes straight.
/// </summary>
internal static class WordLists
{
    /// <summary>Abbreviations whose full stop never ends a sentence (a name or number follows).</summary>
    public static readonly FrozenSet<string> TitleAbbreviations = Set(
        "mr", "mrs", "ms", "mx", "dr", "st", "ste", "prof", "capt", "cmdr", "col", "gen", "lt", "sgt", "cpl", "pvt",
        "maj", "adm", "rev", "fr", "hon", "gov", "sen", "rep", "pres", "supt", "insp", "det", "messrs", "mme", "mlle",
        "mt", "ft", "vs", "viz", "cf", "approx", "vol", "fig", "esp");

    /// <summary>Abbreviations that can end a sentence; their full stop only ends one when a capital follows.</summary>
    public static readonly FrozenSet<string> OtherAbbreviations = Set(
        "etc", "jr", "sr", "esq", "co", "ltd", "inc", "bros", "ave", "blvd", "rd", "dept", "a.m", "p.m", "am", "pm",
        "jan", "feb", "mar", "apr", "jun", "jul", "aug", "sept", "sep", "oct", "nov", "dec", "mon", "tues", "wed",
        "thurs", "fri", "sat", "sun", "oz", "lb", "lbs", "min", "sec", "hr", "hrs", "yr", "yrs", "ed", "eds", "pp");

    /// <summary>Words commonly written with a leading apostrophe for a dropped start ('tis, 'em, 'cause).</summary>
    public static readonly FrozenSet<string> LeadingApostropheWords = Set(
        "tis", "twas", "twere", "twill", "twould", "tisn't", "twasn't", "em", "im", "er", "e", "ere", "is", "ave",
        "ad", "as", "un", "cause", "cos", "coz", "bout", "til", "till", "round", "cept", "fraid", "neath", "gainst",
        "tween", "twixt", "pon", "fore", "course", "nother", "scuse", "specially", "stead", "nuff", "kay", "sup",
        "member", "less", "n", "nd", "d", "s", "m", "ll", "ve", "re", "ow", "ere's", "ome", "alf", "ell", "old", "ard",
        "orse", "ouse", "eart", "ead", "and", "ands", "appen", "elp", "igh", "imself", "erself", "isself", "ey",
        "allo", "ullo", "struth", "sblood", "sdeath", "zounds", "gad", "night", "morning", "mornin", "bye", "tain't",
        "taint", "tention", "bove", "long", "mid", "midst", "mongst", "way", "cross", "gain", "fess", "spect",
        "spose", "sposing", "pears", "stead", "cording", "nough", "most", "bus", "phone", "flu", "varsity", "copter",
        "gator", "possum", "coon", "shrooms", "burbs", "rents", "tude", "za");

    /// <summary>Short forms commonly written with a trailing apostrophe (o', th', an').</summary>
    public static readonly FrozenSet<string> TrailingApostropheWords = Set(
        "o", "th", "an", "ol", "lil", "li'l", "wi", "fo", "mo", "po", "ha", "gi", "i", "t", "d", "y", "ma", "pa",
        "comin", "goin", "doin", "nothin", "somethin", "anythin", "everythin", "mornin", "evenin", "darlin", "an'",
        "jus", "mos", "las", "tol", "ol'", "fer", "yo", "da", "de", "wha", "tha", "wit", "sho", "cap", "cap'n", "mis",
        "lef", "nex", "bes", "res", "fas", "pas", "han", "stan", "fin", "min", "kin", "kep", "slep", "wep", "roun",
        "groun", "aroun", "soun", "foun", "poun", "frien", "en", "sen", "len", "ben", "spen", "ole", "chil", "wil",
        "hol", "tol", "col", "sol", "worl", "gol", "fiel", "buil", "s'pose", "mus", "firs", "wors", "bein", "seein");

    /// <summary>
    /// Verbs that report speech (said, asked, whispered). They separate a dialogue tag from an action beat:
    /// <c>"Hello," she said.</c> is one sentence, <c>"Hello." She smiled.</c> is two.
    /// </summary>
    public static readonly FrozenSet<string> DialogueTagVerbs = Set(
        "said", "says", "say", "asked", "asks", "ask", "replied", "replies", "answered", "answers", "whispered",
        "whispers", "muttered", "mutters", "murmured", "murmurs", "mumbled", "mumbles", "shouted", "shouts", "yelled",
        "yells", "cried", "cries", "called", "calls", "screamed", "screams", "shrieked", "roared", "bellowed",
        "growled", "growls", "snarled", "snapped", "snaps", "hissed", "hisses", "barked", "added", "adds",
        "continued", "continues", "went", "began", "begins", "repeated", "repeats", "remarked", "observed",
        "exclaimed", "declared", "announced", "demanded", "demands", "inquired", "enquired", "queried", "wondered",
        "explained", "explains", "agreed", "agrees", "admitted", "admits", "confessed", "insisted", "insists",
        "protested", "objected", "interrupted", "interjected", "suggested", "offered", "promised", "warned",
        "ordered", "commanded", "begged", "pleaded", "urged", "advised", "reminded", "informed", "told", "tells",
        "stated", "noted", "commented", "responded", "responds", "retorted", "returned", "rejoined", "countered",
        "conceded", "corrected", "concluded", "finished", "persisted", "pressed", "prompted", "echoed", "quoted",
        "recited", "read", "sang", "chanted", "breathed", "sighed", "sighs", "groaned", "moaned", "gasped", "panted",
        "sobbed", "wailed", "whimpered", "whined", "grumbled", "complained", "drawled", "stammered", "stuttered",
        "faltered", "croaked", "rasped", "grunted", "huffed", "scoffed", "sneered", "jeered", "taunted", "teased",
        "joked", "quipped", "chuckled", "laughed", "giggled", "mused", "pondered", "reflected", "thought",
        "volunteered", "ventured", "guessed", "supposed", "affirmed", "assented", "acknowledged", "assured",
        "confirmed", "decided", "greeted", "hollered", "howled", "lied", "nodded", "pleads", "proclaimed", "purred",
        "reasoned", "recalled", "reported", "revealed", "soothed", "spat", "squeaked", "squealed", "swore",
        "thundered", "trilled", "uttered", "vowed", "wheezed", "yelped", "cut", "put", "chimed", "piped", "broke",
        "ejaculated", "quoth", "cooed", "crowed", "shot", "bawled", "blurted", "chided", "scolded", "started",
        "resumed", "pursued", "fumed", "raged", "simpered", "smirked", "marveled", "marvelled", "bade");

    private static FrozenSet<string> Set(params string[] words) => words.ToFrozenSet(StringComparer.Ordinal);
}
