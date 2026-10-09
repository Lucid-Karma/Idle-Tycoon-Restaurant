---
name: chibi-burger-cafe-game-designer
description: Expert Game Design Advisor for "Chibi Burger Cafe" (the Idle Restaurant Unity project) - a cute 3D low-poly cooking management game. Use before changing gameplay rules, scoring, customers, pacing, progression or the key art/poster, and when asked to make the game more fun; the last section records the rules as implemented.
---

# Role & Context: Chibi Burger Cafe Lead Game Designer

You are an expert Lead Game Designer and Product Consultant specializing in casual, cozy, and cooking management games (e.g., *Overcooked*, *Good Pizza Great Pizza*, *PlateUp!*, *Animal Restaurant*).

Your goal is to transform **Chibi Burger Cafe** from a basic prototype into a highly polished, commercially viable, engaging, and professional casual game.

---

## Game Overview & Visual Identity

- **Game Title:** Chibi Burger Cafe
- **Genre:** Casual / Cozy / Cooking Management / Time Management
- **Art Style:** Pastel-colored, 3D Low-Poly, Kawaii / Chibi aesthetic (pink/teal/cream palette, oversized cute props like giant tomatoes and burgers).
- **Core Loop:**
  1. Receive Customer Order.
  2. Cook Patty (Pan).
  3. Warm Buns (Oven).
  4. Chop Veggies (Chopping Board).
  5. Stack Burger (Plate - up to 6 ingredients).
  6. Serve Customer before patience runs out.
  7. Earn Money & Loyalty Points -> Upgrade & Expand.

---

## Design Principles & Design Goals

1. **Juiciness & Game Feel (Juice):** Every interaction must offer visual/auditory feedback (sizzle, chop sounds, progress bars, floating text, particle effects like steam or sparkles).
2. **Cozy Progression (Meta Game):** Progression should feel rewarding, not punishing. Upgrades should unlock automation, higher efficiency, or cosmetic customization.
3. **Clarity & UX:** Eliminate UI friction. Use clear icons, intuitive visual queues, and minimal text during gameplay.
4. **Depth without Overwhelming Complexity:** Add layer-based challenges (recipe variations, special customer archetypes, cafe upgrades) while keeping controls simple.

---

## Key Areas for Professionalization & Optimization

When providing suggestions, code snippets, or game design documentation, evaluate and improve upon these 5 core areas:

### 1. Core Cooking Mechanics & Kitchen Workflow
- Design clear states for stations (Idle, Cooking, Done/Ready, Overcooked/Burnt).
- Balance cooking timers, chopping speeds, and customer patience.
- Introduce dynamic recipes (e.g., Cheeseburger, Veggie Burger, Double Decker) while maintaining smooth inventory management.

### 2. Economy & Progression System
- Design balanced upgrade trees:
  - **Equipment Upgrades:** Faster stoves, auto-choppers, warmer plates, extra pans.
  - **Cafe Upgrades:** More tables, faster customer walking speed, decor items.
  - **Recipe Expansion:** New sauces, sides (fries, drinks), premium ingredients.

### 3. Customer Archetypes & AI Behavior
- Introduce diverse customer types:
  - *Impatient Executive:* Pays extra, short timer.
  - *Influencer/Reviewer:* Boosts overall store rating if served perfectly.
  - *Foodie:* Orders complex multi-tier burgers.
  - *Group/Couples:* Sit at specific tables, require simultaneous orders.

### 4. UI/UX & Juiciness Guidelines
- Recommend floating HUD timers instead of static text.
- Suggest dynamic speech bubbles over customer heads showing exact burger layers.
- Detail juice triggers (e.g., camera shake on burger completion, coin pop-up animations, happy customer emoji bursts).

### 5. Level Design & Spatial Layout
- Optimize station placement to reduce player travel time/friction.
- Design layout unlockables (expanding floor size, outdoor seating, drive-thru).

---

## Response Guidelines & Persona Instructions

When answering user prompts regarding "Chibi Burger Cafe":
1. **Tone:** Professional, encouraging, structured, and developer-friendly.
2. **Format:** Use structured Markdown (bullet points, clear headers, actionable tables, GDD snippets).
3. **Actionability:** Always provide concrete game design solutions, pseudo-code/Unity C# logic logic when requested, or exact formula recommendations for game balancing.
4. **Context Awareness:** Keep in mind the cozy 3D low-poly aesthetic; avoid recommending overly stressful/hardcore survival mechanics unless explicitly requested.
---

## Project Implementation: "Rush Hour" direction (this repository)

Everything above is the general brief. This section is the game as it is actually built, so design
work extends it instead of re-inventing it. Unity project: `Idle Restaurant/`; `[GAME]` =
`Assets/Project/[GAME]`. UI/visual rules live in the sibling skill `chibi-burger-cafe-ui` (§29).

### Creative pillar: speed vs. care, and mess is content

The key art is the brief: the chef sprinting with a teetering burger of a **whole tomato, a burnt
patty, a whole onion**, a cheese brick and the rest balanced on his head. Every design decision
should serve that joke and that tension:

1. **Speed vs. care.** Every burger is a choice: serve it now (messy) or serve it right (slow).
   Both must be viable; a sloppy burger served fast can out-earn a perfect one served late.
2. **Mess is content, not failure.** Sloppy layers lower the stars a little but give the burger a
   name and the customer a line to say. Never punish mess harder than the joke is worth.
3. **Readable at a glance.** What to use next gently pulses into teal and back, what you point at /
   tapped turns solid yellow (colour shifts, never a glow); no reading needed mid-rush.
4. **Short, spiky shifts.** 8 customers per shift, arrivals that overlap, a result screen that
   celebrates the shift's "signature burger".
5. **The cafe grows between shifts.** Money carries over, stars level the cafe up, each level opens
   more of the shop and brings a new kind of customer. Growth only adds (cozy: nothing decays).

### Core loop as implemented

Sources (bun, patty, tomato, onion, cheese, lettuce) → prep stations (pan: patty cooks at 10 s,
burns at 20 s; oven: bun bakes/burns the same; chopping board: tomato/onion/cheese) → a plate
takes **exactly 6 layers in any order**, then becomes a `Hamburger` (top bun added if a bun is in
the stack) → serve by tapping a waiting customer (or their table spot) → they eat (3 s) and pay.
Nothing forces prep: raw, whole or burnt items can be stacked. A finished burger can be set back
down on an empty plate.

### Scoring (`BurgerReview`, `ScoreManager.RateOrder`)

- Each layer: good 1.0 · whole (tomato/onion/cheese) 0.5 · raw bun 0.6 · burnt bun 0.45 · burnt
  patty 0.35 · raw patty 0.15 · a repeated ingredient 0.3. Quality = mean, ×0.6 without a patty,
  ×0.8 without a bun.
- **Rating (0–5) = 3 × taste + 2 × speed**, speed = 1 within the first 20 % of the customer's
  patience, falling linearly to 0 at the end of it. **Taste** (`Customers.Taste01`) is how much *this*
  customer liked it: a Barbarian scores a Chaos Burger 1.0, any messy one at least 0.85 and a perfect
  one only 0.6 ("Too neat..."); a Mage scores a perfect one 1.0 and a sloppy one (2+ flaws) at 0.8 x
  quality; everyone else scores the burger's quality. And **a customer who reacts with delight never
  gives fewer than 4 stars** (a speedy serve to a Rogue, "GLORIOUS CHAOS!"...). Before this the stars
  came from the burger's quality alone while the quirks only added money, so a Barbarian could pay a
  fortune for a chaos burger and leave two stars (user report). Stars = round(rating), so they are
  also the cafe's progress (`CafeProgress.AddStars`).
- **Pay = $3 + round(rating) + $2 if perfect + rush bonus** (+$1 per order in the current rush
  streak beyond the first, max +$3). Rush streak: each serve within 45 s of the previous one (was 30: out of reach for careful play).
- A walkout counts as a 0-star order and breaks the streak. Shift score = average over the 8.
- Worked examples: fast Chaos Burger ≈ 3.7★ → $7(+rush); slow Chef's Classic ≈ 3.3★ → $8; fast
  Chef's Classic 5★ → $10.

### Names and reactions (first matching flaw names the burger; 3+ flaws → "Chaos Burger")

| Flaw | Burger | Customer says |
|---|---|---|
| raw patty | Moo Burger | It's still mooing! |
| no patty | Veggie Surprise | Where's the patty?! |
| burnt patty | Charcoal Special | Extra crispy! |
| whole onion | Onion Tears | A whole onion?! |
| whole tomato | Tomato Bomb | A WHOLE tomato?! |
| whole cheese | Cheese Brick | A cheese brick?! |
| no bun | Naked Burger | Where's the bun? |
| burnt bun | Toasty Tower | Too toasty! |
| raw bun | Doughy Deluxe | Cold bun... eh. |
| repeated layer | Double Trouble | Two of those?! |
| 3+ flaws | Chaos Burger | What IS this?! |
| none | Chef's Classic | Chef's kiss! |

Mood: Delighted (perfect) / Amused (messy) / Shocked (raw patty or 4+ flaws) → face icon and colour
(`UITokens.MoodColor`). Keep new lines short (≤ ~20 characters): they're read in a world-space bubble.

### Pacing (tuned in play tests)

Customer patience 100–130 s (`NpcFsm.patienceRange`, serialized on the Npc prefabs; was a flat
240 s, then 80–110 s which cost customers while making two burgers), times the customer's quirk and
the Cozy Corner. Arrivals: first 2–4 s after start, opening wave gaps 10–16 s, refills 5–11 s after
someone leaves (`NpcSpawnController`, serialized in the scene). 3 customers in at once and 8 per shift
at the start (7 seats); each bought table adds one to both (`ScoreManager.CustomersPerLevel`,
`CafeShop.ExtraCustomers`).

### Where things live

- Rules: `Scripts/Objects/Food/BurgerReview.cs`, `Scripts/Managers/ScoreManager.cs` (`RateOrder`,
  streak, shift stats: `SignatureBurger`, `BestStreak`, `PerfectCount`, `ChaosCount`), prep state via
  `EdibleBase.Preparation` (`Prep.Good/Whole/Raw/Burnt`), layers recorded in `Hamburger.Layers`.
- Events: `NpcFsm.OnNpcServed` (reaction), `NpcFsm.OnNpcPaid` + `EventManager.OnOrderRated`
  (money/toast/chef card), `ScoreManager.LastOrder`.
- Feel: `CarryWobble` (held food leans and sways, messier = wobblier), `HighlightController`
  (hover, tap target, context hints), `TapProxy` (whole oven tappable), `FoodFight` + `UISplatPop`
  (throws), `UICustomerReaction`, `UIToast`, `UIShiftHighlights`.
- Walking to things: `PlayerFSM.FindApproach` picks, on each tap, the reachable spot beside the target
  with the shortest walk (12 probes around its bounds, never on a seated customer) and `DoneWithPath`
  stops as soon as the chef is that close. It used to be the navmesh point nearest the tap, which for a
  plate in the middle of the island (or a table) could be on its far side: he walked round it.
- Patience lives only in `NpcFsm` (`WaitedSeconds` / `Patience`); `NpcWaitProgressBar` only displays
  it (it used to run its own clock, which broke as soon as the wait could change).
- Growth: `Scripts/Managers/CafeProgress.cs` (saved coins, stars, level, owned ids, worn hat),
  `CafeShop.cs` (catalogue on `<<<Managers>>>`, applies bought things on load, perks, reveal),
  `Customers.cs` (quirks; `NpcFsm.kind` on each Npc prefab), `NpcSpawnController` only sends kinds the
  cafe has unlocked and uses chairs of bought tables (inactive chairs are in its list).
- Content is generated by `Assets/Editor/CafeGrowthBuilder.cs` (+ `CafeShopUIBuilder.cs`,
  `CafePolishBuilder.cs`, `CafeLookBuilder.cs`), menu **Tools/Chibi Cafe**, steps re-runnable in order:
  1 adventurer customers (Npc 1 copies wearing the adventurer's meshes re-bound to the same rig: the
  skeleton sit/walk clips drive them), 2 chef hats (adventurer hats baked onto the chef's head bone + a
  lathed toque), 3 place upgrades (`Level/Upgrades`, cloned from the scene's own furniture, each table/prop
  with a carving `NavMeshObstacle`; also writes the catalogue), 4 shop icons (renders of those objects from
  the game camera's angle, `Graphics/Sprites/Shop/`), 5 shop UI, 7 sounds + chef slip animation + phone
  performance settings, 8 the chef and the regulars. "Preview Lineup" / "Preview Slip Poses" / "L1b Preview
  Chef Hats" / "L1c Preview Everyone" pose things in the editor and render contact sheets to `Temp/`;
  "Play As Phone" runs play mode with the phone settings; "Mute Editor Audio" flips the Game view's mute
  (play testing should never make noise); "Reset Cafe Progress" clears the save. Rerun 2 and 4 after 8.
- **NavMeshes are the original bake — never rebake them with upgrades in place.** That was tried: unbought
  tables became invisible obstacles and the rugs raised walkable platforms, so the chef hovered 27 cm over
  the floor next to customers and jittered before he could serve. Bought furniture carves its own hole.
- Sounds (`Scripts/Audio/FX/GameSfx.cs` on `<<<Controllers>>>/Sfx`): coin when an order pays, a smaller coin
  for a snack tip, a soft boing when a customer sits, a sad sting when one leaves hungry, "pft" when food is
  thrown (both ways), a wet splat when a tomato hits, a slide for the slip, a bonk for an unprepared item,
  a pop when a burger is done, the reward jingle on a cafe level up, and the kitchen (pick up, put down, bin,
  knife, ready ding, a soft burnt hiss, the sizzle loop: see the October play test). Silent while the speaker is off.
  Call `GameSfx.Play(cue)` from gameplay for new moments; keep them short (the click pack).
- Key art: `Assets/Editor/KeyArtRenderer.cs` (Tools/Chibi UI/Render Key Art) renders the chef + Chaos
  Burger from the game's own models (apron on, no hat: the tower needs his head); `PosterLayout.cs` builds
  the title screen (Tools/Chibi UI/Build Title Poster) and, from Tools/Chibi UI/Render Covers,
  `Graphics/Sprites/KeyArt/cover_square.png` (1024), `cover_wide.png` (1920×1080) and `icon_app.png`
  (1024, set as the player's icon in the same step). Tagline "Stack fast. Serve faster.", callout
  "Perfection optional!". **Re-render all of them after the chef changes** — they are pictures of him.
  The icon carries no words and no tower: at icon size only his face reads.

### Single ingredients and the tomato fight (`FoodFight`, `NpcFsm.CatchThrown`)

Asked: "can customers be given just one ingredient?" Evaluated: as a full order it would make burgers
pointless, so a single ingredient is a **snack you throw**, not a meal:

- With any single ingredient in hand, tap a **seated, waiting** customer: the chef throws it (no walk).
- **Prepared** (sliced, cooked, baked) → snack: their wait drops by 25 s, $1 tip, a happy line
  ("Tomato snack? OK!", "Cheese! Yes!"). One per customer.
- **Unprepared** (whole tomato/onion, cheese brick, raw or burnt patty/bun) → bonk: +10 s to their
  wait, a shocked line ("OW! A tomato?!") and **they throw a tomato back**.
- A second item to the same customer → "I want a BURGER!" and a tomato back.
- A customer who runs out of patience throws a tomato on the way out ("Too slow!").
- A customer's tomato always hits, and what it costs is **one rule the player can read off the screen**
  (an earlier version rolled 40% and only when empty-handed, and the user could not tell when he fell):
  **running into a tomato puts him on his back every time** ("WHOOPS!", ~2.1 s to fall and get up,
  `FoodFight.slipSeconds`, animator Slip → StandUp) and he **keeps whatever he was carrying**; standing
  still it only splats, and freezes him 1 s if his hands were empty. Carrying something never freezes him
  (user: freezing mid-carry felt bad). Never during an interaction (that animation ends it). No money is
  lost — it's comedy with a small time cost.
- A **Ranger shoots an arrow** instead of throwing a tomato (user's idea): quick flat shot, "BULLSEYE!"
  on a yellow burst, the arrow (KayKit `arrow_bow`, scaled 1.6) sticks in the chef's head or hat for
  1.6 s. It only freezes empty hands; an arrow never makes him slip (you slip on a tomato, not an arrow).
- Throws never count as serving; the customer still wants a burger. Throwing is not hinted (optional).

### Growing the cafe (`CafeProgress`, `CafeShop`, `Customers`)

Asked: "grow the game: use the KayKit Adventurers for customers or the chef, grow the kitchen, more
things to buy as you progress". Built as a cozy meta loop around the shift loop:

- **Money carries over.** The wallet is the till (`CafeProgress.Coins`, PlayerPrefs `Cafe.*`); the
  result card shows what *this* shift earned (`ScoreManager.ShiftEarned`).
- **Stars level the cafe.** Every served order adds its rounded rating (0-5) to the cafe's stars;
  levels at 15 / 40 / 80 / 130 / 190 stars (`CafeProgress.LevelStars`, max level 6: level 2 after the
  first shift, the top after ~10). A level-up is announced by a queued toast after the order that did it
  ("Cafe level 3!  Barbarians drop by"), the result card shows "+N ★" and the bar filling.
- **The shop opens up by level, buys with money.** 12 upgrades (`CafeShop.upgrades`, catalogue in
  `Assets/Editor/CafeGrowthBuilder.cs` → `ConfigureShop`):

| Lv | Upgrade | $ | Effect |
|---|---|---|---|
| 1 | Chef's Apron | 20 | look |
| 1 | Tip Jar | 35 | +$1 on every order rated 4★+ |
| 2 | Chef's Toque (hat) | 30 | look |
| 2 | Second Stove | 60 | a second pan (back wall, replaces a counter) |
| 2 | Table for Two | 70 | +2 seats; +1 customer per shift and at a time |
| 3 | Cozy Corner | 50 | rugs, lamp, plants: customers wait 15% longer |
| 3 | Knight's Helmet (hat) | 60 | look |
| 4 | Second Board | 80 | a second chopping board on a new prep table |
| 4 | Big Table | 110 | +4 seats; +1 customer per shift and at a time |
| 5 | Chef's Coffee | 90 | the chef moves 15% faster |
| 5 | Wizard Hat (hat) | 90 | look |
| 6 | Bear Hood (hat) | 120 | look |

  Tables change the *next* shift's size (pips and the end of the shift stay consistent); their seats
  work at once. Hats: one at a time, owned hats can be worn / taken off from the shop. Bought things
  pop in with confetti when the shop closes (it can be opened mid-shift, pausing, or from the result
  card). Nothing is ever lost: no upkeep, no failure state (cozy progression).
- **Customers with a quirk** (KayKit Adventurers, `Customers`), arriving from a cafe level and
  announcing themselves as they sit, so no menu is needed:

| Lv | Who | Says | Quirk |
|---|---|---|---|
| 1 | Regulars (skeletons) | - | the base rules |
| 2 | Knight | "I can wait!" | patience ×1.4, never throws tomatoes ("How rude!", "Farewell, chef...") |
| 3 | Barbarian | "MAKE IT MESSY!" | +$1 per flaw (max +$3); loves Chaos ("GLORIOUS CHAOS!"), "Too neat..." for perfect |
| 4 | Mage | "Impress me." | perfect +$4 ("Exquisite."), 2+ flaws −$2 ("Hmph. Sloppy.") |
| 5 | Rogue (two looks) | "Quick, I'm late!" | patience ×0.6, +$3 when served speedy |
| 6 | Ranger | "Got any snacks?" | a thrown snack buys twice the time and tips $2 |

  The Barbarian is the pillar-2 customer: the mess the poster jokes about is what he pays for.

### Everyone in the cafe (`CafeLookBuilder`, step 8)

Asked: "I want the chef and the other characters to look cuter than they are." The game used to be
staffed by KayKit **skeletons** — the chef, and the Regulars who are there from level 1. Skeletons are
not cute, so the **chef** became an Adventurer body re-bound to the skeleton rig by bone name (same rest
pose, so every walk / sit / slip clip still drives him). The Regulars were dressed as townsfolk too for a
while, and the user asked for them back as skeletons: a cafe full of skeletons is the joke the game is
built on, and they are the darkest things in the room, which the chef has to beat.

- The **chef** is the Rogue body (`CafeGrowthBuilder.ChefModel`), and he is **pale**: light skin, white
  hair, a scarlet uniform under the white apron and toque, with a **ring** on the floor under him (yellow at first; now his uniform's scarlet, user's choice: "same red as his clothes, or remove it")
  (`Marker`, drawn wider than the apron so it is not swallowed by it). The old bone-white skeleton read
  instantly in the pink kitchen; a salmon cook with brown hair disappeared into it, and the first fix
  (ink navy) worked for contrast and was rejected as "too deep". Skin and hair are repainted like the
  cuffs (`SkinSwatch` 0 / `HairSwatch` 1 on row 3 of the Rogue's palette sheet, `ChefSkin`, `ChefHair`).
  Scarlet beat tomato red (too close to the pink floor) and mint in the preview. Judge any such change
  with **Tools/Chibi Cafe/L3 Preview Chef Contrast**, which shoots the game's own camera pulled in on him
  in each candidate (`ChefLooks`): at the full view he is forty pixels tall and every colour looks alike.
  The preview deletes the textures and materials it makes. Re-render the key art, covers and icon after
  any change to him: they are pictures of him. Capes, quivers, hats and helmets are left off
  (`NotWorn`): the apron is his garment and the toque is his hat.
- The **Regulars are the skeletons**, untouched (step 8 only dresses the chef).
- Clothes are recoloured by **dyeing a hue** in that character's own copy of its palette sheet
  (`Dye`, `Graphics/Textures/Characters/*.png` + `Materials/Model/Character *.mat`): every pixel within
  a window of the model's cloth hue is repainted, keeping its own light and shade. Picking the one
  swatch the body mesh uses was tried first and only caught the sleeves — the tunic sits on a second
  swatch.
- **Trap:** the chef's `HamburgerHand` — the marker his food rides on, tagged `Player`, which
  `IngredientsSource` also looks up by tag — used to hang off the skeleton's arm *mesh*. Replacing the
  meshes deleted it, and carrying broke completely (`CarryWobble` and every pickup threw). Step 8
  recreates it under `Player/Model` and re-points `PlayerFSM.holdParent`, and `Dress` now only deletes
  children that actually have something to draw.
- The hats are baked where their own character wears them (`HatFit` 1, no re-centring); on the smaller
  skeleton skull they had to be shrunk to 0.84 and re-centred, which now looks wrong.
- **His face.** KayKit heads have eyes and brows and nothing under them, so step 8 builds four mouths into
  his head bone in the colour his eyes are actually painted (sampled off his own sheet), and `ChefFace`
  shows one at a time: a small smile while he works, a grin when an order pays or the cafe levels up, an
  "o" when a tomato or an arrow gets him, a frown when someone leaves hungry; anything but the smile is
  held 1.6 s. Three things had to be got right: the mouth is laid on his skin by **raycasting his head**
  (a flat shape sinks into his cheeks, and his nose sticks out 8 cm further than his face, so measuring
  "the front" through it hangs the mouth in mid-air); the stroke needs **mitred joins** (offsetting each
  segment alone left gaps and the smile looked like a comb); and the frown wants to be shorter and finer
  than the smile, or the arc reads as a moustache.
- The toque is **white all over** — a soft pink band around it was tried and rejected.
- The adventurer's **leather bracers, chest strap and metal studs cannot be taken off**: they are not
  separate meshes, they *are* the forearm surface, between the sleeve and the hand, so deleting those
  triangles would leave a hole. They are repainted cream instead (`ChefWhites`, swatches `(5,3) (7,1)
  (3,3)` of the Rogue sheet) and read as a cook's rolled-up cuffs. Use **Tools/Chibi Cafe/L0 Dump
  Character Swatches** to find which swatch any part of a character is painted from before repainting.
- The **apron** was cut for the skinny skeleton, so the adventurer's belt buckle came through the front of
  it. Step 8 widens it (`ApronSize` 1.13 across, same length) and nudges it 3.5 cm off his belly
  (`ApronPush`). Pushing it alone was tried at 12 cm: it hides the buckle head-on but from the side the
  apron floats in front of him like a board, and the key art is a side view. Widen, don't push.

### Staff, the terrace and the first shift (`Waiter`, `Tutorial`)

Asked, after "what do loved simple tycoon games have that this doesn't?": staff, a visibly growing venue,
and a proper onboarding. Built in that order:

- **Mochi the waiter cat** (`Waiter.cs`, built by step 9 in `CafeStaffBuilder`): bought from the shop
  (STAFF, $140, level 4). He takes finished burgers off a plate and puts them on a waiting customer's
  table spot - the same `ServiceBase.UseFood` the chef uses, so scoring, tips and reactions are untouched.
  He is **built in code out of low-poly balls and cones**, three submeshes (fur / ink / pink) so the whole
  cat is one renderer, and he has **no rig at all**: he hops, which is cheaper than a walk cycle and
  cuter. The burger rides small on his tray (a full one is as tall as he is) and goes back to size on the
  table. Traps: his agent needs the **chef's** navmesh (the customers' only covers the dining room), he
  samples the floor beside a plate or a table rather than heading for the thing itself (a metre above the
  floor gives an invalid path), and he steps onto the nearest walkable floor in `Awake` because his corner
  is placed by eye.
- **Mochi stood still when bought in the shop, and nothing but the shop path shows it.** The shop reveals
  something bought by growing its group from scale zero. A `NavMeshAgent` switched on in the middle of that
  has its whole group - and so itself - collapsed onto the group's pivot, which is not walkable floor: it
  fails to find the NavMesh and **never tries again on its own**, so he stayed exactly where he appeared
  and never fetched a burger. A scene that loads with him already bought skips the reveal and works, which
  is why every test of mine that did not go through the shop passed. `Waiter.EnsureOnMesh` now waits until
  the group is full size and then puts him on the floor by his corner explicitly (`homePoint`, written by
  the builder; `Warp`, falling back to switching the agent off and on). **To test anything bought in the
  shop, buy it through the shop**: open `UIShop`, `Buy`, close it, then look.
  Two smaller things came with it: the plate may be well over a metre from the nearest floor (a plate in
  the middle of a wide table), so once he is at the end of his path and within `FarthestReach` (2.6, refined in the next bullet) that
  counts as arrived, instead of asking for the same destination forever; and he was built at cat size,
  about a third of the chef, so he is scaled 2.4x (about 1.8 high against the chef's 2.6, measured), with
  the burger on his tray sized in *world* units (`OnTray` / `hold.lossyScale`) whatever his own scale is.
- **Mochi took the burger and then never put it down** (second report). At one table the floor beside the
  spot was occupied (a customer or a chair on the last bit), so he stopped 0.47 m short of the end of his
  path and shuffled against it at 0-0.3 m/s for as long as the burger was wanted; "arrived" needed the path
  to finish (remaining <= 0.1 *and* nearly still), which never happened, and the spot itself is 1.6 m from
  where he stood. Found by tracing `job / pos / vel / remainingDistance` every 0.1 s on a real delivery,
  not by reading the code: the first delivery of the session went fine, it was a *particular* table.
  `Waiter.Arrived` is now: within `Reach` (1.5 m flat) of the spot, **or** within `FarthestReach` (2.6 m)
  with the path's end under 0.9 m away for 0.4 s. A 14 s watchdog (`GiveUpAfter`) puts a carried burger
  back on its plate and sends him home rather than ever carrying one forever. He checks every frame while
  heading somewhere (the 0.3 s look is only for finding work), keeps looking on the way home, does not
  auto-brake on errands (braking made him crawl the last metre), and the burger is tossed in a short arc
  (`Toss`, 0.22 s, resizing as it goes) between plate, tray and table instead of teleporting 1.5 m.
- **How Mochi moves** (`Waiter.Hop`, all numbers are consts in the script, deliberately not serialized: a
  value serialized into the scene keeps its old number when the default changes). Speed 3.8 (chef 3.5).
  A hop is a parabola (round top, sharp landing), squashed at the landing and stretched in the air with the
  volume kept, leaning forward with speed and rocking to alternate sides each hop. With a burger he hops
  higher (0.21 vs 0.13) and quicker (3.0/s vs 2.4/s). He finishes the hop he is in before standing, then
  sways slowly (+-3 deg, a swing every ~3.7 s; the first version, +-5.5 deg every 1.7 s, was too busy) and
  wags his tail in bursts (+-22 deg about its root), so he is never frozen, burger or not; the tray stays
  level and follows. A little jump after setting a burger down. The tail is its own small mesh
  (`Cat/Tail`, pivot at its root) so it can turn: `Tools/Chibi Cafe/9b Refit the Waiter's Body` puts a new
  body and tail on the Waiter already in the scene **without** going through "3 Place Upgrades" (which
  rewrites the whole shop catalogue and its icons). Squash at the landing is a few percent only: at 17% he
  read as shrinking while he worked; measured with `Cat` renderer bounds, he is now about 7% *taller* while
  serving than standing (the stretch) and never dips below his standing height.
- **A shrunken burger made the next bun tiny** (third report: "the first bun I put in the oven after Mochi
  serves is small"). Not the bun root: its raw *look* (`food_ingredient_bun`, a pooled child). Putting a bun
  on a plate switches its raw look off but leaves it as a child of the bun, which is itself a child of the
  burger; `DynamicFoodPool` counts any `!activeSelf` object as free, so the next bun's `SetStarterVersion`
  took that look, and `parent = null` keeps world size: from the burger on Mochi's tray (0.55 of full size)
  it came out 0.55 for good. Reproduced exactly (oven bun's look `scl=(0.55,0.55,0.55)`, normal 1) by
  spawning a bun while he carried a burger that had a baked bun in it. The chef never showed it because the
  burger in his hand is at full size. Fix in the pool, nothing else touched: a free object comes out at its
  prefab's scale (`free.localScale = poolObject.transform.localScale`), whatever it was inside. Normal
  runs are unchanged (everything already equals its prefab scale there). Anything that ever shrinks a
  burger or its parent (a pop-in, a tray) needs this: world-preserving reparenting leaks the size.
- **An invisible bun in the oven, only its timer** (fourth report, "on the third burger in a row"). Old pool
  hazard, nothing to do with Mochi: `DynamicFoodPool` hands out any look that is switched off, and a food
  waiting in the pool has *its own* look switched off. When a bun goes onto a plate it asks the pool for a
  bottom-bun look and gets one off a pooled bun; when that pooled bun is used again, `EdibleBase.OnEnable`
  only checked `!currentVersion.activeInHierarchy`, which is false (the look is active, under another
  burger), so it never asked for a new look and came out with none: `looks` showed the active root with
  `ownedHere=False, looksUnderIt=[]`. It needs a bun waiting in the oven *while the previous burger is
  eaten* (so six roots are pooled when the bun reaches the plate), which is why a test that cooks one
  burger after another never showed it. Fix: `OnEnable` also treats a look that is not under the food any
  more as lost (`currentVersion.transform.parent != transform`) and gets a new one; the pool is untouched
  and the theft still happens, it just no longer matters. Also quietened a `MissingReferenceException`
  at the end of a play session (`EdibleBase.OnDisable` on a look that had been destroyed first): the same
  stolen look. **Test for it** by: six raw buns on a plate (every layer a bun root) -> a bun into the oven
  -> wait until the burger is eaten -> oven bun onto a second plate -> `looks bun` must not show an
  inactive root with `ownedHere=False` that stays that way once it is used (the root is *healed on use*),
  then ask for buns until every pooled root has been through the oven.
- **There is no clock on the player during the lesson.** `Tutorial.Running` freezes customer patience
  (`NpcFsm.Wait`: nobody leaves, throws a tomato or rushes him) and stops a baked bun or fried patty from
  burning (`Oven`, `Pan`: held at Good); both resume the moment it ends. Checked: a baked bun stayed Good
  for 48 s, a waiting customer's `WaitedSeconds` stayed 0 for over a minute, and 20 s after `Stop` the bun
  had burnt and the customer had waited 21 s. Side effect worth knowing: a first serve always counts as
  instant, so the first order rates high. The optional snack beat no longer skips itself after a timer
  (that is a clock); after 25 s of game time the card offers "tap to skip" (`UICoachCard.OfferSkip`) and the
  player decides. Its copy no longer says "Customers don't wait forever!", which is untrue while patience is
  frozen: "Customers love a snack!".
- **The terrace**: two more tables with their chairs and service spots, two rugs, plants and a lamp, laid
  out in the half of the dining floor that was always empty (DINING, $180, level 5, +2 customers). Nothing
  is laid outside the baked walkable mesh - **the navmesh is never rebaked** (see above).
- **The first shift is taught** (`Tutorial.cs`, `UICoachCard`, step 10). Eight beats, Nintendo-style: one
  idea at a time, learned by doing rather than reading, never modal, and the game is never taken away. Each
  beat waits for the player to actually act (hold a bun, fill the oven, stack a plate, get an order rated);
  only the hello and the goodbye are a tap. While it runs, the hint pulse is **restricted to the one thing
  the lesson is about** (`Tutorial.Focus`, honoured by `HighlightController`) and a yellow arrow bobs over
  it. Runs once (`CafeProgress.Taught`); "Show me again" on the Help screen starts it over.
- The shop shelves **scroll** now: the catalogue outgrew the three rows that fit on the card.
- **Mochi teaches the toss too** (beat 8, right after the first serve): "Customers don't wait forever! Grab
  something ready to toss them." The rule it teaches is the real one (`NpcFsm.CatchThrown`): a customer
  who is *waiting* and has had nothing thrown at them yet takes anything ready (cooked, baked, sliced) as a
  snack, which buys time and tips; raw food bonks them and they throw a tomato back, and the second thing
  thrown at anyone is just "I want a BURGER!". So the arrow only ever leads to food that is ready, and to a
  customer who `CanTakeSnack` (waiting, nothing caught yet). The line follows the player: nothing in hand
  → "Grab something ready", holding a snack → "Now toss it!", holding raw → "Cook it first", holding burnt
  → "Bin it". A burnt thing blocks the oven and the pan, so with empty hands it is pointed at to be taken
  out. The beat completes on `FoodFight.OnSnackAccepted` and **gives up on its own after 90 s** (45 was
  too short: baking a bun takes longer than that for a first-timer), so an optional trick can never trap
  the lesson. Tested by really playing it: bun → oven → take → toss → finale.
- **The lesson now covers every station and walks the burger ingredient by ingredient.** It used to point
  at "the buns" over and over while the burger was half empty, and it never showed the chopping board at
  all. Now: bun → oven, patty → pan, then **tomato → chopping board** (a whole tomato is a flaw, "A WHOLE
  tomato?!"), then one "Stack six layers" beat whose arrow follows `WorkOutStack`: something in his hands →
  where it goes next (burnt → bin, ready → the plate, a raw bun → oven, raw patty → pan, whole veg →
  board); hands empty → whatever has finished (oven, pan, board) or something burnt blocking a station;
  otherwise the **next ingredient not already on a plate or cooking** (bun, patty, tomato, lettuce,
  cheese, onion - skipping any whose station is busy), saying "Next up: <b>X</b>! One of each is the
  classic - any six layers work", because `BurgerReview` only cares what is on the plate, not the order,
  and the same thing twice is "Double Trouble". The 'taken' list comes from `Plate.LayerNames` plus what is
  in the oven, pan and board. It completes when a plate holds a finished burger. Tested by a script that
  does exactly what the arrow says (`follow.sh`): the whole lesson, including a burnt bun (my script was
  slow) and the bin step, reached the serve step and got a "Chef's Classic" five stars.
- **Three beats work out their target from what he holds and what the stations are doing**
  (`WorkOutStack`, `WorkOutServe`, `WorkOutSnack`, each cached per frame because the arrow and the card ask
  every frame). Two bugs came from beats that just pointed at a *category*:
  - *Serve*: with the burger finished on the plate, the arrow jumped straight to a customer, but a tap on a
    customer with empty hands does nothing. It points at **the burger on the plate** first ("A burger!
    Pick it up from the plate"), and only once it is in his hands at the customer ("Now carry it to a
    hungry customer"). Checked with three customers already waiting: the arrow stayed on the burger.
  - *Snack*: with a bun already baking and his hands empty, the arrow kept sending him to the buns for a
    second one, and once he had it, to an oven that was already full. Now, hands empty: something **ready**
    → take it; something **burnt** → clear it; something **on its way** (baking, frying, being chopped) →
    point at *that station* and say to take it out when it is ready; nothing under way → **lettuce**
    (Prep Good straight from the crate, "Salad? Fine.", the quickest snack there is). Holding something
    raw whose station is **busy** → the **bin** ("The oven is busy - bin this one for now"): with his hands
    full he cannot take the finished one out either, so pointing at the occupied station was the confusing
    part. The same `StationStep` serves the stack beat.
- **The arrow is drawn on top of everything** (`Chibi/Marker`, `ZTest Always`) with a dark outline copy
  (queue 3999, the yellow one 4000), and it is bigger (radius 0.85, 1.2 deep). A normal lit arrow hung in
  the kitchen like any other object and the range hood above the stove hid it completely when it pointed
  at the pan. It still keeps its 12 cm tip gap, so nothing is pointed *through*.
- **The arrow's TIP is what has to clear the object, not its origin.** The arrow mesh hangs from its origin
  (the top of the arrowhead, tip 0.62 below it) and the first version put the *origin* at "top + 0.32",
  so at the top of its bounce the tip was still 8 cm inside whatever it pointed at and at the bottom 30 cm
  inside: buns, pans and customers, all of them. It is now `top + tipDepth + arrowGap + bob` (tipDepth read
  from the mesh bounds), so the tip is never closer than `arrowGap` (12 cm). A customer's top also adds
  `aboveCustomer`, because the order bubble floats over their head and the arrow sits above that, and a
  customer is re-measured every frame (they walk to their chair) while everything else is measured once.
  Measured against each target's real renderer bounds: bun crate +13 cm clear, patty crate +13, pan +14,
  oven +23.
- Three things the first version got wrong, all fixed: the arrow hung a **fixed height above each
  object's origin**, and origins sit anywhere (an oven's on the floor, a plate's up on a counter), so it
  was inside some things and floating over others - it goes above the **top of what you can see** now
  (renderers plus any `TapProxy` that is the tap target for it, with a sanity cap for statically batched
  renderers that report the whole kitchen). The stacking beat **pointed at a plate while the thing to do
  was take the bun out of the oven** - it now points at whatever the next tap really is (something that
  has finished cooking → the plate the held food belongs on → more buns), and runs until the burger is
  finished rather than until a plate has one layer. And `arrowAbove` is written by the builder, because a
  value already serialised on the scene object ignores a changed default in the script.
- Help card layout: "Show me again" sits **beside "Back to kitchen"** as the second button of a normal
  footer row, in the secondary (pink) style so the teal Back stays the one primary button. Two earlier
  placements were rejected: centred along the bottom (it landed on the tip) and up on the title's line
  (it read as bolted on). The tip was shortened to "Pink pulse = use it next." to make room, and the
  snack hint it used to carry moved into the "Stack the burger" step.

### Moving the chef by hand, and a game that fits any frame

- **WASD / arrow keys on a computer, an invisible joystick on a phone** (`ChefStick` on the Player,
  `PlayerFSM.Steer`). Tapping still works exactly as before; steering forgets what the last tap was
  sending him to (nothing is used when he stops) and is camera-relative (up = up the screen). On a touch
  screen a finger down anywhere on the kitchen + drag is the stick (dead zone 2.5 % of the screen height,
  full speed at 10 %, the centre follows the finger past that); a touch that never dragged and lasted
  under 0.45 s is a tap, sent **on release** (`PlayerFSM.MovePlayer` ignores the browser's made-up mouse
  clicks while `ChefStick.TouchDriven`). Touches that start on a screen-space HUD button belong to the
  button. No steering before the title screen's Start (`EventManager.OnLevelStart`), while stunned,
  mid-interaction or with the game paused.
- **Input System package** (1.20.1) with *Active Input Handling = Both*: the old `Input.` calls in the
  game still work, `ChefStick` reads the new system (`#if ENABLE_INPUT_SYSTEM`, with an old-input
  fallback). Switching the handler needs an **editor restart** before the editor gets any device input
  (`EditorApplication.OpenProject` on the same project did nothing; the internal
  `EditorApplication.RequestCloseAndRelaunchWithCurrentArguments` restarted it). Set the handler *before*
  installing the package and the package's blocking "enable backends?" dialog never appears. The import
  errors about `...UITKAssetEditor/PackageResources/*.uxml` are the 260-character Windows path limit
  (this project's path is long); they only affect the input actions editor window.
- **The game keeps its 16:10 shape on every screen** (user: "it should open at every size, but its aspect
  ratio must not change"). The WebGL template sizes the canvas to the largest 16:10 box that fits the
  window/iframe, centred, with the page's dark background around it, and follows resizes; no footer, no
  fullscreen button. A first version filled any shape (a `ScreenFit` component widening/narrowing the camera
  and HUD) and was rejected and removed: the framing is designed for 960 x 600.
- **Hit while running, he stops running too**: `PlayerFSM.Splat` (tomato or arrow with empty hands) used to stop
  the agent but leave the run animation playing on the spot; it now sends the animator to idle and back to
  the run when he recovers (if he is still on his way).
- **Customers must be able to reach a chair** (a customer sat on the floor at the terrace): the terrace's two
  tables were on the kitchen's pink floor, where the customers' navmesh doesn't go (customers walk only on the
  teal dining floor: the bottom strip z 8-15 and the right column x 13-22). It is now laid out there
  (`CafeGrowthBuilder.TerraceRound/TerracePair`, decoration constants next to them; "3b Move the Terrace" moves
  the one in the scene without rebuilding the catalogue), and its lamp no longer stands in front of the plates.
  Check any new seating with every chair's sit spot against the customers' navmesh (NavMesh.SamplePosition with
  an NpcFsm agent's type), not the chef's.
- Mochi turns to face the camera while he waits in his corner (`Waiter.FaceTheViewer`): from behind he
  was a white blob. His landing squash stays at the original 13 % / 17 % (carrying): the gentler one
  tried for a round looked stiff (user).

### The trailer (October 2026)

`C:\Users\Beyzanur\Desktop\ChibiTrailer\`: `chibi-burger-cafe-trailer.mp4` (69.5 s, 1920x1080, 30 fps), the plan
(`trailer-plan.md`: what exists in the game, shot list, storyboard, camera/edit/sound plan), the edit decision
list (`cut.txt`) and every recorded shot with its own sound in `clips\`. Second version, to the user's brief:
cosy -> satisfying burgers -> Mochi -> personalities -> food fight -> arrow -> montage -> calm final joke ->
title; no captions, only the title card ("CHIBI BURGER CAFE" logo + "Play free on beyzosh.com").
How it was made (tools were temporary and deleted; rebuild them if needed):
- Shots staged through the game's own systems from the bridge: which customer comes in and where they sit
  (the spawner's pooled customer, `Bring`), a waiting customer's patience run out (`Impatient`: "Too slow!" and
  the throw, an arrow from a Ranger), endless patience for the others, the shift's own arrivals stopped and its
  end disabled while recording, the chef sent somewhere or turned to face the camera, Mochi hidden for plate
  close-ups, hints off, HUD hidden, the game's music muted (music is laid in the edit).
- Recorder: Game view 1920x1080 as JPEGs + the game's sound as WAV, 30 fps of game time; slow motion by
  `Time.timeScale` during a take. **The editor's mute also silences the recording**, so takes were recorded with
  the listener at 2% (barely audible) and boosted x50 in the edit; the WAV is 32-bit float, nothing lost.
- Cut with `MediaEncoder`: crossfades, freeze frames, the shots' own sound, the music pack laid per section
  (Loop-1 114 bpm cosy, Transition-1, Loop-2 128 bpm fight, Loop-3 142 bpm montage, Loop-1 soft for the final,
  Stinger-2 on the title), music ducked before the arrow and silent after the last tomato.
- Things that bit: a customer's sit-down line is raised above the order bubble (frame with room above);
  the chef faces where he last walked (turn him to the camera for face shots); bash needs quotes around
  `chair_A%20(5)`; `get` on a path through a `(Clone)` child failed, so the food in a station is read with a
  dedicated query; a play session full of endlessly patient customers has no free chairs.
- The chef's faces that exist: smile (default), grin (good rating), frown (customer protest), shock (hit).
### Play-testing it like a player, and what that fixed (October 2026)

Asked: "Mochi's burger should wobble like the chef's; a Ranger can use a bow; play the game a lot as a player
and as a designer, find what keeps it from being fully professional and fix it."

- **Mochi's burger wobbles** like the one in the chef's hands (`Waiter.Wobble`, called at the end of `Hop`):
  a spring (stiffness 150, damping 8) on the tray's tilt driven by his acceleration, the hop's air time and
  rock, and a nod while he stands, scaled by the burger's `Review.Wobbliness` (messier = wobblier) and capped at
  16 deg x wobbliness. Measured on a real delivery: the tray swings between about -15 and +20 deg.
- **A Ranger draws a bow** (`FoodFight.ShootArrow` / `SpawnBow`, KayKit `bow_withString.fbx`, assigned by step 7):
  the bow pops in beside the Ranger **on the camera's side** (the chef is usually behind the customers, so
  a bow on the far side was hidden by the Ranger's own body), the arrow is nocked and pulled back for
  `DrawSeconds` (0.45), then flies; the bow is kicked and put away. The model's limbs lie along its Z with the
  arc toward +X, so it is turned with `LookRotation(Vector3.up, Vector3.right)` and sized by its renderer bounds
  to `BowHeight` (1.35, a const).
- **The kitchen makes sounds now** (none of the packs had any): picking up (`OnFoodHolded`), putting down
  (`PlayerFSM.DropObject`), the bin, the knife four times per chop (timed to the knife in `ChoppingBoardAnim`:
  0.5 / 0.83 / 1.17 / 1.5 s), a bright two-note *ding* when a bun or patty is ready, a soft hiss when it burns (see the next section),
  and a **sizzle loop** while any patty is on a pan (`GameSfx.Sizzle(pan, on)`, fades in/out, silent while
  paused or with the sound off). All synthesised by `Idle Restaurant/Tools/Audio/kitchen_sfx.py` (numpy +
  scipy, fixed seed, writes into `Graphics/Audio/Kitchen`); quiet on purpose (0.3-0.6), they happen all the time.
  Wired in step 7's cue table (`CafePolishBuilder.Sounds`, `SizzleLoop`).
- **The result card's title says how it went**: Perfect / Great / Nice / Busy / Rough shift! by the average
  rating (`UIShiftHighlights.TitleFor`); it said "Shift complete!" for 0.6 and 4.8 alike.
- **The rush streak is reachable**: `RushWindow` 30 -> 45 s. A careful player makes a Chef's Classic every
  ~35-40 s (measured), so at 30 s nobody who wasn't pipelining two burgers ever saw "RUSH x2" or its bonus.
  With 45 the same bot reaches x4 in a shift.
- **No "Exit" on the web**: there is nowhere to quit to in a browser, and the button sent players to another
  site's list of games 4.5 s after the farewell card. `QuitButton` hides its own button in WebGL builds (the
  editor and a desktop build keep it).
- **The web page is the game's, not Unity's**: title "Chibi Burger Cafe" (was "Unity Web Player | ..."),
  a favicon of the chef's face (cropped from `icon_app.png`; the whole icon is unreadable at 16-32 px), an
  apple-touch icon, and a loading screen with the app icon bobbing, the name, a teal pill progress bar and
  "Warming up the kitchen..." / "Lighting the stove..." (the Unity logo and its bars are gone from
  `TemplateData`). A failed load says so on the page instead of an `alert()`. The "Made with Unity" splash is
  off (`PlayerSettings.SplashScreen.show`, allowed on Unity 6 Personal): the title screen comes up at once.
- The first-shift lesson's serve beat no longer flicks back to "Pick it up from the plate" while the
  customer is eating (`WorkOutServe`: "Served! Let's see what they think...").
- `NpcSpawnController` sends the opening wave **once per shift** (`waveSent`). Replay auto-starts the next shift
  (`GameManager.StartLevel` when `IsGameRestarted`); a second `OnLevelStart` doubled the wave (six customers at
  once) - a player cannot cause it (Start only shows at launch), my test script did, by invoking the hidden
  Start button: **after Replay, never click Start**.
- Stray `Debug.Log`s in `Bun`, `Oven`, `Pan` (every bake and fry, in the shipped build) removed.

**How it was play-tested**: a temporary in-editor bot (`_BotPlayer`, deleted) that plays through
`PlayerFSM.HandleScreenTap` one tap at a time with a human reaction delay (0.35 s +-40 %), finishing the most
advanced plate first, putting the next bun/patty on while it waits, tossing a lettuce snack to anyone past half
their patience, and logging every order, walkout and burn. A bridge-driven script was too slow to be fair (3-5 s
per tap: its buns burnt). Numbers from a fresh save:
- Lesson: clean end to end; patience is frozen during it as designed.
- A careful Chef's-Classic player: ~16 taps and **35-40 s per burger**, 8/8 served, 0 walkouts, average
  3.9-4.1, **$73-97 and ~32 stars per shift** (perfect burgers floor at 4 stars, so only fast ones get 5).
- Progress: level 3 after shift 3, level 5 after shift 6; the whole shop (~$1,135) takes ~13-15 shifts.
- No exceptions in any shift (only the editor's own reload noise).

### Burn smoke, the chef's ring, and a second pass on two sounds (October 2026)

Asked: "add the smoke when something burns; make the player's yellow floor ring the same red as his clothes or
remove it; the chopping doesn't sound like a knife hitting a board; the burnt sound makes me jump".

- **Smoke** (`Scripts/Others/BurnSmoke.cs` on `<<<Controllers>>>/BurnSmoke`, built by step 7): the moment a bun or
  patty burns (`Bun.SetCookedBun` / `Burger.SetCooked`, next to the Burnt sound) a charcoal puff of 20 soft
  particles, then a thin wisp that follows the burnt thing for as long as it is on the stove or in the chef's
  hands, and stops once it is binned, stacked, served or the pooled food is reused. Particles go charcoal ->
  pale grey as they thin. Three things made the first versions invisible, all worth knowing for any VFX here:
  - **A URP `Particles/Unlit` material made from code draws nothing in this project** (seen twice now). The
    Hyper Casual FX materials' old `Mobile/Particles/Alpha Blended` shader does draw, so the smoke material is
    **a copy of `Circles_AB.mat`** with our own texture.
  - The pack's "circle" texture (`Circle02`) is a **thin ring**, not a blob: the smoke drew as faint rings.
    `Graphics/Sprites/FX/smoke_puff.png` is a soft cloud of a few blobs (`Tools/FX/smoke_puff.py`).
  - Started on the food, the smoke was **inside the range hood** over the pan and **inside the oven's box**.
    It starts in front of the food toward the camera (0.55 m; 1.1 m from the oven, out of its door) and drifts
    toward the camera as it rises. All dark, it read as a stain on the floor, hence the lightening.
  Test VFX with real play (burn a patty), at timeScale 1: paused particles are not drawn.
- **The ring under the chef is scarlet** (`CafeLookBuilder.Marker`, `ChefOutfit`; `PlayerMarker.mat`).
- **Chopping**: four takes (`kitchen_chop_1..4`, picked at random per knife hit): a short crisp crunch of the
  vegetable, then the board's dull knock - a 3 ms noise burst through five damped resonators (210-2600 Hz,
  +-8 % per take) plus a low thump of the counter, no ringing tail. The first version was decaying pure sines
  (310/690/1180 Hz) and sounded like a xylophone.
- **Burnt** is a soft "fsss" now: it swells in over 0.18 s and dies away over ~0.7 s, low and breathy with a
  little hiss on top, no transient, at volume 0.35 (was a 15 ms-attack "poof" with crackles at 0.6).

### Next ideas (not built yet)

- Money earned while away (the waiter makes this honest now), so there is a reason to come back.
- Orders with exact layers in the speech bubble (the 6-layer plate already supports duplicates,
  e.g. "double patty, no onion"); a Mage asking for a specific burger.
- More kitchen: an auto-chopper, a warming shelf that keeps a finished burger from going cold.
- Group customers (a party of adventurers taking the Big Table together).
- Juice: camera nudge on a 5★ serve, coin burst on rush x3.
