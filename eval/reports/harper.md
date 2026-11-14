# Evaluation: harper

Library 0.1.0. Seed 20261114, 30,000 clean words per book, 500 planted errors per category for each corpus and split.

No earlier run to compare with.

## False alarms on clean text

Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change | Confirmed real errors | Not checked |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 454,679 | 20,102 | 44.211 |  | 0 | 0 |
| calibre | test | 8 | 240,253 | 9,842 | 40.965 |  | 0 | 0 |
| gutenberg | dev | 29 | 859,568 | 22,180 | 25.804 |  | 0 | 0 |
| gutenberg | test | 11 | 304,864 | 6,596 | 21.636 |  | 0 | 0 |
| all | dev | 45 | 1,314,247 | 42,282 | 32.172 |  | 0 | 0 |
| all | test | 19 | 545,117 | 16,438 | 30.155 |  | 0 | 0 |

## False alarms per rule

Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05.

| Rule | Category | Dev | Test | Change (dev) |
|---|---|---:|---:|---:|
| harper.SpellCheck | Spelling | 22.686 | 21.225 |  |
| harper.LongSentences | Readability | 2.621 | 3.029 |  |
| harper.Dashes | Formatting | 1.733 | 1.099 |  |
| harper.SplitWords | Typo | 0.75 | 0.976 |  |
| harper.UnclosedQuotes | Formatting | 0.473 | 0.565 |  |
| harper.Spaces | Formatting | 0.438 | 0.053 |  |
| harper.SentenceCapitalization | Capitalization | 0.425 | 0.257 |  |
| harper.OxfordComma | Style | 0.306 | 0.295 |  |
| harper.PronounVerbAgreement | Agreement | 0.296 | 0.211 |  |
| harper.UseEllipsisCharacter | Formatting | 0.268 | 0.081 |  |
| harper.MassNouns | Agreement | 0.235 | 0.213 |  |
| harper.CompoundNouns | WordChoice | 0.226 | 0.235 |  |
| harper.MissingPreposition | Miscellaneous | 0.149 | 0.152 |  |
| harper.DisjointPrefixes | Spelling | 0.108 | 0.108 |  |
| harper.AnA | Miscellaneous | 0.107 | 0.072 |  |
| harper.EllipsisLength | Formatting | 0.107 | 0.042 |  |
| harper.DiscourseMarkers | Punctuation | 0.102 | 0.083 |  |
| harper.AvoidCurses | WordChoice | 0.09 | 0.239 |  |
| harper.PossessiveYour | WordChoice | 0.061 | 0.053 |  |
| harper.QuantifierNumeralConflict | Grammar | 0.049 | 0.018 |  |
| harper.RepeatedWords | Repetition | 0.049 | 0.048 |  |
| harper.ToTwoToo | WordChoice | 0.039 | 0.029 |  |
| harper.MissingTo | WordChoice | 0.036 | 0.044 |  |
| harper.TheyToThem | Grammar | 0.036 | 0.026 |  |
| harper.RegularIrregulars | Grammar | 0.034 | 0.015 |  |
| harper.Someone | Miscellaneous | 0.034 | 0.009 |  |
| harper.Excellent | Enhancement | 0.03 | 0.053 |  |
| harper.PronounInflectionBe | Agreement | 0.03 | 0.04 |  |
| harper.Everyone | Miscellaneous | 0.029 | 0.015 |  |
| harper.WrongNegative | WordChoice | 0.027 | 0.009 |  |
| harper.TheProperNounPossessive | Redundancy | 0.025 | 0.004 |  |
| harper.MultipleSequentialPronouns | Repetition | 0.024 | 0.013 |  |
| harper.QuiteQuiet | Typo | 0.022 | 0.018 |  |
| harper.ThereToTheir | Grammar | 0.021 | 0.011 |  |
| harper.AWhile | Typo | 0.02 | 0.02 |  |
| harper.OneOfTheSingular | Usage | 0.02 | 0.018 |  |
| harper.WouldNeverHave | Style | 0.018 | 0.007 |  |
| harper.SimplePastToPastParticiple | Grammar | 0.016 | 0.024 |  |
| harper.LetsConfusion | WordChoice | 0.015 | 0.022 |  |
| harper.ModalBeAdjective | Miscellaneous | 0.015 | 0.017 |  |
| harper.OrthographicConsistency | Capitalization | 0.015 | 0.125 |  |
| harper.AvoidAndAlso | Redundancy | 0.014 | 0.018 |  |
| harper.DoubleModal | Miscellaneous | 0.014 | 0.006 |  |
| harper.HaveTakeALook | Regionalism | 0.012 | 0.011 |  |
| harper.FindOut | WordChoice | 0.011 | 0.009 |  |
| harper.NominalWants | Miscellaneous | 0.011 | 0.007 |  |
| harper.MergeWords | WordChoice | 0.011 | 0.013 |  |
| harper.ThereIsAgreement | Agreement | 0.011 | 0.015 |  |
| harper.OutOfTheWindow | Regionalism | 0.008 | 0.015 |  |
| harper.TheirToTheyre | Grammar | 0.008 | 0.009 |  |
| harper.WereWhere | Typo | 0.008 | 0.002 |  |
| harper.WhomSubjectOfVerb | Grammar | 0.008 | 0.013 |  |
| harper.YourPredicateAdjective | Grammar | 0.008 | 0.015 |  |
| harper.CapitalizePersonalPronouns | Capitalization | 0.008 | 0.006 |  |
| harper.Nonetheless | Miscellaneous | 0.008 | 0.006 |  |
| harper.Overall | Miscellaneous | 0.008 | 0.006 |  |
| harper.RedundantThat | Repetition | 0.008 | 0.017 |  |
| harper.FillerWords | Miscellaneous | 0.007 | 0.04 |  |
| harper.ForNoun | WordChoice | 0.007 | 0.009 |  |
| harper.ModalOf | WordChoice | 0.007 | 0.013 |  |
| harper.YeaToYeah | Spelling | 0.007 | 0.053 |  |
| harper.CondenseAllThe | Redundancy | 0.006 | 0.006 |  |
| harper.HadOf | Grammar | 0.006 | 0.007 |  |
| harper.HowTo | WordChoice | 0.006 | 0.004 |  |
| harper.LeftRightHand | Miscellaneous | 0.006 | 0.011 |  |
| harper.LitotesDirectPositive | Style | 0.006 | 0.002 |  |
| harper.Wanna | Miscellaneous | 0.006 | 0.002 |  |
| harper.AdjectiveDoubleDegree | WordChoice | 0.005 | 0 |  |
| harper.PhrasalVerbAsCompoundNoun | WordChoice | 0.005 | 0 |  |
| harper.Starving | Enhancement | 0.005 | 0.002 |  |
| harper.ThatThan | Typo | 0.005 | 0.004 |  |
| harper.AdjectiveOfA | Style | 0.005 | 0.006 |  |
| harper.QuantifierNeedsOf | Miscellaneous | 0.005 | 0.004 |  |
| harper.Therein | Miscellaneous | 0.005 | 0 |  |
| harper.Worthwhile | Miscellaneous | 0.005 | 0.006 |  |
| harper.Insofar | Miscellaneous | 0.004 | 0 |  |
| harper.Roundabout | Miscellaneous | 0.004 | 0.006 |  |
| harper.Thereupon | Miscellaneous | 0.004 | 0 |  |
| harper.WillNonLemma | Grammar | 0.004 | 0.002 |  |
| harper.FeelFell | Typo | 0.003 | 0.006 |  |
| harper.Freezing | Enhancement | 0.003 | 0.017 |  |
| harper.GoodAt | Usage | 0.003 | 0.002 |  |
| harper.ItsContraction | Punctuation | 0.003 | 0.002 |  |
| harper.PronounContraction | WordChoice | 0.003 | 0.004 |  |
| harper.The | Typo | 0.003 | 0 |  |
| harper.TheirToThere | Grammar | 0.003 | 0.007 |  |
| harper.UpdatePlaceNames | WordChoice | 0.003 | 0 |  |
| harper.AskNoPreposition | WordChoice | 0.002 | 0 |  |
| harper.ForAWhile | Typo | 0.002 | 0 |  |
| harper.Gonna | Miscellaneous | 0.002 | 0.011 |  |
| harper.However | Miscellaneous | 0.002 | 0 |  |
| harper.InflectedVerbAfterTo | WordChoice | 0.002 | 0.002 |  |
| harper.NorModalPronoun | Grammar | 0.002 | 0.002 |  |
| harper.NounVerbConfusion | WordChoice | 0.002 | 0.006 |  |
| harper.QuestionVerbAgreement | Agreement | 0.002 | 0.002 |  |
| harper.SubjunctiveWasToWere | Grammar | 0.002 | 0.006 |  |
| harper.ThisTypeOfThing | Agreement | 0.002 | 0.004 |  |
| harper.AsHow | Usage | 0.002 | 0.006 |  |
| harper.Bollocks | Spelling | 0.002 | 0 |  |
| harper.CommaFixes | Punctuation | 0.002 | 0.002 |  |
| harper.ExpandThough | Style | 0.002 | 0 |  |
| harper.Gotta | Miscellaneous | 0.002 | 0.013 |  |
| harper.HopHope | WordChoice | 0.002 | 0 |  |
| harper.IfWouldve | Nonstandard | 0.002 | 0.002 |  |
| harper.InDemandInDepth | Punctuation | 0.002 | 0 |  |
| harper.InterestedIn | Usage | 0.002 | 0.004 |  |
| harper.IntroCueCommaBeforeThanks | Punctuation | 0.002 | 0 |  |
| harper.ItsPossessive | Agreement | 0.002 | 0.013 |  |
| harper.MissingDeterminer | Grammar | 0.002 | 0.006 |  |
| harper.Nothing | Miscellaneous | 0.002 | 0 |  |
| harper.OceansAndSeas | Capitalization | 0.002 | 0.002 |  |
| harper.OnesSelf | Usage | 0.002 | 0 |  |
| harper.OutOfDate | Miscellaneous | 0.002 | 0 |  |
| harper.QuoteSpacing | Formatting | 0.002 | 0 |  |
| harper.Regionalisms | Regionalism | 0.002 | 0 |  |
| harper.SingleBe | Grammar | 0.002 | 0.006 |  |
| harper.SomeOfThe | Typo | 0.002 | 0 |  |
| harper.Straightforward | Miscellaneous | 0.002 | 0 |  |
| harper.SufficeItToSay | Usage | 0.002 | 0 |  |
| harper.TheHowWhy | Miscellaneous | 0.002 | 0.004 |  |
| harper.TheLastDays | Usage | 0.002 | 0 |  |
| harper.ThenThan | Miscellaneous | 0.002 | 0.002 |  |
| harper.VeryUnique | WordChoice | 0.002 | 0 |  |
| harper.WayTooAdjective | Miscellaneous | 0.002 | 0 |  |
| harper.APart | WordChoice | 0.001 | 0 |  |
| harper.AndTheLike | Usage | 0.001 | 0 |  |
| harper.BellowBelow | Malapropism | 0.001 | 0.002 |  |
| harper.BetterOffPhrase | Grammar | 0.001 | 0.002 |  |
| harper.DayOneNames | Capitalization | 0.001 | 0 |  |
| harper.DidPast | Grammar | 0.001 | 0 |  |
| harper.DoToDueTo | Usage | 0.001 | 0 |  |
| harper.DoinToDoing | Typo | 0.001 | 0.006 |  |
| harper.EverPresent | Punctuation | 0.001 | 0.002 |  |
| harper.Everybody | Miscellaneous | 0.001 | 0 |  |
| harper.ExpandBecause | Style | 0.001 | 0 |  |
| harper.FaceFirst | Punctuation | 0.001 | 0 |  |
| harper.FriendOfMe | Grammar | 0.001 | 0 |  |
| harper.Hedging | Miscellaneous | 0.001 | 0 |  |
| harper.ImplementIn | Usage | 0.001 | 0 |  |
| harper.ItAreTheOne | Agreement | 0.001 | 0 |  |
| harper.ItTimeAuxiliary | Grammar | 0.001 | 0 |  |
| harper.Months | Miscellaneous | 0.001 | 0 |  |
| harper.Myself | Miscellaneous | 0.001 | 0 |  |
| harper.NationalCapitals | Capitalization | 0.001 | 0 |  |
| harper.NoLonger | Usage | 0.001 | 0 |  |
| harper.NoLongerPronoun | WordOrder | 0.001 | 0 |  |
| harper.Nobody | WordChoice | 0.001 | 0 |  |
| harper.OldestInTheBook | Usage | 0.001 | 0 |  |
| harper.OnFloor | WordChoice | 0.001 | 0 |  |
| harper.Oxymorons | Miscellaneous | 0.001 | 0 |  |
| harper.PassedPast | WordChoice | 0.001 | 0 |  |
| harper.RackOnesBrain | Usage | 0.001 | 0 |  |
| harper.RedundantSuperlatives | Redundancy | 0.001 | 0 |  |
| harper.SeemsLikeAdjective | Redundancy | 0.001 | 0 |  |
| harper.SinceDuration | Miscellaneous | 0.001 | 0 |  |
| harper.SomewhatSomething | Style | 0.001 | 0 |  |
| harper.SubjectPronoun | Grammar | 0.001 | 0 |  |
| harper.TheMy | Repetition | 0.001 | 0 |  |
| harper.TransposedSpace | Typo | 0.001 | 0 |  |
| harper.WillContain | Agreement | 0.001 | 0 |  |
| harper.WorthToDo | Grammar | 0.001 | 0.002 |  |
| harper.YehToYeah | Spelling | 0 | 0.046 |  |
| harper.RedundantFirsts | Redundancy | 0 | 0.006 |  |
| harper.Cant | Enhancement | 0 | 0.004 |  |
| harper.InAWhile | Grammar | 0 | 0.004 |  |
| harper.ToSomeDegree | Eggcorn | 0 | 0.004 |  |
| harper.WayWithWhich | Redundancy | 0 | 0.004 |  |
| harper.ABitOfPlural | Grammar | 0 | 0.002 |  |
| harper.AsEvidentBy | Grammar | 0 | 0.002 |  |
| harper.AwaitFor | Redundancy | 0 | 0.002 |  |
| harper.BuiltIn | Punctuation | 0 | 0.002 |  |
| harper.CallThem | Redundancy | 0 | 0.002 |  |
| harper.ExpandMemoryShorthands | WordChoice | 0 | 0.002 |  |
| harper.ExpandMinimum | Style | 0 | 0.002 |  |
| harper.ExpandTimeShorthands | WordChoice | 0 | 0.002 |  |
| harper.IAmAgreement | Agreement | 0 | 0.002 |  |
| harper.KindSortOf | Typo | 0 | 0.002 |  |
| harper.LetToDo | Usage | 0 | 0.002 |  |
| harper.MultipleFrequencyAdverbs | Usage | 0 | 0.002 |  |
| harper.NoMatchFor | WordChoice | 0 | 0.002 |  |
| harper.NotTo | Typo | 0 | 0.002 |  |
| harper.Payed | Spelling | 0 | 0.002 |  |
| harper.ProgressiveNeedsBe | WordChoice | 0 | 0.002 |  |
| harper.ProperNouns | Capitalization | 0 | 0.002 |  |
| harper.RedundantPretty | Style | 0 | 0.002 |  |
| harper.ThatChallenged | Miscellaneous | 0 | 0.002 |  |
| harper.TheAllThe | Redundancy | 0 | 0.002 |  |
| harper.ToTo | Typo | 0 | 0.002 |  |
| harper.WorseOrWorst | Agreement | 0 | 0.002 |  |

## Planted errors

Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.

### calibre, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 93.6% |  | 75.4% | 90.6% |
| confusable | 500 | 500 | 37.2% |  | 97.3% | 97.3% |
| a-an | 500 | 500 | 96.4% |  | 99.2% | 99.2% |
| repeated-word | 500 | 500 | 64.8% |  | 98.8% | 98.8% |
| agreement | 500 | 500 | 69.6% |  | 69.3% | 69.3% |
| dialogue-punctuation | 500 | 500 | 0.4% |  | 0% | 0% |

### calibre, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 86.2% |  | 72.6% | 87.9% |
| confusable | 500 | 500 | 31.4% |  | 98.7% | 98.7% |
| a-an | 500 | 500 | 89% |  | 99.6% | 99.6% |
| repeated-word | 500 | 500 | 59.8% |  | 98% | 98% |
| agreement | 500 | 500 | 58% |  | 65.5% | 65.5% |
| dialogue-punctuation | 500 | 500 | 0.2% |  | 0% | 0% |

### gutenberg, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 86.8% |  | 74.9% | 88.7% |
| confusable | 500 | 500 | 34.6% |  | 96% | 96% |
| a-an | 500 | 500 | 85.4% |  | 99.8% | 99.8% |
| repeated-word | 500 | 500 | 59% |  | 97% | 98% |
| agreement | 500 | 500 | 61.4% |  | 67.1% | 67.1% |
| dialogue-punctuation | 500 | 500 | 0.4% |  | 0% | 0% |

### gutenberg, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 86.4% |  | 75.7% | 91% |
| confusable | 500 | 500 | 34.2% |  | 96.5% | 96.5% |
| a-an | 500 | 500 | 86.8% |  | 100% | 100% |
| repeated-word | 500 | 500 | 60% |  | 97.3% | 98% |
| agreement | 500 | 500 | 56.2% |  | 75.1% | 75.1% |
| dialogue-punctuation | 500 | 500 | 0.2% |  | 0% | 0% |

### all, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 90.2% |  | 75.2% | 89.7% |
| confusable | 1000 | 1000 | 35.9% |  | 96.7% | 96.7% |
| a-an | 1000 | 1000 | 90.9% |  | 99.5% | 99.5% |
| repeated-word | 1000 | 1000 | 61.9% |  | 97.9% | 98.4% |
| agreement | 1000 | 1000 | 65.5% |  | 68.2% | 68.2% |
| dialogue-punctuation | 1000 | 1000 | 0.4% |  | 0% | 0% |

### all, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 86.3% |  | 74.2% | 89.5% |
| confusable | 1000 | 1000 | 32.8% |  | 97.6% | 97.6% |
| a-an | 1000 | 1000 | 87.9% |  | 99.8% | 99.8% |
| repeated-word | 1000 | 1000 | 59.9% |  | 97.7% | 98% |
| agreement | 1000 | 1000 | 57.1% |  | 70.2% | 70.2% |
| dialogue-punctuation | 1000 | 1000 | 0.2% |  | 0% | 0% |
