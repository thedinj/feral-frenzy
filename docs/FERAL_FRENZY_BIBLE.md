# FERAL FRENZY — GAME BIBLE
### Design Document v1.1 — Working Title: FERAL FRENZY
*Solo Developer Edition*

---

## INTRODUCTION

This document is the guiding truth for the game. It exists so that every decision — about a level, a mechanic, a generator, a character — can be tested against a single source. When something feels wrong, come here first. When something feels right, make sure it's in here.

The Doom Bible was written to capture intent before code. This document serves the same purpose. It is not a technical specification. It is a constitution.

---

## WHY THIS GAME EXISTS

This game will never cost a single dollar. Not because money doesn't matter, but because the goal is simple: **make sure everyone plays it.** A price tag is a barrier. There are no barriers here.

This is a pure passion project. It exists because making it is worthwhile, and because the people who play it deserve something made without a financial motive distorting every decision.

The quality will be high enough that people will wonder why it's free. That will seem absurd to them. Good.

### The Social Mission

This is the game you play when you just met someone and want to have fun together. It is the first game you reach for when friends come over and nobody knows what to put on. It is the game that works at a party, in a dorm room, at a family gathering, between strangers who have nothing in common except that they are both holding a controller right now.

This is the design north star above all others: **two people who have never met should be able to pick this up and have a great time within sixty seconds.**

Everything that conflicts with that mission — complexity, onboarding friction, pay-to-win mechanics, anything that makes a new player feel excluded — is wrong for this game, no matter how good it sounds otherwise.

### What This Means in Practice

- The game is free. Always. No exceptions.
- No content is locked behind real money
- Unlocks exist to reward time spent, never money spent
- The game must be instantly legible to someone who has never seen it before
- Complexity is optional depth, never a barrier to entry
- The chaos must be *fun* chaos, never confusing chaos — a new player should be laughing within the first minute, not confused

### A Note to the Developer

When a decision feels hard, ask: *would this make it more fun for two strangers playing together for the first time?* If yes, do it. If no, reconsider. If it only serves players with fifty hours in the game, it is low priority. If it serves everyone from the first minute, it is high priority.

The free price is the promise that nobody gets left out. The quality is the proof that free didn't mean cheap.

This game is a gift. Make it like one.

---

## THE ONE-SENTENCE PITCH

**This is a chaotic, perfectly-balanced co-op run-and-gun platformer where powerful weapons are as dangerous to you as they are to your enemies — and every run tells the same story in a completely different way.**

---

## TONE AND UNIVERSE

### The Core Tone Decision

**Genuinely epic with absurdist edges. The game plays everything completely straight.**

The comedy comes from the sincerity, not from winking at the camera. Think Mad Max: Fury Road — nobody in that film acknowledges the flaming guitar player. Nobody in this game acknowledges that the crew is four animals. The world is insane. The characters are not ironic about the world.

Reference points:
- **Broforce** — reverent absurdity, love of source material
- **The Expendables / A-Team** — a crew that owns their roles, no hierarchy
- **The Incredibles** — found-family dynamic, each member essential
- **Impractical Jokers** — chaotic equals who genuinely enjoy what they do
- **Cocaine Bear** — animal protagonist treated with complete narrative seriousness

### What This Means in Practice

- The animals are never commented on. They are the crew. This is normal.
- The villain's evil plan is played straight even when it is ridiculous.
- New players laugh because the content is insane. Not because the game is joking.
- The crew never breaks. They are always professional. The professionalism IS the joke.

---



This is a 2D side-scrolling run-and-gun whose deepest DNA is **Mega Man** — the tight controls, the eight-directional aiming, the weapon variety, the movement that feels good before anything else does. Layered on top of that foundation is the co-op chaos of Contra, the density of Metal Slug, and the anarchic energy of Broforce. Players run, jump, slide, and shoot their way through three thematic chapters in approximately 30 minutes. The camera is wide and distant. The pixel art is small, crisp, and dense. The screen is full of things happening at all times.

It is designed first and foremost to be played with other people in the same room.

### The Combat Loop

The bulk of gameplay is **quantity combat** — waves and swarms of enemies filling the screen, projectiles flying in multiple directions, controlled chaos that brushes up against bullet hell without fully committing to it. Think Contra's relentlessness crossed with the projectile density of a light bullet hell. Players are always outnumbered. That is the fun.

This is broken up rhythmically by **boss battles and setpieces** — moments where the screen clears and something singular, dramatic, and memorable takes over. These are the exhale after the inhale. They need to feel earned because the player has been drowning in chaos right up until that moment.

The rhythm is: **swarm → swarm → swarm → setpiece → swarm → boss.** The generator encodes this rhythm. Deviating from it without intention is a generation error.

The game is **not** a precision platformer. Platforms exist as tactical terrain, not as puzzles. The primary verb is shooting. The secondary verb is moving. Everything else supports those two things.

It is **not** a deep RPG. Progression is light. The joy is in the run itself.

It is **not** fair in the traditional sense. It is chaotic. The skill ceiling is "surviving chaos you partially created yourself."

---

## WHAT THIS GAME FEELS LIKE

Close your eyes and imagine:

- Four players on a couch, all screaming
- A rocket launcher that just destroyed the bridge everyone needed
- A dinosaur with laser cannons charging from the right side of the screen
- Someone grabbed the wrong power-up and now they're tiny
- The screen is absolutely full of enemies and explosions and nobody has died yet somehow
- Then everyone dies at once

**That is this game.**

The reference points are: **Mega Man** (weapons, movement, feel — the deepest influence), Contra III, Metal Slug, Broforce, Dead Cells (structure), Spelunky (generation philosophy). The tone is loud, colorful, and relentless.

---

## CORE DESIGN PILLARS

These are non-negotiable. Every feature, level, mechanic, and generator decision must serve at least one of these.

### 1. CHAOS WITHIN A SAFE ENVELOPE
The game must feel out of control without ever being unfair. Players always have an out — a gap to dodge through, a platform to retreat to, an escape route. The generator's prime directive is: *never fully corner the player*. Chaos is fun. Cheap death is not.

### 2. POWER IS EARNED AND MOSTLY CONSEQUENCE-FREE
Overpowered weapons are allowed. Encouraged. *Celebrated.* Mowing down enemies with a rocket launcher while your friends cheer is the point. Most of the time, power has no downside — it just makes the chaos louder and more fun. The easy mode is valid. The easy mode is *fun*.

The salt in the spice is the destructible level. Once or twice per run, the rocket launcher will be a liability. Players who've been cruising will need to think for a minute. Then the destructible level ends, and the power fantasy resumes. We never punish players for having fun. We just occasionally wink at them.

### 3. CO-OP IS THE REAL GAME
Solo is brutally hard and intentionally so. Every additional player makes the game easier and more chaotic simultaneously. "You're missing out if you're not playing with others" is not a warning — it is the promise. The game scales enemy counts with player count. The geometry never changes.

### 4. THE RUN ALWAYS MOVES FORWARD
No pausing. No menu-ing mid-run. No dead space between segments. Players drop into mild action immediately and never stop moving until the chapter ends. Flow is sacred. The generator must never produce a segment where players spend 10 seconds figuring out what to do.

### 5. EVERY RUN TELLS THE SAME STORY DIFFERENTLY
The macro structure of a run is fixed — same chapters, same emotional arc, same narrative beats. The micro content is generated. Players will recognize the shape of a run without being able to predict its contents. Familiarity breeds mastery. Variety breeds replayability.

### 6. INSTANT SOCIAL GLUE
Two strangers must be able to pick this up and have a great time within sixty seconds. Onboarding friction is the enemy. Complexity is optional depth, never a barrier. The first minute must produce laughter, not confusion. This game is the reason people meet. Design every system with that person in mind — the one who has never played before and is holding a controller for the first time right now.

---

## THE CREW

### Group Identity

The crew are chaotic equals. Nobody leads. Nobody follows. Somehow it always works out. They do this because it is genuinely fun — not out of duty, obligation, or heroism. They were doing something like this anyway. The mission is the good time.

Their history is deliberately ambiguous — they feel like fast friends with deep shared history, but the game never explains it. A new player picking up a controller should feel like they joined something that already exists and is already good, without feeling like they missed a backstory. **This ambiguity is a social design decision.** It serves the core mission: two strangers should feel like a team within sixty seconds.

### Roster — Four Characters at Launch

The fifth play style slot (Balanced) is reserved for the first expansion character. The roster should feel complete at four and obviously expandable — when a fifth joins it feels like a reunion, not an addition.

Each character is a widely recognized internet meme or cultural reference. The familiarity is the instant readability. Players have a pre-existing relationship with each animal before they touch the controller. The game leverages that relationship and plays it completely straight.

**All roster animals are predators or omnivores. No purely herbivorous animals. The crew reads as dangerous even when the animals are inherently funny.**

---

### 🐻 BEAR
*Play Style: Tanky*

**The immovable object.** Large, wide silhouette. Reads as unstoppable at a glance. The safest character for new players — powerful, forgiving, and the most cooperative member of the crew without ever trying to be.

| Attribute | Value |
|---|---|
| Size | Large |
| Speed | Slow |
| Weapon Scale | Biggest damage |
| Extra Hit | ✅ Yes |
| Tight Gaps | Sliding only |
| Double Jump | Standard arc |

**Secondary — ROAR:** Staggers all enemies on screen. The whole team exploits the window. In solo, Bear must exploit his own opening. In co-op, the Roar becomes a force multiplier unlike anything else in the game.

*Shines when:* co-op, boss fights
*Struggles when:* surrounded solo
*New player feel:* safe and powerful
*Veteran mastery:* Roar timing for team combos

---

### 🐊 CROC
*Play Style: Giant*

**The ancient engine.** Huge and boxy upright. Distinctive long jaw. The largest character on screen — becomes low-profile when sliding. Pure selfish aggression. Commits forward and clears everything in the path.

| Attribute | Value |
|---|---|
| Size | Huge |
| Speed | Slow |
| Weapon Scale | Big damage |
| Extra Hit | ❌ No |
| Tight Gaps | Sliding only |
| Double Jump | Lower arc |

**Secondary — JAW SLAM:** Leaps forward, bites down, shockwave launches everything ahead. Entirely positional. No subtlety. Maximum commitment.

*Shines when:* crowds, chokepoints
*Struggles when:* vertical levels
*New player feel:* big and destructive
*Veteran mastery:* Jaw Slam positioning

---

### 🦈 HAMMERHEAD
*Play Style: Ranged*

**The T-shaped professional.** Medium size. Instantly recognizable silhouette. Permanently looks confused. Never is. The breathing situation is never addressed. Not once.

| Attribute | Value |
|---|---|
| Size | Medium |
| Speed | Normal |
| Weapon Scale | Normal damage |
| Extra Hit | ❌ No |
| Tight Gaps | Sliding only |
| Double Jump | Standard arc |

**Secondary — REVERSE FIRE:** Shoots behind while moving forward. Punishes enemies for being behind him. Makes being chased into an advantage. Rewards spatial awareness.

*Shines when:* long sightlines, being chased
*Struggles when:* close quarters ambush
*New player feel:* competent immediately
*Veteran mastery:* Reverse Fire kiting

---

### 🦡 HONEY BADGER
*Play Style: Agile*

**The smallest thing on the screen. Somehow always fine.** Tiny. Genuinely tiny. The size differential between Honey Badger and Croc communicates the crew's dynamic before a single word is read. Honey Badger don't care. This is mechanically true.

| Attribute | Value |
|---|---|
| Size | Tiny |
| Speed | Fast |
| Weapon Scale | Small damage |
| Extra Hit | ❌ No |
| Tight Gaps | Always fits |
| Double Jump | Travels further |

**Secondary — PINBALL JUMP:** Launches into the air, bounces off enemies and walls at high speed. Uncontrollable. Devastating. The game treats this as a normal field operation technique.

*Shines when:* pure speed and chaos
*Struggles when:* taking any hit
*New player feel:* fast and chaotic
*Veteran mastery:* Pinball bounce routing

---

### Weapon Scaling Rule

Weapons scale with character size. No separate damage stats per character — the same weapon produces more impact in larger hands. A Bear firing a pistol is firing a cannon. Honey Badger firing a rocket launcher is carrying something bigger than she is. The game never explains this. It is simply true.

**This eliminates weapon affinity as a separate system. Size IS the affinity. Clean, readable, requires no documentation to understand.**

### Two-Button Combat Philosophy

**Button 1** is the primary attack — the Mega Man blaster. Works everywhere, eight-directional aiming, always reliable. Identical across all four characters. Charging is a weapon feature, not a character feature — it belongs to specific weapons in the loadout.

**Button 2** is the secondary attack — the character's signature. This is where identity lives. Each secondary tells a completely different gameplay story.

### The Missing-Out Mechanic

Bear's Roar is deliberately the most team-oriented move in the game. It is the mechanical expression of the social design mission. Bear solo is powerful. Bear with one other player is a force multiplier. Bear with three other players is a different game entirely. Players who have experienced Bear in co-op will feel his absence in every subsequent solo run.



### Movement Envelope
All crew members share the same core movement primitives. **Mega Man X is the movement bible.** This is also the solvability bible for the level generator.

- **Run** — horizontal movement, speed varies by character (Honey Badger fast, Bear and Croc slow)
- **Double Jump** — standard air control; Croc has a lower arc, Honey Badger travels further
- **Wall Kick** — Mega Man X style, can chain indefinitely on opposing walls. This nearly eliminates unsolvable layouts.
- **Slide** — can be initiated from standing still; reduces all characters to the same height while active; traversal only, no invincibility frames. Honey Badger always fits through tight gaps without sliding.
- **Eight-directional aiming** — all ranged weapons aim in 8 directions while moving or standing. This is non-negotiable.

Croc sliding to normal height is the great equalizer. **All levels must be completable by a sliding Croc.** This is the single most important solvability constraint.

### Combat
- Melee, ranged, and environmental kills are all valid
- Small health pool — slightly more forgiving than Broforce
- Death is not instant failure — teammates can revive fallen players
- If ALL players die simultaneously, the current segment restarts
- Weapons are chosen before the run and carried through — the generator cannot assume any specific loadout

---

## WEAPONS AND BALANCE

### The Mega Man Philosophy
Mega Man is the spiritual father of this weapon system. The lessons it taught are law here:

- Every weapon has a distinct identity, not just a stat variation
- Weapons feel good to use even when they aren't optimal
- The fun is in discovering what each weapon *does*, not in optimizing damage numbers
- Aiming is expressive — eight directions gives players creative agency in every encounter

### The Curiosity Reward Gradient

Weapons are designed around a three-tier discovery curve. Players are never told this curve exists. They experience it.

**Tier 1 — The Default (everyone starts here)**
A Mega Buster-style rapid-fire blaster. Feels good immediately. Clearly powerful. Easy to aim, satisfying to use, works in every situation. New players will carry this for their first several runs and have a great time. This is intentional. The default weapon should never feel like a placeholder — it should feel like a solid, complete choice.

There will also be a fire flower-style pickup somewhere in the game — a classic, immediately legible power fantasy that any player who has touched a Nintendo controller will recognize and love. These familiar touchstones matter for instant accessibility.

**Tier 2 — The Spinning Blade (reward for mild curiosity)**
The moment a player pokes around the weapon selection screen — not through any prompting, just natural curiosity — they find this. Eight-directional razor disc modeled on Mega Man 2's Metal Blade. Immediately more fun than the default. Faster, more expressive, more satisfying. The reaction should be: *"oh, this is obviously better — why didn't I know about this?"*

This is the reward for being the kind of player who checks the options. It requires almost no effort to find. That's the point. The game is quietly saying: *exploring the settings pays off.*

**Tier 3 — The Unlock Pool (reward for deep play)**
Weapons discovered through actual playthroughs. Each one should produce a genuine *"wait, THIS exists?"* moment. At least one early unlock should make a player completely reconsider their playstyle. These are not universally better than the Spinning Blade — they are *different* in ways that reward creative thinking and personal style.

### The Design Rule for All Three Tiers
The default must never feel bad. The Blade must feel like a natural upgrade for anyone who finds it. Unlocks must feel like genuine discoveries, not grind rewards. A player who never leaves the default has a good time. A player who finds the Blade has a better time. A player who digs deep has the most fun. None of these players is wrong.

### The Golden Rule
**The generator cannot assume the player's loadout. Every segment must be completable with any reasonable weapon combination.**

### Power vs. Consequence
Powerful weapons are genuinely, unapologetically overpowered. That is intentional. The rocket launcher clears rooms. The spread shot melts bosses. Players who want easy mode have it, and easy mode is a blast.

The only place this gets complicated is destructible levels — once or twice per run, explosive weapons become a liability because the geometry blows up with the enemies. This is not a punishment. It is a single pinch of salt in an otherwise rich meal. Players navigate it, feel clever for doing so, and then go back to causing mayhem.

- Powerful weapons are the default fun. They are not nerfed, gated, or balanced away.
- Eight-directional aiming is a baseline expectation — all ranged weapons support it
- The Spinning Blade remains the perfect all-rounder precisely because it never becomes a liability anywhere

### Power-ups
- Found in levels, placed by the generator as reward nodes
- Majority are positive
- A small percentage are negative or double-edged — rare enough to be surprising, consistent enough that players always hesitate slightly
- That hesitation is the fun

### The Golden Rule
**The generator cannot assume the player's loadout. Every segment must be completable with any reasonable weapon combination.**

### Power vs. Consequence
Powerful weapons are genuinely, unapologetically overpowered. That is intentional. The rocket launcher clears rooms. The spread shot melts bosses. Players who want easy mode have it, and easy mode is a blast.

The only place this gets complicated is destructible levels — once or twice per run, explosive weapons become a liability because the geometry blows up with the enemies. This is not a punishment. It is a single pinch of salt in an otherwise rich meal. Players navigate it, feel clever for doing so, and then go back to causing mayhem.

- Powerful weapons are the default fun. They are not nerfed, gated, or balanced away.
- Eight-directional aiming is a baseline expectation — all ranged weapons support it
- The Spinning Blade remains the perfect all-rounder precisely because it never becomes a liability anywhere

### Power-ups
- Found in levels, placed by the generator as reward nodes
- Majority are positive
- A small percentage are negative or double-edged — rare enough to be surprising, consistent enough that players always hesitate slightly
- That hesitation is the fun


---

## THE ONE MECHANIC RULE

This is one of the most important design commitments in this document.

**Every level has exactly one mechanic or gimmick that exists nowhere else in the game.** When you finish a level, you are done with that exact experience forever. Nothing repeats. Nothing overstays its welcome.

This is why the game has relatively few levels. Quantity is not the goal. Every level must earn its place by doing something no other level does. A level without a unique identity should not exist.

### What "Mechanic" Means Here

A mechanic is not just a graphical reskin. It is something that changes how you play, how you think, or how you move through that level specifically. Examples of the kind of thinking this demands:

- A level where the floor is rising water and you're always being pushed upward
- A level played entirely in silhouette where enemy shapes are your only information
- A level where a friendly dinosaur is charging alongside you and you have to keep up
- A level where every explosion triggers a chain reaction through the entire geometry
- A level with a wind mechanic that pushes projectiles sideways

The graphical theme can carry a level's *identity*. The mechanic carries its *memory*. Both are required. A level with a great theme but no mechanic is scenery. A level with a great mechanic but no theme is a prototype. Ship neither.

### The Generator's Role

The macro generator tags each segment with its unique mechanic. No two segments in a run share the same mechanic tag. The validator enforces this. Repetition is a generation error, not a design choice.

### Why Fewer Levels Is Correct

Twenty levels each with a distinct mechanic will be remembered. Fifty levels where mechanics repeat will be forgotten after the first playthrough. This game is built for the player who has never played it before and the player on their twentieth run equally — the one-mechanic rule serves both.

---

## THE THREE CHAPTERS

Each chapter follows the same structural template instantiated with different theme, geometry personality, and mechanical flavor. One template. Three expressions.

### Chapter Template
1. **Opening** — establishes the theme, introduces the chapter environment, debuts the first level-specific mechanic
2. **Escalation** — ramps density and difficulty, each segment introducing a new mechanic twist
3. **Setpiece** — a scripted, memorable, adrenaline moment unique to the chapter; its mechanic is the most dramatic of the chapter
4. **Boss** — tests the player's mastery of the chapter's mechanical vocabulary

### A Note on Mechanic Design
Chapter mechanics establish the vocabulary. Level mechanics are words in that vocabulary. The Dino Riders chapter deals in open terrain and mounted combat — level mechanics within it should feel like expressions of that world, not departures from it. The mechanic surprise comes from *how* the vocabulary is used, not from abandoning it.

### CHAPTER 1 — THE CRETACEOUS
*"Laser dinosaurs. Wide open skies. Pure freedom."*

**Story:** Prehistoric Earth has been occupied and subjugated by Baroness Cretacia, who has been obsessed with dinosaurs since age 7. The conquest was an afterthought. She rides a triceratops named Gerald. Gerald is also evil.
**Inspiration:** Dino Riders (1987 animated series), classic Contra exterior levels
**Geometry:** Open, exterior, rolling terrain. Wide horizontal space. Natural platforms — rock formations, cliff edges, dinosaur backs. Long sightlines.
**Enemies:** Mounted dinosaurs, ground troops, aerial threats
**Chapter Mechanic:** Rideable dinosaurs as mobile platforms and weapons
**Generator Personality:** Favors wide spacing, clear sightlines, generous vertical variance. Lowest ceiling pressure of all three chapters.
**Tone:** Bright, explosive, liberating
**Villain:** Baroness Cretacia — spectacular, inconclusive defeat. Maybe 60% certain she's gone. Exits screaming, laughing, promising revenge. Gerald may or may not escape with her.

### CHAPTER 2 — DEAD STATION
*"Cramped corridors. Zombie aliens. Splash damage becomes your enemy."*

**Story:** A space station in 2387, occupied by Professor Static — a consciousness uploaded to the station's network. Omnipresent, omniscient, insufferable. Has briefed his zombie alien crew on the evil plan 40 times.
**Inspiration:** Doom, Duke Nukem 3D, classic corridor shooters
**Geometry:** Tight, interior, claustrophobic. Corridors with perpendicular junctions. Ceilings matter as much as floors. Vertical stacking of threats.
**Enemies:** Zombie aliens, turrets, ambush spawners from vents and doors
**Chapter Mechanic:** Destructible walls and pillars; enemies spawn from unexpected directions
**Generator Personality:** Favors enclosed spaces, chokepoints, vertical threat stacking. This is the chapter where rocket launchers become a liability.
**Tone:** Dark, tense, claustrophobic
**Villain:** Professor Static — his server core is destroyed. Probably. There are backups. Broadcasts from every remaining intercom simultaneously as they go offline one by one.

### CHAPTER 3 — THE INFERNAL KEEP
*"Lava. Flame bars. Dropping platforms. The hybrid."*

**Story:** A fortress built inside an active volcano in the Medieval Volcanic Age, occupied by Lord Inferno, who loves volcanoes more than is healthy. Built his fortress inside one on purpose. His cape is literally on fire. This is load-bearing to his identity.
**Inspiration:** Bowser's Castle, classic final world design
**Geometry:** Semi-open interior. More breathing room than Chapter 2, less than Chapter 1. Dramatic verticality. Environmental hazards do the work that enemies did in Chapter 2.
**Enemies:** Mix of Chapter 1 and 2 rosters, elite variants
**Chapter Mechanic:** Timed environmental hazards — flame bars, lava rises, dropping platforms. Hazards are first-class generator primitives here.
**Generator Personality:** Hybrid of chapters 1 and 2. Hazard budget replaces some enemy budget.
**Tone:** Epic, dramatic, volcanic
**Villain:** Lord Inferno — the volcano itself begins erupting mid-fight. His exit is dramatic. His survival is ambiguous. Screams his exit promises while surfing a lava flow. Possibly fine.

---

## DESTRUCTIBLE LEVELS

### The Fixed Rule
One level per run is always destructible. It lives in the same chapter every run (Chapter 2 — Dead Station). Players will come to know this. It will not diminish the tension.

### The Surprise Rule
50% of runs will have one additional level with destructible terrain, drawn from a non-Chapter-2 segment. Players cannot predict which one. This is a soft difficulty modifier for veteran players who always run the same overpowered loadout.

### The Design Intent
Destructible levels are not the game's difficulty system. They are a brief moment of consequence in an otherwise generous power fantasy. Players with explosive loadouts will need to slow down and aim carefully for one or two segments. Then it's over and the mayhem resumes.

The feeling should be: *"oh right, I should probably not rocket this wall"* — a small tactical adjustment, not a punishment. Players feel clever for navigating it. They do not feel frustrated.

### Generator Rules for Destructible Levels
- Critical path geometry is tagged **INDESTRUCTIBLE**
- Destructible geometry clusters near enemies and near the critical path — it must be a tempting target
- Indestructible geometry should be visually distinct but not obviously so — players should learn the hard way at least once
- Chain-destructible objects (barrel → wall → floor) are encouraged
- No destructible floors above lava

---

## SURPRISE GENRE LEVELS

Twice per run, between chapters, the game performs a **genre transplant**. The camera changes. The controls change. The rules change entirely. These are not generated — they are self-contained modules that plug into the master spine.

### The Gradius Level — The Commute
*Between Chapter 1 and Chapter 2*

**Story beat:** The crew needs transit from the Cretaceous to the future space station. The Feral Frenzy Division has one asset available — a single temporal fighter craft of disputed maintenance history. The crew gets in. It works perfectly. This is treated as a miracle by everyone except the crew.

**The joke:** The Gradius Level is the commute. The crew shoots down entire armadas of villain interceptors on the way to work. The post-mission briefing describes this as "transit completed without incident."

**Format:** Horizontal scrolling shooter, ship-based. 2-3 minutes. Genuine expression of the genre, not a parody.

### The Brawler Level — The Layover
*Between Chapter 2 and Chapter 3*

**Story beat:** The space station's escape route deposits the crew in a temporal transit hub — a neutral zone populated by villain-adjacent hired muscle on layover. No weapons allowed in the neutral zone. These are the rules. The crew respects the rules. Fists only.

**The joke:** A top-down brawler set in what is essentially a villain's airport terminal. Vending machines. Lockers. A corkboard with motivational posters. Someone's lunch in the break room fridge with their name on it. It gets destroyed. The crew treats this as a perfectly normal operational situation.

**Format:** Top-down beat em up. 2-3 minutes. Fists only.

### Rules for All Surprise Levels
- Short and punchy — 2-3 minutes maximum
- The surprise is the point, not the depth
- Co-op chaos must carry over — these are better with friends
- Must feel like a genuine expression of the guest genre, not a parody

---

## THE RUN STRUCTURE

```
START
  │
  ├── Chapter 1: The Cretaceous (~10 min, 6-8 segments)
  │   └── Villain: Baroness Cretacia
  │
  ├── SURPRISE: The Gradius Level — The Commute (~2 min)
  │
  ├── Chapter 2: Dead Station (~10 min, 6-8 segments)
  │   ├── Villain: Professor Static
  │   └── [Always contains the guaranteed destructible level]
  │
  ├── SURPRISE: The Brawler Level — The Layover (~2 min)
  │
  ├── Chapter 3: The Infernal Keep (~10 min, 6-8 segments)
  │   └── Villain: Lord Inferno
  │
  └── FINAL BOSS [deferred]
```

Total runtime: ~30 minutes for a clean run.

---

## THE LEVEL GENERATOR

### Philosophy
The generator's job is to deliver the chaos, not describe it. It produces a **semantic macro structure** — a description of intent — which a separate importer layer translates into actual Godot TileMap geometry. These two concerns never mix.

### The Layered Pipeline
```
Master Spine (fixed narrative sequence)
      ↓
Chapter Generator (themed, parameterized)
      ↓ emits JSON
Macro Validator (solvability, budget checks)
      ↓
Godot Importer (semantic → TileMap)
      ↓
Micro Generator (WFC fills geometry within constraints)
```

Each layer is independently testable and replaceable.

### The Solvability Contract
The generator guarantees:
1. Every segment is completable by a sliding Giant player
2. The critical path is never blocked by destructible geometry
3. Players always have an escape route — never fully cornered
4. Entry points drop players into mild action immediately
5. Exits are visible or strongly implied from the entry point

### Segment Schema (Semantic, Not Geometric)
Each generated segment describes **intent**, never coordinates. Example fields:

- `chapter` — which chapter context applies
- `type` — opening / combat / platforming / breather / setpiece / boss
- `geometry_profile` — open / corridor / hybrid
- `ceiling_present` — boolean, changes combat feel entirely
- `destructible` — none / partial / heavy
- `difficulty_budget` — normalized 0.0–1.0
- `hazard_class` — environmental / enemy / both
- `platform_motivation` — tactical / traversal / dramatic
- `sightline_rating` — short / medium / long
- `reward_nodes` — count and type of power-up placements
- `enemy_roster` — tagged enemy types from chapter pool

### Difficulty
Difficulty is **scripted**, not dynamic. The difficulty curve is baked into the segment sequence. Better gear makes the game more chaotic and fun, not easier in terms of segment design. Enemy counts scale with player count. Geometry does not change.

---

## VISUAL AND AUDIO IDENTITY

### Art Style — The Target

**16-bit pixel art. Cartoony but not soft. Crisp and confident.**

The two reference points that bracket the target:

**Jurassic Park 2: The Chaos Continues (SNES, 1994)** — too dark, too moody, too drab. The graphics are detailed and technically competent but visually heavy. Desaturated palette, gritty atmosphere, realistically proportioned sprites that read as serious and murky at a distance. This is the ceiling on grimness. We are not this game.

**Joe & Mac: Caveman Ninja (SNES, 1991)** — great color energy, bright saturated backgrounds that pop, characters with genuine personality. But the sprites are chunky and primitive, the proportions squat and rubbery, and the overall feel is a little too goofy and soft. The color instincts are right; the execution is too loose. This is the floor on cartoon sloppiness. We are better than this game.

**The target lives between them:** Joe & Mac's color confidence and cartoon personality, with JP2's level of detail and visual seriousness — but crisper, more intentional, and more modern-feeling than either. Think late-era SNES quality: the kind of pixel art that looks designed rather than drawn by committee. Characters read instantly at a distance. Colors are saturated but purposeful. Nothing is muddy. Nothing is soft.

### Pixel Art Philosophy
Small pixels on a big screen. The pixel art is very small relative to the play area — giving the game a surprising density of content that reads as crisp rather than cluttered. Because the camera is wide and the sprites are small, every character and enemy must have an immediately readable silhouette. This is a hard constraint: if you can't identify what something is from its outline alone, the sprite is not finished.

The small scale is a feature, not a limitation. It allows far more content on screen than a typical pixel art game without feeling crowded — but only if the art is clean enough to survive being small.

### Character Art Direction
Cartoony proportions — slightly larger heads, expressive poses, readable at thumbnail size. Not chibi, not realistic. Think the confident cartoon style of late-era Mega Man or early Metal Slug: characters that are clearly stylized but never feel accidental. Personality over realism. Every character should be identifiable from their silhouette alone.

### Camera
Wide angle, distant, always shared. All players on one screen at all times. Co-op players must stay roughly together — the camera does not split. This is both a technical constraint and a design intention: you are in this together.

### Chapter Visual Identity
- **Chapter 1** — saturated, bright, outdoor. Greens, oranges, blue skies. Dinosaur bone structures, jungle canopy, volcanic rock. Joe & Mac's color energy applied with more precision.
- **Chapter 2** — darker and more industrial than Chapter 1, but never as drab as JP2. Greys and sickly greens with deliberate accent lighting. Metal corridors, flickering lights, alien detail. Moody but readable.
- **Chapter 3** — dramatic contrast, volcanic palette. Deep reds, black stone, orange lava glow. The most visually intense chapter. Epic scale.

---

## CUSTOMIZATION AND COMMUNITY

### Player Customization
- Upload a photo of your face as your character portrait
- Design custom weapon skins
- Cosmetic unlocks earned through runs
- All customization is **cosmetic only** — no stat implications

### Custom Weapon Balance Rule
Custom weapons are reskins of existing weapon archetypes, not arbitrary stat profiles. The destructible level balance system must not be breakable by custom content.

### Level Editor
The level editor and the generator write **identical JSON**. The editor gives a human the controls to write the macro structure manually. The generator automates it. They are the same system with different input methods.

The editor is also the generator's debugging tool — inspect, tweak, and re-run any generated level.

Steam Workshop support is a long-term target. Community levels extend the game's life indefinitely.

---

## STORY ARCHITECTURE

### The Three Eras
The three chapters are three conquered eras of Earth's history. Each chapter boss is a separate villain who has occupied and subjugated that era. The villains are connected by a single shadowy sponsor — The Laugh — whose presence is felt exactly once per chapter, always after the boss exits.

The crew has been cleaning up middle management. They have not met the CEO yet.

### Villain Design Rules
All chapter villains follow the same rules:
- Comically, sincerely evil. They WANT world domination. They are simply bad at it.
- Personality-first. Each is a specific flavor of unhinged. The personality IS the boss fight.
- Never actually hurt anyone. The incompetence is structural, not incidental.
- Defeated in a way that is spectacular but inconclusive. Maybe 60% certain they are gone.
- Exits screaming, laughing, and making promises. All simultaneously.
- The game never confirms they are dead. The game never confirms they are alive.

### The Recurring One
A minor villain so thoroughly defeated in Chapter 1 who keeps showing up. Different outfit each time. Same energy. The crew recognizes them immediately. Never the main boss. Always dispatched. Always returns. Already planning the next attempt.

### The Laugh — The Shadow Sponsor
Behind all three chapter bosses is a single presence: The Laugh.

**Rules for The Laugh:**
- Heard exactly once per chapter. Always after the boss exits. Always brief.
- Never reacts to the player. Does not know the crew exists yet.
- Slightly too calm for the situation.
- Gets one frame closer to a suggestion of shape each chapter — never a reveal. A shadow. A silhouette that is gone before it can be read.
- The villains never mention The Laugh. It simply happens.
- By chapter three the player is genuinely unsettled without being able to explain why.

The less defined, the better. The player's imagination fills in something worse than anything that could be shown. Dr. Claw works for exactly this reason.

**The expansion hook:** The Laugh becomes The Voice. Still no face. Now talking directly. Has been running a much larger operation the entire time.

### Setpiece Moments (Outlined, Not Finalized)
- **Chapter 1** — a stampeding brachiosaurus chase
- **Chapter 2** — the tilting space station reactor sequence
- **Chapter 3** — the volcanic fortress collapse during the final approach
- **Final boss** — three-phase era destruction sequence (proposed, not locked)

*All setpieces are deferred to a separate design session. They must feel discovered, not designed by committee. They need adrenaline.*

---

## META PROGRESSION

- Light unlocks — cosmetics, character styles, weapon skins
- Permanent unlocks earned through specific run achievements
- Each chapter has one optional challenge segment that gates a cosmetic unlock
- The hook is the chaos itself — players return because runs are genuinely different and genuinely fun, not because a Skinner box demands it

---

## TECHNICAL NORTH STARS

- **Engine:** Godot 4, C# (.NET)
- **Distribution:** Free. Always. Steam or equivalent, no price tag ever.
- **Co-op:** Local only, same screen
- **Save Format:** All level data is serialized JSON — runs are reproducible from a seed
- **Asset Pipeline:** Fully data-driven from day one. No hardcoded asset references. Every visual element has a slot that accepts custom assets.
- **Generator Output:** Semantic JSON, human-readable, Claude-legible
- **Solo Developer:** Every architectural decision should reduce future workload, not increase it
- **Framework First:** The engine is built as a reusable framework. This game is its first tenant, not its only one.

---

## ENGINE ARCHITECTURE — THE SEPARATION PRINCIPLE

### The Core Rule

**The engine has zero knowledge of this game.** It knows nothing about dinosaurs, honey badgers, zombie aliens, or lava fortresses. It knows about entities, segments, budgets, signals, and rules. Feral Frenzy is content that runs on top of the engine. A future game would be different content running on the same engine.

Any line of engine code that contains a Feral Frenzy-specific assumption is an architectural violation.

### The Three Layers

```
┌─────────────────────────────────────────┐
│           CONTENT LAYER                 │
│  (Feral Frenzy-specific, freely changed)  │
│                                         │
│  • Chapter definitions (JSON)           │
│  • Character definitions (JSON)         │
│  • Enemy roster and behaviors (JSON)    │
│  • Weapon definitions (JSON)            │
│  • Villain/boss data (JSON)             │
│  • Tileset and asset references         │
│  • Difficulty curve data                │
│  • Master spine sequence                │
│  • Destructible level rules             │
│  • Surprise level modules               │
└──────────────────┬──────────────────────┘
                   │ feeds
┌──────────────────▼──────────────────────┐
│            IMPORTER LAYER               │
│   (The seam — only place Godot         │
│    touches game content)                │
│                                         │
│  • JSON → Godot nodes/tilemaps         │
│  • Content schema → Engine schema       │
│  • Asset slot resolution                │
│  • Signal registration per game        │
└──────────────────┬──────────────────────┘
                   │ drives
┌──────────────────▼──────────────────────┐
│            ENGINE LAYER                 │
│        (Game-agnostic, reusable)        │
│                                         │
│  • Physics and collision                │
│  • Movement primitives                  │
│  • Camera system                        │
│  • Tilemap rendering                    │
│  • Entity/component system              │
│  • Input handling                       │
│  • Audio engine                         │
│  • Save/seed system                     │
│  • Particle and effects system          │
│  • WFC micro-generator                  │
│  • Macro generator framework            │
│  • Level editor shell                   │
│  • Signal bus                           │
└─────────────────────────────────────────┘
```

### The Godot Constraint

Godot's scene system wants to own everything. The discipline required here is: **scenes are rendering containers, not game logic holders.** A scene does not know it is a Chapter 1 level. It knows it is a tilemap with entities. The content layer tells it what to render. The engine layer tells it how.

Godot resources (.tres, .res) are appropriate for assets. Game data lives in JSON. The engine layer communicates with Godot. The content layer communicates with the engine layer. Godot never sees a honey badger.

### Godot's Signal System as the Seam

Engine events fire as Godot signals — entity spawned, segment loaded, player died, destructible geometry triggered. Feral Frenzy-specific listeners respond to those signals. The engine does not know who is listening. It just fires.

A future game registers different listeners for the same signals. The engine is unchanged.

### The Two JSON Namespaces

**Engine schema** — stable, versioned, never game-specific. Describes segments, entities, budgets, solvability constraints, generation rules. Changes to this schema are breaking changes that require migration.

**Content schema** — game-specific, freely changeable. Describes chapters, characters, enemies, weapons, story beats. Feral Frenzy owns this namespace. A future game defines its own.

The importer translates content schema → engine schema. Nothing else crosses that boundary.

### The Practical Tension

Godot 4's C# ecosystem assumes GDScript in much of its documentation and community examples. Maintaining the separation discipline will occasionally feel like swimming upstream — tutorials will show the "easy" way that couples content to engine. **Resist this.** The short-term friction is worth the long-term flexibility. When in doubt, ask: *would this code make sense in a game about space trading?* If no, it belongs in the content layer.

### What a Future Game Looks Like

A second game built on this engine would:
1. Write a new content layer in JSON
2. Register new signal listeners for its own game events
3. Provide its own tilesets and asset slots
4. Reuse the entire engine layer unchanged — WFC generator, macro spine, camera, physics, level editor shell, save system, all of it

The engine is the investment. Feral Frenzy proves the investment is sound.



## WHAT WE DO NOT DO

- We do not charge money. For anything. Ever.
- We do not put content behind a paywall
- We do not dynamically adjust difficulty mid-run
- We do not split the co-op screen
- We do not punish players with permanent run loss from a single death
- We do not require specific weapon loadouts to complete any segment
- We do not produce segments where players spend 10 seconds figuring out what to do
- We do not make powerful weapons weaker — we make powerful weapons consequential
- We do not make the meta progression the reason to play — the run is the reason to play
- We do not optimize for retention, engagement metrics, or session length — we optimize for joy

---

## OPEN QUESTIONS (DEFERRED)

### Villain Design Session
- Full personality deep-dives for Baroness Cretacia, Professor Static, Lord Inferno
- The Recurring One — full arc across all three chapters
- Boss fight structure and mechanical vocabulary for each villain
- The Laugh — exactly how present, exactly how disturbing

### Setpiece Design Session
- Chapter 1 setpiece — the stampeding brachiosaurus chase (outlined, not finalized)
- Chapter 2 setpiece — the tilting space station reactor sequence (outlined, not finalized)
- Chapter 3 setpiece — the volcanic fortress collapse during the final approach (outlined, not finalized)
- Final boss structure — three-phase era destruction sequence (proposed, not locked)

### Character Design Session
- Names for the four crew members
- Individual character personality deep-dives
- How characters interact with each chapter's specific boss
- The fifth character — identity, play style (Balanced), unlock conditions

### Weapon Design Session
- Charging mechanic — belongs to specific weapons, not characters. Full weapon list TBD.
- Tier 3 unlock pool — which weapons, how discovered
- How weapon pickups scale visually with character size

### Technical / Infrastructure
- Exact run restart conditions for solo vs. co-op edge cases
- Workshop moderation policy for community levels

---

*"The rocket launcher is fine. Just watch the bridge — then go back to having a great time."*
*— design philosophy*
