namespace FictionChecker.Grammar;

/// <summary>
/// One family of commonly confused words, checked by one rule. A family has one or more groups: the base
/// words and, where they are confused in the same way, their other forms ("loses" and "looses").
/// </summary>
internal sealed record ConfusableFamily(string RuleId, string Name, string Explanation, string[][] Groups, string[] Flagged, string[] Accepted);

/// <summary>The confusable words the library knows, and the fixed list of tokens their neighbours are stored under.</summary>
internal static class ConfusableSets
{
    /// <summary>
    /// Neighbours that are not words of the lexicon: punctuation and the tails of contractions, as the
    /// Google Books data writes them. They are numbered after the lexicon's words, in this order.
    /// </summary>
    public static readonly string[] Specials =
        [",", ".", "?", "!", ";", ":", "-", "--", "\"", "'", "...", "(", ")", "'s", "'re", "'ll", "'d", "'ve", "'m"];

    /// <summary>
    /// How strong the evidence must be by default, in half doublings: 10 means the other word must be about
    /// 2^5 = 32 times as likely as the written one.
    /// </summary>
    public const int DefaultThreshold = 10;

    // Pairs that need stronger evidence, as "written>meant". The more often the written word is right in
    // ordinary text, the more chances it has to look wrong by accident, so the common words sit highest.
    // Each value is the strongest evidence seen against a correct word in the development books, plus a margin
    // of one doubling. Two are set by hand, lower than that: "loose" and "brake" as verbs ("I loose you",
    // "ere I brake my fast") turn up in deliberately old-fashioned prose, and a cut-off high enough to pass
    // every one of those would also pass "I don't want to loose you".
    private static readonly Dictionary<(string Written, string Meant), int> Thresholds = new()
    {
        [("accept", "except")] = 12,
        [("brake", "break")] = 16,
        [("breathe", "breath")] = 16,
        [("effected", "affected")] = 14,
        [("except", "accept")] = 16,
        [("it's", "its")] = 19,
        [("its", "it's")] = 16,
        [("lead", "led")] = 14,
        [("led", "lead")] = 12,
        [("loose", "lose")] = 21,
        [("loosing", "losing")] = 11,
        [("of", "off")] = 14,
        [("off", "of")] = 23,
        [("passed", "past")] = 12,
        [("past", "passed")] = 15,
        [("quiet", "quite")] = 17,
        [("than", "then")] = 22,
        [("their", "there")] = 18,
        [("their", "they're")] = 18,
        [("then", "than")] = 16,
        [("there", "their")] = 18,
        [("there", "they're")] = 12,
        [("they're", "their")] = 19,
        [("they're", "there")] = 18,
        [("to", "too")] = 21,
        [("too", "to")] = 24,
        [("wandered", "wondered")] = 12,
        [("wandering", "wondering")] = 15,
        [("who's", "whose")] = 12,
        [("wondering", "wandering")] = 16,
        [("you're", "your")] = 19,
        [("your", "you're")] = 17,
    };

    /// <summary>Whether a family's rule is on unless the options say otherwise.</summary>
    public static bool OnByDefault(string ruleId) => true;

    /// <summary>Whether the evidence that <paramref name="best"/> was meant is strong enough to flag <paramref name="written"/>.</summary>
    public static bool IsStrongEnough(string written, string best, ConfusableEvidence evidence) =>
        evidence.Total * 2 >= Thresholds.GetValueOrDefault((written, best), DefaultThreshold) * ConfusableData.Unit;

    /// <summary>Every family, in the order their rules are listed.</summary>
    public static readonly ConfusableFamily[] Families =
    [
        new(RuleIds.ConfusableTheirThere, "Their, there, they're",
            "\"Their\" means belonging to them. \"There\" is a place, or opens a statement (\"there is\"). \"They're\" is short for \"they are\".",
            [["their", "there", "they're"]],
            ["She went over their to look.", "Their was nobody in the house.", "I think there going to be late."],
            ["They took their coats and went there.", "They're over there with their friends."]),
        new(RuleIds.ConfusableItsIts, "Its, it's",
            "\"Its\" means belonging to it. \"It's\" is short for \"it is\" or \"it has\".",
            [["its", "it's"]],
            ["I think its going to rain.", "The dog wagged it's tail."],
            ["It's a long way to its nest.", "The house had lost its roof, and it's been empty since."]),
        new(RuleIds.ConfusableYourYoure, "Your, you're",
            "\"Your\" means belonging to you. \"You're\" is short for \"you are\".",
            [["your", "you're"]],
            ["I hope your going to tell me.", "Take off you're coat."],
            ["You're late, and your dinner is cold."]),
        new(RuleIds.ConfusableWhoseWhos, "Whose, who's",
            "\"Whose\" means belonging to whom. \"Who's\" is short for \"who is\" or \"who has\".",
            [["whose", "who's"]],
            ["I wonder whose going to tell him.", "She was a woman who's husband had left."],
            ["Who's there, and whose horse is that?"]),
        new(RuleIds.ConfusableThenThan, "Then, than",
            "\"Then\" is about time or what follows. \"Than\" compares (\"taller than\").",
            [["then", "than"]],
            ["She was taller then her brother.", "He looked at her and than he left."],
            ["She was taller than him then.", "He waited, then walked on, more slowly than before."]),
        new(RuleIds.ConfusableLoseLoose, "Lose, loose",
            "\"Lose\" is to no longer have, or to be beaten. \"Loose\" means not tight, or to set free.",
            [["lose", "loose"], ["loses", "looses"], ["losing", "loosing"]],
            ["I don't want to loose you.", "One button had come lose."],
            ["He did not want to lose the loose change in his pocket."]),
        new(RuleIds.ConfusableAffectEffect, "Affect, effect",
            "\"Affect\" is usually the verb (to change something). \"Effect\" is usually the noun (the result).",
            [["affect", "effect"], ["affects", "effects"], ["affected", "effected"], ["affecting", "effecting"]],
            ["The medicine had no affect on him.", "It did not effect her at all."],
            ["The effect was slight, and did not affect the others."]),
        new(RuleIds.ConfusableAcceptExcept, "Accept, except",
            "\"Accept\" is to take or agree to. \"Except\" means leaving out.",
            [["accept", "except"]],
            ["She could not except his offer.", "Everyone came accept for Tom."],
            ["Everyone except Tom was ready to accept the offer."]),
        new(RuleIds.ConfusableToToo, "To, too",
            "\"To\" shows direction or goes before a verb. \"Too\" means also, or more than is wanted (\"too hot\").",
            [["to", "too"]],
            ["It was to late to go back.", "She wanted too go home.", "He was to tired to argue."],
            ["It was too late to go to the house.", "She wanted to come too."]),
        new(RuleIds.ConfusableOfOff, "Of, off",
            "\"Of\" shows belonging or what something is made of. \"Off\" means away from, or not on.",
            [["of", "off"]],
            ["He walked of without a word.", "She got out off the car.", "He turned of the light."],
            ["He took off his hat at the end of the day."]),
        new(RuleIds.ConfusableLeadLed, "Lead, led",
            "\"Led\" is the past of \"lead\" (\"she led the way\"). \"Lead\" is the present, or the metal.",
            [["lead", "led"]],
            ["He lead her into the room.", "This would led to trouble."],
            ["She led the way, and the path would lead them home."]),
        new(RuleIds.ConfusablePastPassed, "Past, passed",
            "\"Passed\" is the verb (\"she passed the house\"). \"Past\" is a time before now, or means beyond (\"walked past\").",
            [["past", "passed"]],
            ["She walked passed the house.", "The years had past quickly."],
            ["She passed the house and walked past the church."]),
        new(RuleIds.ConfusableBreathBreathe, "Breath, breathe",
            "\"Breath\" is the noun (\"a deep breath\"). \"Breathe\" is the verb (\"to breathe\").",
            [["breath", "breathe"], ["breaths", "breathes"]],
            ["She could hardly breath.", "He took a deep breathe."],
            ["He took a breath and tried to breathe slowly."]),
        new(RuleIds.ConfusableQuiteQuiet, "Quite, quiet",
            "\"Quite\" means fairly or completely. \"Quiet\" means making little noise.",
            [["quite", "quiet"]],
            ["She was quiet sure of it.", "The house was very quite."],
            ["The house was quite quiet."]),
        new(RuleIds.ConfusablePeekPeak, "Peek, peak, pique",
            "\"Peek\" is a quick look. \"Peak\" is the top. \"Pique\" is to stir up (interest) or a feeling of hurt pride.",
            [["peek", "peak", "pique"], ["peeks", "peaks"], ["peeked", "peaked", "piqued"]],
            ["She took a peak through the curtains.", "They climbed to the peek of the mountain."],
            ["She took a peek at the peak, and it piqued her interest."]),
        new(RuleIds.ConfusableWanderWonder, "Wander, wonder",
            "\"Wander\" is to walk about with no aim. \"Wonder\" is to ask yourself, or a feeling of amazement.",
            [["wander", "wonder"], ["wanders", "wonders"], ["wandered", "wondered"], ["wandering", "wondering"]],
            ["I wander if she knows.", "He wondered through the streets for hours."],
            ["I wonder where they wander at night."]),
        new(RuleIds.ConfusableDesertDessert, "Desert, dessert",
            "\"Desert\" is dry empty land, or to abandon. \"Dessert\" is the sweet course of a meal.",
            [["desert", "dessert"], ["deserts", "desserts"]],
            ["She ordered desert.", "They were lost in the dessert sands."],
            ["They ate dessert before crossing the desert."]),
        new(RuleIds.ConfusableBrakeBreak, "Brake, break",
            "\"Brake\" is what slows a vehicle. \"Break\" is to damage, to stop for a rest, or a gap.",
            [["brake", "break"], ["brakes", "breaks"], ["braking", "breaking"]],
            ["She was afraid it would brake.", "The breaks failed on the hill."],
            ["He hit the brake before anything could break."]),
        new(RuleIds.ConfusablePrincipalPrinciple, "Principal, principle",
            "\"Principal\" means main, or the head of a school. \"Principle\" is a rule or belief.",
            [["principal", "principle"], ["principals", "principles"]],
            ["Their principle concern was money.", "The principle reason was money."],
            ["The principal reason was a matter of principle."]),
        new(RuleIds.ConfusableComplimentComplement, "Compliment, complement",
            "\"Compliment\" is praise. \"Complement\" is something that completes or goes well with another.",
            [["compliment", "complement"], ["compliments", "complements"], ["complimented", "complemented"]],
            ["She paid him a complement.", "The ship had its full compliment of men."],
            ["He paid her a compliment on how the wine complemented the meal."]),
    ];
}
