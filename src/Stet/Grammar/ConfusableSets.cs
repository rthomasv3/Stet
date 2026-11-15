namespace Stet.Grammar;

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

    /// <summary>
    /// The default for the families from "allowed, aloud" on. Their words are rarer in the development books,
    /// so those books say less about how often a correct use looks wrong; the bar is set one doubling higher.
    /// </summary>
    public const int RarerThreshold = 12;

    private const int FirstRarerFamily = 27;

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
        [("bare", "bear")] = 20,
        [("bear", "bare")] = 16,
        [("hear", "here")] = 19,
        [("here", "hear")] = 14,
        [("know", "no")] = 22,
        [("peace", "piece")] = 18,
        [("piece", "peace")] = 19,
        [("weather", "whether")] = 18,
        [("week", "weak")] = 15,
        [("bored", "board")] = 14,
        [("plain", "plane")] = 18,
        [("rains", "reins")] = 16,
        [("right", "write")] = 22,
        [("sole", "soul")] = 16,
        [("soles", "souls")] = 14,
        [("wait", "weight")] = 19,
        [("weight", "wait")] = 13,
        [("whole", "hole")] = 14,
        [("write", "right")] = 16,

        // "rein", "reign" and "rain": a correct word here looked wrong three times in one book that was not
        // used for tuning, so every direction is held to the higher bar.
        [("rein", "reign")] = 16, [("rein", "rain")] = 16, [("reign", "rein")] = 16, [("reign", "rain")] = 16, [("rain", "rein")] = 16, [("rain", "reign")] = 16,
        [("reins", "reigns")] = 16, [("reins", "rains")] = 16, [("reigns", "reins")] = 16, [("reigns", "rains")] = 16, [("rains", "reigns")] = 16,
    };

    /// <summary>The number of the family a lowercase word belongs to, or -1.</summary>
    public static int FamilyOf(string lower) => FamilyByWord.GetValueOrDefault(lower, -1);

    private static Dictionary<string, int> BuildIndex()
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int f = 0; f < Families.Length; f++)
        {
            foreach (string[] group in Families[f].Groups)
            {
                foreach (string word in group)
                {
                    index[word] = f;
                }
            }
        }

        return index;
    }

    /// <summary>Whether a family's rule is on unless the options say otherwise.</summary>
    public static bool OnByDefault(string ruleId) => true;

    /// <summary>Whether the evidence that <paramref name="best"/> was meant is strong enough to flag <paramref name="written"/>.</summary>
    public static bool IsStrongEnough(string written, string best, ConfusableEvidence evidence) =>
        evidence.Total * 2 >= Thresholds.GetValueOrDefault((written, best), FamilyOf(written) >= FirstRarerFamily ? RarerThreshold : DefaultThreshold) * ConfusableData.Unit;

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
            ["I think its going to rain.", "The dog wagged it's tail.", "The cat licked it's paw and looked away."],
            ["It's a long way to its nest.", "The house had lost its roof, and it's been empty since."]),
        new(RuleIds.ConfusableYourYoure, "Your, you're",
            "\"Your\" means belonging to you. \"You're\" is short for \"you are\".",
            [["your", "you're"]],
            ["I hope your going to tell me.", "Take off you're coat.", "Your not going to believe this."],
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
        new(RuleIds.ConfusableWhetherWeather, "Whether, weather",
            "\"Whether\" introduces a choice or a doubt (\"whether he will come\"). \"Weather\" is rain, wind and sun.",
            [["whether", "weather"]],
            ["I don't know weather he will come."],
            ["Whether the weather holds or not, we sail."]),
        new(RuleIds.ConfusableHereHear, "Here, hear",
            "\"Here\" is this place. \"Hear\" is what ears do.",
            [["here", "hear"]],
            ["Come over hear and look at this."],
            ["I can hear you from here."]),
        new(RuleIds.ConfusableBearBare, "Bear, bare",
            "\"Bear\" is to carry or put up with, or the animal. \"Bare\" means uncovered.",
            [["bear", "bare"], ["bears", "bares"]],
            ["She could not bare to watch."],
            ["He could not bear the cold on his bare hands."]),
        new(RuleIds.ConfusableAdviceAdvise, "Advice, advise",
            "\"Advice\" is the noun (\"good advice\"). \"Advise\" is the verb (\"to advise\").",
            [["advice", "advise"]],
            ["He gave her some good advise about the road."],
            ["I advise you to take her advice."]),
        new(RuleIds.ConfusableWeekWeak, "Week, weak",
            "\"Week\" is seven days. \"Weak\" means not strong.",
            [["week", "weak"]],
            ["I saw him last weak at the market."],
            ["He was weak for a week after the fever."]),
        new(RuleIds.ConfusableKnowNo, "Know, no",
            "\"Know\" is to be sure of something. \"No\" is the opposite of yes, or means not any.",
            [["know", "no"]],
            ["There was know one in the house."],
            ["I know there is no way back."]),
        new(RuleIds.ConfusablePiecePeace, "Piece, peace",
            "\"Piece\" is a part of something. \"Peace\" is calm, or the absence of war.",
            [["piece", "peace"]],
            ["She tore off a peace of cloth.", "They lived in piece for years."],
            ["She ate a piece of bread in peace.", "At last she found a peace of mind she had not known."]),
        new(RuleIds.ConfusableAllowedAloud, "Allowed, aloud",
            "\"Allowed\" means permitted. \"Aloud\" means spoken so it can be heard.",
            [["allowed", "aloud"]],
            ["She was aloud to stay up late."],
            ["He was allowed to read aloud."]),
        new(RuleIds.ConfusableThroneThrown, "Throne, thrown",
            "\"Throne\" is a ruler's seat. \"Thrown\" is the past of \"throw\".",
            [["throne", "thrown"]],
            ["The king sat on his thrown and said nothing."],
            ["He was thrown down before the throne."]),
        new(RuleIds.ConfusableStakeSteak, "Stake, steak",
            "\"Stake\" is a pointed post, or what is risked. \"Steak\" is a cut of meat.",
            [["stake", "steak"], ["stakes", "steaks"]],
            ["He cut into the juicy stake."],
            ["He drove a stake into the ground and went in for his steak."]),
        new(RuleIds.ConfusableWaistWaste, "Waist, waste",
            "\"Waist\" is the middle of the body. \"Waste\" is to use badly, or what is thrown away.",
            [["waist", "waste"]],
            ["It was a waist of time."],
            ["She tied the rope round her waist and did not waste a moment."]),
        new(RuleIds.ConfusableBoredBoard, "Bored, board",
            "\"Bored\" means weary of something dull. \"Board\" is a plank, or to get on a ship.",
            [["bored", "board"]],
            ["They went on bored the ship."],
            ["Bored, he tapped the board with one finger."]),
        new(RuleIds.ConfusableWholeHole, "Whole, hole",
            "\"Whole\" means all of something. \"Hole\" is a gap or hollow.",
            [["whole", "hole"]],
            ["She ate the hole thing."],
            ["The whole floor fell into the hole."]),
        new(RuleIds.ConfusableWeightWait, "Weight, wait",
            "\"Weight\" is how heavy something is. \"Wait\" is to stay until something happens.",
            [["weight", "wait"]],
            ["I cannot weight any longer."],
            ["Wait until you feel the weight of it."]),
        new(RuleIds.ConfusableWriteRight, "Write, right",
            "\"Write\" is to put words down. \"Right\" means correct, or the opposite of left.",
            [["write", "right"]],
            ["You were write about him.", "Turn write at the mill."],
            ["You were right to write to her."]),
        new(RuleIds.ConfusablePlainPlane, "Plain, plane",
            "\"Plain\" means simple, or flat open land. \"Plane\" is an aircraft, a flat surface or a tool.",
            [["plain", "plane"], ["plains", "planes"]],
            ["The plain landed at dawn."],
            ["The plane crossed the plain in an hour."]),
        new(RuleIds.ConfusableRoleRoll, "Role, roll",
            "\"Role\" is a part played. \"Roll\" is to turn over, or a small loaf.",
            [["role", "roll"], ["roles", "rolls"]],
            ["She played the leading roll."],
            ["His role was to roll the barrels out."]),
        new(RuleIds.ConfusableSightSite, "Sight, site",
            "\"Sight\" is seeing, or something seen. \"Site\" is a place.",
            [["sight", "site"], ["sights", "sites"]],
            ["He caught site of her in the crowd."],
            ["The site of the battle was a grim sight."]),
        new(RuleIds.ConfusableSoulSole, "Soul, sole",
            "\"Soul\" is the spirit. \"Sole\" means only, or the bottom of a foot.",
            [["soul", "sole"], ["souls", "soles"]],
            ["There was not a sole in sight."],
            ["The sole survivor prayed for his soul."]),
        new(RuleIds.ConfusableTailTale, "Tail, tale",
            "\"Tail\" is the end of an animal. \"Tale\" is a story.",
            [["tail", "tale"], ["tails", "tales"]],
            ["The fox had a bushy tale."],
            ["It was a tale about a fox with no tail."]),
        new(RuleIds.ConfusableStealSteel, "Steal, steel",
            "\"Steal\" is to take what is not yours. \"Steel\" is the metal.",
            [["steal", "steel"]],
            ["He tried to steel a horse."],
            ["He would not steal the steel sword."]),
        new(RuleIds.ConfusableHealHeel, "Heal, heel",
            "\"Heal\" is to make or get well. \"Heel\" is the back of the foot.",
            [["heal", "heel"], ["heals", "heels"]],
            ["The wound would not heel."],
            ["His heel would heal in time."]),
        new(RuleIds.ConfusablePourPore, "Pour, pore",
            "\"Pour\" is to tip liquid out. \"Pore\" is to study closely (\"pore over\"), or a tiny opening in skin.",
            [["pour", "pore"], ["poured", "pored"], ["pouring", "poring"]],
            ["She pored the wine into his cup."],
            ["He poured a drink and pored over the map."]),
        new(RuleIds.ConfusableReinReign, "Rein, reign, rain",
            "\"Rein\" is a strap for guiding a horse. \"Reign\" is a ruler's time in power. \"Rain\" is water from the sky.",
            [["rein", "reign", "rain"], ["reins", "reigns", "rains"]],
            ["The rein fell all night."],
            ["In the rain he took the reins, in the last year of her reign."]),
    ];

    // Declared last: it is built from the list above, and static fields are set in the order written.
    private static readonly Dictionary<string, int> FamilyByWord = BuildIndex();
}
