# Rules

Every rule Stet has, 65 in all. Each has an ID, which is what you use to turn it on or off:

```csharp
var options = new CheckerOptions().WithRule(RuleIds.UnknownName, false);
```

`checker.Rules` gives the same list at run time, with each rule's name, description and examples, for
building a settings screen. Every rule is on by default.

**Formal rules.** Some rules flag things writers do on purpose in a character's speech, such as
"could of" or "they was". These are marked *formal*. In narration they always apply. Inside dialogue
they apply only when dialogue strictness is set to `Strict`.

## Spelling

`RuleCategory.Spelling`

| ID | Rule | Flagged | Left alone |
|---|---|---|---|
| `spelling.misspelling` | Misspelled words | She could not beleive it.<br>He opened teh door. | She could not believe it.<br>"I'm gonna find 'em," he said. |
| `spelling.unknown-name` | Unknown names | They rode north to Vaelmoor. | They rode north to London. |
| `spelling.other-dialect` | Other-dialect spellings | The colour had faded. | The color had faded. |
| `spelling.capitalization` | Missing capitals | She sailed for london. | She sailed for London. |

## Mechanics

`RuleCategory.Mechanics`

| ID | Rule | Flagged | Left alone |
|---|---|---|---|
| `mechanics.repeated-word` | Repeated words | She opened the the door.<br>He walked to to the gate. | He had had enough.<br>I know that that is true. |
| `mechanics.a-an` | A and an | She ate a apple.<br>He waited a hour. | She ate an apple.<br>He waited an hour. |
| `mechanics.could-of` | Could of *(formal)* | He could of gone home.<br>She should of known better. | He could have gone home.<br>He could, of course, go home. |
| `mechanics.double-comparative` | Double comparison *(formal)* | This rope is more stronger than the other.<br>It was the most quickest way to the harbour. | This rope is stronger than the other.<br>More older men came than young ones. |
| `mechanics.missing-apostrophe` | Missing apostrophe | He wont tell anyone, I promise.<br>She cant have gone far. | He won't tell anyone.<br>He rose early, as was his wont. |
| `mechanics.doubled-space` | Doubled spaces | She opened&nbsp;&nbsp;the door.<br>He ran,&nbsp;&nbsp;&nbsp;then stopped. | She opened the door.<br>She opened the door.&nbsp;&nbsp;Then she stopped. |
| `mechanics.punctuation-spacing` | Punctuation spacing | He opened the door , and went in.<br>He looked up,, then looked away. | He opened the door, and went in.<br>He waited . . . , then spoke. |
| `mechanics.sentence-capital` | Sentence capitals | He left. she stayed.<br>the door was open. | He left. She stayed.<br>“Why?” she asked. |
| `mechanics.lowercase-i` | Lowercase I | Then i saw the smoke.<br>She knew i'm not one to complain. | Then I saw the smoke.<br>See chapter i for more. |

## Agreement

`RuleCategory.Agreement`

| ID | Rule | Flagged | Left alone |
|---|---|---|---|
| `agreement.pronoun-verb` | Pronoun and verb agreement | I has no idea.<br>They is late again. | They are late again.<br>One of you is lying. |
| `agreement.dialect-verb-form` | Dialect verb forms *(formal)* | They was late again.<br>He don't know the way. | They were late again.<br>If I were you, I would go. |
| `agreement.verb-form` | Verb form *(formal)* | I have went there many times.<br>She has ate nothing since morning. | I have gone there many times.<br>He did not go to the meeting. |
| `agreement.noun-verb` | Plural noun with a singular verb *(formal)* | The children was playing in the yard.<br>The old dogs is asleep by the fire. | The children were playing in the yard.<br>The box of letters was on the table. |

## Dialogue punctuation

`RuleCategory.DialoguePunctuation`

| ID | Rule | Flagged | Left alone |
|---|---|---|---|
| `dialogue.full-stop-before-tag` | Full stop before a dialogue tag | “Hello.” she said.<br>“Come in.” said the old man. | “Hello,” she said.<br>“Hello.” She smiled. |
| `dialogue.capitalized-tag` | Capitalized dialogue tag | “Hello,” She said.<br>“Come in,” Said the old man. | “Hello,” she said.<br>“Hello,” Tom said. |
| `dialogue.missing-comma` | Missing comma before a dialogue tag | “Hello” she said.<br>“Come in” said the old man. | “Hello,” she said.<br>“Hello—” she said. |
| `dialogue.comma-outside-quote` | Comma outside the quote marks | “Hello”, she said.<br>“Come in”, said the old man. | “Hello,” she said.<br>He called it “home”, though it was not. |

## Confusable words

`RuleCategory.ConfusableWords`

These flag a word only when the words on either side show strongly that another was meant, judged
from how often each word is seen in those surroundings in published fiction. They stay quiet when
unsure, so they miss swaps that only the wider sentence gives away ("a matter of principal").

| ID | Words | Flagged |
|---|---|---|
| `confusable.their-there` | their, there, they're | She went over their to look. |
| `confusable.its-its` | its, it's | I think its going to rain. |
| `confusable.your-youre` | your, you're | I hope your going to tell me. |
| `confusable.whose-whos` | whose, who's | I wonder whose going to tell him. |
| `confusable.then-than` | then, than | She was taller then her brother. |
| `confusable.lose-loose` | lose, loose | I don't want to loose you. |
| `confusable.affect-effect` | affect, effect | The medicine had no affect on him. |
| `confusable.accept-except` | accept, except | She could not except his offer. |
| `confusable.to-too` | to, too | It was to late to go back. |
| `confusable.of-off` | of, off | He walked of without a word. |
| `confusable.lead-led` | lead, led | He lead her into the room. |
| `confusable.past-passed` | past, passed | She walked passed the house. |
| `confusable.breath-breathe` | breath, breathe | She could hardly breath. |
| `confusable.quite-quiet` | quite, quiet | She was quiet sure of it. |
| `confusable.peek-peak` | peek, peak, pique | She took a peak through the curtains. |
| `confusable.wander-wonder` | wander, wonder | I wander if she knows. |
| `confusable.desert-dessert` | desert, dessert | She ordered desert. |
| `confusable.brake-break` | brake, break | She was afraid it would brake. |
| `confusable.principal-principle` | principal, principle | Their principle concern was money. |
| `confusable.compliment-complement` | compliment, complement | She paid him a complement. |
| `confusable.whether-weather` | whether, weather | I don't know weather he will come. |
| `confusable.here-hear` | here, hear | Come over hear and look at this. |
| `confusable.bear-bare` | bear, bare | She could not bare to watch. |
| `confusable.advice-advise` | advice, advise | He gave her some good advise about the road. |
| `confusable.week-weak` | week, weak | I saw him last weak at the market. |
| `confusable.know-no` | know, no | There was know one in the house. |
| `confusable.piece-peace` | piece, peace | She tore off a peace of cloth. |
| `confusable.allowed-aloud` | allowed, aloud | She was aloud to stay up late. |
| `confusable.throne-thrown` | throne, thrown | The king sat on his thrown and said nothing. |
| `confusable.stake-steak` | stake, steak | He cut into the juicy stake. |
| `confusable.waist-waste` | waist, waste | It was a waist of time. |
| `confusable.bored-board` | bored, board | They went on bored the ship. |
| `confusable.whole-hole` | whole, hole | She ate the hole thing. |
| `confusable.weight-wait` | weight, wait | I cannot weight any longer. |
| `confusable.write-right` | write, right | You were write about him. |
| `confusable.plain-plane` | plain, plane | The plain landed at dawn. |
| `confusable.role-roll` | role, roll | She played the leading roll. |
| `confusable.sight-site` | sight, site | He caught site of her in the crowd. |
| `confusable.soul-sole` | soul, sole | There was not a sole in sight. |
| `confusable.tail-tale` | tail, tale | The fox had a bushy tale. |
| `confusable.steal-steel` | steal, steel | He tried to steel a horse. |
| `confusable.heal-heel` | heal, heel | The wound would not heel. |
| `confusable.pour-pore` | pour, pore | She pored the wine into his cup. |
| `confusable.rein-reign` | rein, reign, rain | The rein fell all night. |
