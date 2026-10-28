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

- **Second version of the lesson (2026-10-11).** CrazyGames showed 4.5 min average play time and the user
  suspected players quit inside the lesson because they do not read. Changed: no hello card (the first line is
  the first action) and the goodbye leaves by itself after 3 s, so nothing waits for a tap; every line is 2-4
  words with the key word bold ("Tap the **buns**!", "Into the **oven**!", "**Chop** it!"); the stack lines
  carry a progress count "(3/6)" in berry; baking and frying run at 2x while the lesson runs
  (`Tutorial.CookSpeed`, used by `Oven`, `Pan` and `CookingProgressBar`); the snack toss is **out of the
  lesson** and is now a one-time tip (`BeginSnackTip`, `CafeProgress.SnackTipShown` / `Cafe.SnackTip`) the first
  time, after the lesson, a customer who can take a snack has used half their patience: same card and arrow,
  no freezing (`Tutorial.Running` is false during it), ends on a toss or after 20 s. Each beat sends
  `tutorial_step` (step, step_name) and the tip `snack_tip` (tossed / timeout) to Google Analytics, so the
  funnel shows the exact beat where players leave. Played through by `follow.sh` (arrow-follower) to the
  finale; tip shown and ended by a real toss.

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
- **...and every seat must stay reachable from the door with everything bought** (2026-10-15, user: "three
  customers inside each other, one sitting backwards to the table, one walking through the others"): the bought
  round tables' carve holes (0.9 of the top) plus the chairs' carve boxes closed both walkways - the terrace's
  round table cut off the bottom strip at x 12-15, the Big Table (with the original stool chair) the right column
  at z 15-19. Customers sent past them stopped where their path ended, several on one spot, and sat down there
  facing wherever they were. Fix: bought tables carve at 0.72 of the top (`CafeGrowthBuilder.TableHole`, step
  "3c Fit Table Holes"; the carve still grows by the walker's radius, so nobody crosses a tabletop). Verify with a
  reachability map of the customers' agent type from the door (21.35, 1.2, 8.79), all upgrades bought, while
  playing (carving is runtime): no cut-off cells, and every seated customer within 0.1 m of their seat.
- **Store preview videos** (2026-10-15): the cafe grown (level 6, everything bought), a modest wallet (~$205-255),
  a temporary editor bot tapping through `PlayerFSM.HandleScreenTap` on game time, `GameplayRecorder.BeginDirect`
  (not the Recorder package: it stalls the editor at ~1 fps) at 2880x1620, cut with `PreviewVideoMaker` (play
  mode). Spawn gaps under ~4 s make customers walk through each other at the door: keep the crowd natural.
- **Load time** (2026-10-17, CrazyGames reported 13.6 s): the cost was not the download but Boot's async load of
  GameScene, ~9 s (WebGL has no loading thread; measured 1015d: scene at 10.9 s, gameplay 13.2 s). Boot now loads at
  `ThreadPriority.High` (back to Normal once the scene is in): build 1017 = scene 2.5 s, gameplay 3.4-3.7 s local,
  5.4 s at 20 Mbit/s. Also: music streamed from StreamingAssets/music_loop.m4a (`Audio`, the editor plays the WAV by
  path), shop pictures from StreamingAssets/shop (`ShopIcons`, step 4 writes them there; the catalogue holds no
  sprite refs), shadowmask R8 + directional lightmaps 128 on WebGL (one mixed light, channel 0), no mips on
  screen-only UI sprites, Medium managed stripping in CrazyGamesBuild (+ Assets/link.xml), ScorePanel/FarewellPanel
  removed (never shown). Download 14.8 -> 13.2 MB: data.unity3d is LZ4 inside the .br, so asset trimming gains less
  than expected. **Measure load in a VISIBLE browser pane** (show the preview pane, cold cache: delete IndexedDB
  "UnityCache" and "/idbfs"): a hidden pane runs Unity at ~1 fps and gave false 13-14 s readings. Pane clicks are too
  short for the Input System (dropped) and drags over 0.45 s are joystick holds; UI buttons work with a short drag.
- **Whose burger it is** (2026-10-17, user: "even a perfect burger, Mochi can take it to the wrong customer"): a
  plate used to keep the customer it picked for its first layer (a bun - everybody's) whatever was stacked after.
  Now `Orders.BestFor` runs on every layer and when closing: exact order 2000 > all of it with extras 1000 > layers
  wanted x100 - extras x30, + waited x0.5, + 60 for the customer it is already for; closing an exact order may take
  the customer from a plate still being stacked. `Waiter.Table` falls back to the best `Orders.Fit`, not the first
  customer in the list.
- **The chef's hats are his own** (2026-10-17, user: "the shop's hats are the same as the customers'"): step 2
  repaints the knight helmet, wizard hat and bear hat with the chef's palette sheet (scarlet / white / gold) by
  moving their UVs between cells (`CafeGrowthBuilder.ChefColours`) - his material, no new texture. Re-render only
  the hat icons after it (step 4 re-renders all 14; the terrace's spread-out pieces frame badly, keep its old icon).
- **The first minute** (2026-10-20; CrazyGames "gameplay conversion" = players who play at least 1 minute: ours
  34% - desktop 45%, mobile 24% - top games 80%+; 66% left inside the first minute). Measured with a bot that plays
  the lesson like a careful new player (1 s per tap): before, 13 taps and 37.5 s to the first coins, $20 (first shop
  buy) at 161 s. Now (Tutorial 4th version): the first order is `Orders.FirstRecipe` (bun + patty, not on the menu)
  and `Tutorial.ReadyKitchen` puts a baked bun in the oven and a fried patty on the pan when the lesson starts
  (`IngredientsSource.SpawnInto`), so it is 6 taps: first serve 15.7 s, first coins 18.8 s (21.8 s at 1.5 s per tap);
  a cheer, then the second customer's Cheeseburger from scratch with the same arrow; $20 at 55-67 s.
- **Forgiving taps** (`PlayerFSM.NearestInteractive`): on a phone held sideways (740x360) crates, bin and plates
  are 21-31 px (fingertip ~44). A tap that misses everything interactive (and no customer near) goes to the
  nearest interactive thing within 7% of the screen height on touch, 3% with a mouse.
- **Phone held upright**: the WebGL template shows "Turn your phone sideways" (#chibi-rotate) - the game keeps
  its wide shape and would be a thin strip with ~10 px crates.
- **Lesson spotlight** (2026-10-20; first try with manga focus lines + the arrow was "too chaotic"): `UISpotlight`
  (a mesh Graphic on its own overlay canvas, sortingOrder 5 - above the HUD, which dims too; Mochi's card canvas is
  raised to 10 - its OWN nested canvas with overrideSorting: raising rootCanvas lifted the whole HUD over the dim)
  dims everything to Ink 60% except an oval around Tutorial.Focus and glides between targets. The snack tip uses it
  too (no tap blocking: optional, the shift is live) and ends - to come back later - when no hungry customer is left
  in clear view (it pointed at nothing while the only customer sat under the card). No arrow
  any more, no lines in the lesson; a blocked tap makes the oval pulse.
- **Opening titles** (`LessonIntro`, user: "Stylized Action, like an opening title sequence with cinematic shots"):
  once a session, before the lesson's first step, ~4 s: cinematic bars, the HUD faded out, the ortho camera snapping
  close-up to close-up (chef "CHIBI BURGER CAFE!", oven "BAKE!", pan "FRY!", the first customer walking in "SERVE!")
  with whip pans (Sweep `UISpeedLines`), hits (Radial lines, flash, shake, Dutch roll, a slammed label, pop sound),
  then a pull-out "LET'S COOK!". Captions (user's pick 2026-10-21, after Permanent Marker and Fredoka-on-pills were
  rejected): Bangers (OFL, `Graphics/Font/Bangers`, static SDF of 46 glyphs built by Tools/Chibi Cafe/16
  `TitleFontBuilder`, its material has OUTLINE_ON + UNDERLAY_ON so the build keeps that variant), no label: warm
  white face (LET'S COOK yellow), ink outline 0.3, offset shadow in the shot's colour, ~21% of screen height, tilted.
  Tutorial.titleFont references it. Unscaled time; any tap skips (Tutorial.AcceptsTap); the camera,
  HUD alpha and size are restored exactly. Measured with it: first coins ~22 s, $20 at ~59 s. (Recording it with
  GameplayRecorder is misleading: capture locks game time, the titles run on real time.) `Tutorial.AcceptsTap` (first thing in
  PlayerFSM.HandleScreenTap): during the lesson a tap only counts within 6% of screen height of the focus and is aimed
  at it; elsewhere it does nothing but Punch the lines. The burger can be closed before its customer sits (they walk
  in), the lesson's customers sit nearest the door and the first comes at once: first coins 19.4 s (1 s/tap).
- **One loading screen** (2026-10-20): the template's bar stays up (download = 0-85%, then creeping) until Boot
  calls `ChibiGameReady` (jslib) when GameScene is loaded (20 s safety); Boot draws no "Loading..." of its own on the
  web. CrazyGames shows its own screen before ours (page/ads): ours is the build downloading and starting - removing
  it would leave a blank screen for the same time.
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

### Store covers (October 2026)

`Desktop\ChibiCovers\`: landscape 1920x1080, portrait 800x1200, square 800x800. Asked: cover images with the game's
title as the only text, the characters and the chef true to the game (a reference picture was an AI render with
invented characters). Built from the game's own models, not painted:
- `Assets/Editor/CoverRenderer.cs`: `RenderCafe` (the cafe as the game camera sees it, in play mode: shop fully
  bought through the shop UI, customers sitting, the chef holding a real burger; world-space bubbles hidden)
  and `RenderFood` (one ingredient, posed). Both are matted from a black and a white render like the key art
  (`KeyArtRenderer.RenderMatted`, which now calibrates against the frame's corners: with post-processing on,
  white minus black is not exactly 1 and left a grey haze over the frame).
- `Tools/Covers/compose_covers.py <parts> <out>`: pink gradient + light sunburst + streaks, the cafe floating on a
  soft shadow, ingredients with drop shadows, the title logo (`GameTitle.png`); the portrait puts the chef with his
  Chaos Burger (`keyart_rush.png`) in front. Supersampled 2x. Re-run it after any change to the cafe or the chef.
- **Second version (2026-10-11), the current covers.** The first (cafe diorama on soft pink) got 1.1% click-through
  on CrazyGames (bottom 20%; top 20% is ~4%): beside the Cooking/Simulation thumbnails (Papa's etc.: saturated warm
  colours, a big face, a huge title) it was pale and nothing read at tile size. Now: a big hero in front of a yellow
  comic starburst on a saturated teal background (no neighbour uses teal), the game's pink/white checker floor, the
  logo as large as fits with a navy outline (GameTitle.png is only 512 px: upscaled 4x, alpha re-sharpened), a few
  big ingredients; the landscape keeps the cafe diorama under the title ("restaurant game" at a glance).
  The hero is `Assets/Editor/HeroArtRenderer.cs` (`Render(path, clip, time, yaw, size)` / `Sheet(dir, size)` via
  scall): the chef facing the player, grin mouth, fist up (`Spellcast_Raise` 0.5, yaw 200), a tall proper burger
  (two patties, cheese, lettuce, tomato) 1.55x life size on his head and a whole tomato bouncing off. Output
  `ChibiCovers/parts/hero.png` (and `hero_balance.png`, arms out). The first covers and videos are in
  `ChibiCovers/v1/`; the preview videos were re-cut to open on the new covers.
- Staging the cafe needs the real thing running: a person-speed bot (tap by tap) built two burgers while customers
  seated; a bridge-driven script was far too slow (customers walked out). Patience was held at zero while staging.
  The save was restored afterwards.

### CrazyGames (October 2026)

Asked: publish on CrazyGames, do what docs.crazygames.com requires, earn from ads. Requirements read and met:
- **CrazyGames Unity SDK 5.33** in `Assets/CrazySDK` + `Assets/Plugins/crazySDK.jslib` (downloaded from
  sdk.crazygames.com, demo scenes left out). Its sitelock freezes the game on unknown domains: `beyzosh.com`,
  `www.beyzosh.com`, `games.beyzosh.com` are whitelisted in `CrazySDK/Resources/CrazyGamesSettings` (re-importing
  the SDK resets that list - add them back). On a whitelisted domain `CrazySDK.IsAvailable` is false.
- **`GamePlatform`** (Scripts/Managers) is the only code that talks to the SDK: saves (Data module there, PlayerPrefs
  elsewhere - same keys, and in the editor the Data module *is* PlayerPrefs), gameplay start/stop, happy time, ads.
  **`Scenes/Boot.unity` is first in the build**: `Boot` starts the SDK and only then loads GameScene, because the
  cafe's save must not be read before the Data module is ready (gives up after 6 s and plays on local saves).
  `PlatformSession` (DontDestroyOnLoad) reports gameplay start/stop from one honest signal: a shift running with
  time not paused (shop, help, result card and ads all pause it); a new cafe level is `HappyTime`.
- **Ads**, per the ad rules: a **midgame** video on "Play again" (`RestartButton`; the card is blocked while it is up),
  at most one per 8 minutes of play (`GamePlatform.MidgameGap`; never in the first shift) - CrazyGames' advice for
  idle/tycoon games is to use forced ads "very sparingly". A **rewarded** "Double it" button on the result card
  (`UIRewardedBonus`, built by step 11, video icon `icon_video`): pays the shift's earnings again only on a finished
  ad; then rests 6 minutes; "No video right now" / "Ad blocker on" on failure; hidden for the session if ads are off
  (Basic Launch); a player who watched it gets no midgame on the same transition. No banners.
- **Build**: Tools/Chibi Cafe/Build for CrazyGames (`CrazyGamesBuild`): 16:9 canvas (CrazyGames' frame) instead of
  the beyzosh 16:10, Brotli, no fallback, clean, release; everything restored afterwards. Upload every file in the
  output folder. The WebGL template disables text selection / long-press (CrazyGames mobile rule).
- **Covers and preview videos**: `Desktop\ChibiCovers`. Videos (16.7 s, silent, opening on the cover) are cut from
  the trailer shots by `Assets/Editor/PreviewVideoMaker.cs` (play mode; VideoPlayer decode + MediaEncoder, no ffmpeg
  on this machine) from `ChibiCoversideo\cut_*.txt`.
- **Size** (CrazyGames: first download <= 50 MB, <= 20 MB to be on the mobile home page): 24.2 -> **16.3 MB**
  (data 10.5 + wasm 6.3 + loader/framework 0.1; 11 files, 17.05 MB with the template's icons) by
  `Tools/Chibi Cafe/12 Shrink Textures for Web` (`WebSizeTrimmer`, re-runnable) and the build menu's release
  recipe. In order of what it saved: lightmap `light.exr` 1024->512 (HDR, nearly incompressible: the heaviest
  thing in the build), directional lightmaps 256, shadowmasks 512, 13 KayKit colour palettes 1024->256 (swatch
  grids), neon-sign metallic map 2048->64, particle soft circle 1000->128, FBX mesh compression Medium, the
  farewell panel's two pictures 32 px in WebGL (never shown on the web: Exit is hidden), Unity's splash logo
  dropped; and CrazyGames' own release settings (IL2CPP OptimizeSize + wasm "Disk Size with LTO": wasm 8.1 -> 6.3 MB).
  Checked by screenshot diff against the original (mean difference 2.4/255, all of it animation). **Not done:** URP
  carries ten film-grain textures (2.5 MB) that no volume uses; emptying the list in `UniversalRenderPipelineGlobalSettings`
  through a SerializedObject did not persist, and saving that asset rewrites `m_RuntimeSettings` - left alone.
  Gotcha found on the way: an NPOT texture (e.g. 455x1024 key art, 1000x1000) is stored uncompressed RGBA32 whatever
  the importer says - `keyart_rush.png` (1.8 MB) and the sprites like it are the next candidates (pad to POT).
  The LTO build takes ~35 min; a clean build is required (never an incremental one).
- At submission: tick **Progress Save** (the Data module is off otherwise), orientation landscape, Unity build.
- Wallet amount now shrinks to fit (it ran under the cart button past $999).

#### Google Analytics (2026-10-11, CrazyGames build only)

GA4 property "Chibi Burger Cafe - CrazyGames", measurement id `G-F01FFEBQRP` (`Scripts/Managers/GoogleAnalytics.cs`),
browser side `Plugins/WebGL/ChibiAnalytics.jslib` (loads gtag.js from the game, so no template change). Started by
`Boot` only when `GamePlatform.IsCrazyGames`; off on beyzosh, in the editor and on localhost (`?ga_test=1` turns it on
locally in debug mode -> GA DebugView). Ad features off (ad_* consent denied, no Google signals); EEA/UK/CH get
analytics_storage denied (no cookies, cookieless pings) since the game has no consent banner; cookie
`SameSite=None;Secure` for the iframe. Events (GA4 recommended names): `game_ready`, `tutorial_begin` /
`tutorial_complete`, `level_start` / `level_end` (one shift; stars, earned, perfect, chaos, walkouts, best_streak),
`level_up` (cafe level), `spend_virtual_currency` (shop), `ad_midgame` / `ad_rewarded` (outcome),
`tutorial_step` (step, step_name), `snack_tip` (outcome). Sent from
`PlatformSession`, `Tutorial`, `CafeShop`, `GamePlatform`. Editor check: `GoogleAnalytics.EditorLog = true` logs
"[GA] ..." lines. Custom parameters only show in GA reports after being registered as custom dimensions/metrics.

### The arcade-idle flow (October 2026, "hybrid" direction)

Asked (after Burger Life on CrazyGames: walk around, collect money, unlock things in the world, hire staff):
"can we make a loop like this?" Chosen with the user: **hybrid** - keep burger building (and the Chaos
Burger joke), wrap it in the arcade-idle meta, in phases:

1. **Shifts flow on (done).** `ShiftFlow` (added to the GameManager's object at runtime by `GameManager.Awake`):
   when the last customer leaves there is no result card and no pause (`GameManager` no longer pauses on
   `OnLevelFinish`, `GamePanels` no longer shows the ScorePanel). The toast line shows "Great shift! 4.2 stars
   +$58" (`UIToast.ShiftSummary`), the empty cafe is the break where `GamePlatform.Midgame` may play (max 1 per
   8 min), then `ScoreManager.BeginNextShift` + `NpcSpawnController.ResetForNextShift` + `OnLevelStart` again
   (pips rebuild, objective toast, GA level_start). The shift is still the unit underneath (stats, GA, a bought
   table adds its customer next shift). Hints stay on between shifts. The old card and "Play again" are unused.
   Why: avg session was 4.5 min = about one shift; the result card was the natural place to quit.
2. **Money on the table (done).** `MoneyPiles` (<<<Controllers>>>, built by step 12 `CafeFlowBuilder`): a paid
   order drops a stack of **banknotes** (one per $2 + 1, max 10; stacked a little untidily, a yellow paper band
   round it from 4 notes, the last note slid off beside it; slowly turning), a white glint flashing every 0.8-1.6 s
   (`sparkle.png` on a **SpriteRenderer** - a legacy particle material on a mesh, even with vertex colours, drew
   nothing), and the amount floating over it ("$12": TextMeshPro 3D, `MoneyLabel.mat` with an ink outline, size 6,
   billboarded). Notes: a box mesh built in code, `BillFace.mat` (`bill_face.png`, mid green with a "$" in an
   oval, `Tools/FX/money_art.py`), `BillEdge.mat`, `BillBand.mat` (Yellow), paper-matte, double-sided, 0.72 x
   0.36 m. History: a plain yellow disc stack read as a cheese wheel; gold coins with a stamped "$" were "not
   liked"; the user asked for paper money.
   The pile sits on its table service spot; the chef scoops it by walking within 2.1 m, or it flies home after 20 s; opening the
   shop sweeps all piles first. The money is **saved at rating time** (nothing lost on quit); only the HUD
   wallet waits (`EarningTextController` shows Coins - `MoneyPiles.Pending`).
3. **"Double it" on the HUD (done).** Step 12 copies the result card's BonusBTN under the wallet
   (`UIRewardedBonus.offerSeconds` = 25: offered for 25 s after each shift). Tested from Boot in the editor:
   midgame at the break, rewarded paid the shift again.
- **Table spots stay square (2026-10-12).** The white squares on the tables (`TableService` spots, one per chair)
   were hidden, then made round discs, and finally put back exactly as committed by the user's choice: leave
   them alone. Lessons: they are prefab instances whose scene overrides include `m_StaticEditorFlags`
   (2147483647; the upgrade tables' copies are 0 by design) - reverting a property override drops the scene's
   own value, so restore by value, not by revert; and any non-cube mesh on them reads the baked lightmap wrongly
   (dark patches) because the lighting was baked for the cube. Kept from those tries: a burger tapped onto an
   empty spot goes to the spot of the customer waiting at that table (`PlayerFSM.PreferWaitingCustomer`).
4. Next: unlock pads in the world instead of the shop menu for furniture/stations, dirty tables, station levels.

### Faster chef, using things by walking up to them, an oven door (2026-10-12)

Asked: "the player can move faster; besides tapping, walking up to an ingredient or object can use it; the bun
oven could open and close."
- **Speed**: `PlayerFSM.runSpeed` 4.6 (the scene's agent had 3.5) and `runAcceleration` 40, the coffee's +15% on
  top. `PlayerAnimationController` plays the run cycle faster with him (speed / 3.5, max 1.45) so feet don't slide.
- **Walk-up use** (`PlayerFSM.UseWhatHeWalksUpTo`), only while **steering by hand** (WASD / stick); taps unchanged
  and a tap's walk never second-guessed. Empty hands: crates, a baked bun / fried or burnt patty / chopped veg,
  a finished burger. Holding: raw bun -> free oven, raw patty -> free pan, whole veg/cheese -> free board,
  prepared (Good) food -> plate, burger -> the spot of a customer waiting for one. Never the bin; raw or burnt
  food onto a plate stays a tap. Rules, each from a failed play test:
  - he must be *steering into* it (stick heading within ~50 deg of it); walking along the counter past the crates
    uses nothing, and standing still uses nothing (a version that used what he faced after stopping grabbed crates
    he had only stopped near);
  - among several, the one he is aimed at wins over a nearer one (cost = distance + 4 x (1 - aim)); with 8 keyboard
    directions on an isometric camera the key combo decides which neighbouring crate he walks into;
  - reach = how close the walkable floor around it gets (ring probes with complete paths, like FindApproach) + 0.35;
    the navmesh point nearest a crate's middle can be behind the counter (the patty crate was unreachable);
  - after a use, that same *use* waits until he steps away (one bun per push, not one a frame), but the next
    different use of the same thing is fine (veg put on the board is taken off once chopped without walking away);
    blocking everything near him broke "tomato crate -> board", which are neighbours.
  Tested by an in-editor bot driving simulated keys along navmesh paths (bridge calls are far too slow for a chef at
  4.6 m/s): crates, oven, pan, board and back, plate, serving a waiting customer by walking up, bin ignored. Keyboard
  input only reaches play mode with the editor focused unless InputSystem.settings.backgroundBehavior is IgnoreFocus
  (set and restored around tests; the settings object is in memory, not an asset).
- **Oven door** (`OvenDoor` on OvenStuff, set up by step 12): the model's own `oven_door` turns about its hinge
  (local X): 90 = open as modelled, **15 = closed** (at 0 it sinks inside the oven body). Open when the chef nears
  with a raw bun (2.2 m), shut while baking (the bun shows through the door's window), open when ready or burnt,
  shut once taken; a 0.35 s peek on every put-in/take-out. The door was taken out of static batching (only that
  part) so it can move.

### Second pass on walk-up use, the oven tray, the Ranger's bow, Mochi (2026-10-12)

User report after playing: "unprepared food (whole cheese, burnt bun) can't go on plates; I want to throw anything
in the bin; I never managed to get the patties; the oven's tray isn't animated; I never see the Ranger's bow and the
arrow lands above his head; when a customer leaves while Mochi is serving, the burger stays on the table."
- **Walk-up use now includes** anything onto a plate (any prep: mess is part of the game) and the **bin** (any food,
  a burger too); a burger can also be set down on an empty plate. Steering *into* it keeps both deliberate.
- **Ranking is "the one he stands in front of"**: cost = 0.5 x distance + 1.2 if he is off its corner (not within its
  x or z extent) + (1 - aim to its nearest side). The patty crate sits deeper than the cheese crate whose corner
  sticks out beside it; with 8 keyboard directions neither W nor W+A pointed at it and the cheese/bun crate always
  won. Tested: patty crate, patty to pan, cooked patty off the pan, patty and a whole cheese onto a plate, a tomato
  into the bin.
- **Oven tray** (`OvenDoor`, OvenStuff is the tray): opening = door swings down first, then the tray slides out;
  closing = tray (with the bun: `NonStackBase.Slot` follows it) slides in first, then the door shuts. `trayTravel`
  0.4 m (0.6 hid the bun behind the oven top). OvenStuff is out of static batching too.
- **Ranger's bow** (`FoodFight.ShootArrow`): it was there but invisible - held facing the chef it was edge-on to the
  camera, and in front of the customer it overlapped them on screen. It now faces the chef's way *on the screen*
  (the aim with the camera depth removed, limbs across it, curve towards him: `SpawnBow(at, -onScreen, limbs)`) and
  is held out beside the archer towards the chef (0.95 m along that, 0.3 towards the camera). Compared by spawning
  four orientations side by side and looking. The arrow now aims at the head bone + 0.55 (the head spans ~1.05-2.15
  above his feet; a fixed 2.1 only grazed the top).
- **Mochi** (`Waiter`): before fetching, on the way and at the moment of putting it down he checks that a customer
  still waits at that spot (`Awaited`); if not, the burger goes to another waiting customer, or back to a plate that
  can take it (`ReturnPlate`) - never onto an empty chair's spot. Tested both: redirected to another customer, and
  back on the plate when everyone had left.

### Straight into the kitchen (2026-10-12)

CrazyGames Basic Launch day 2: desktop 4/15, mobile 3/15 (need 9). Gameplay conversion 37% (desktop 54%, mobile 26%):
on CrazyGames a player only counts once `gameplayStart` fires, and that waited for the title screen's Start button.
Now `GameManager.StartLevel` raises `OnLevelStart` one frame after the scene starts (every time, not only after a
restart) and `GamePanels.InitializeWelcomePanel` hides the title poster instead of showing it: the game opens in the
kitchen, a new player straight into the lesson ("Tap the buns!"). Tested from Boot (SDK initialised, gameplayStart
reported, GA level_start + tutorial_begin) and as a returning player (no lesson, customers arriving). The Welcome
panel and StartBTN still exist in the scene, unused. Next in the agreed plan: specific orders (customers ask for a
given burger), daily goals, new content every 1-2 shifts, then D1 hooks (offline earnings, daily reward).

### Specific orders (2026-10-12)

The user got bored "after about the 5th customer: always the same thing" - every customer wanted *any six layers*.
Now every customer orders a recipe as they sit down (`Orders.Pick`, `NpcFsm.Ordered`), and burgers are judged
against it. Chosen with the user: **a plate finishes at the order's size** (3-6 layers).
- **Menu** (`Orders.Menu`, opens with the cafe level; the newest level's recipes are twice as likely):
  Lv1 Classic (bun, patty, lettuce, tomato) and Cheeseburger (bun, patty, cheese); Lv2 Onion Burger (bun, patty,
  onion, cheese); Lv3 Garden (bun, lettuce, tomato, onion - no patty) and Deluxe (5); Lv4 Double (two patties);
  Lv5 Big Stack (2 patties, 2 cheese); Lv6 The Tower (6). The lesson always orders a Classic. A level-up toast says
  "New recipe: X".
- **Plate** (`Plate.ForCustomer/Order/Target`): when the first layer goes on it claims the waiting, unclaimed customer
  whose order best matches what is there (then longest wait, and only orders at least that big); it becomes a burger at
  `Target` layers (6 with nobody to make it for); if the customer leaves it re-claims someone else. The finished
  `Hamburger.ForCustomer` steers hints (only that customer is hinted), the lesson's serve arrow and Mochi.
  `Orders.Plates/Burgers` keep the live ones (register in OnEnable/OnDisable).
- **Review** (`BurgerReview.Of(layers, order)`): wanted layers score by prep; an extra layer scores 0.3 and is a flaw
  ("I didn't order onion!", a second one of something ordered once is "Two of those?!"); a missing one scores 0 and is
  a flaw ("Where's my cheese?!", "Where's the patty?!"); flaws counted per wrong layer, capped at 6. Exactly as ordered:
  titled with the recipe ("Cheeseburger") and "Just what I ordered!". Without an order the old rules (wobble only).
  Pay: +$1 for a 5-layer order, +$2 for 6 (`ScoreManager.SizeBonus`).
- **Bubble** (`UIOrderTicket` in Npc_WS_Canvas' order bubble, step 13 `CafeOrdersBuilder`): the order drawn as a mini
  burger from flat ink-outlined layer sprites (`Tools/UI/order_layers.py` -> `Graphics/Sprites/Orders/layer_*.png`),
  bottom bun to top bun; layers already on the plate being made for them fade to 35%. The order bubble is 1.35x the
  eating bubble (scale 0.0135 x 1.35, raised 0.2). TRAP: the bubbles' local scale is 0.0135 under a 1:1 canvas -
  writing "1.35" made it 100x and a giant sesame bun filled the screen. Set absolute values from the eating bubble.
- Lesson stack step: "Make their order!" with "Next: lettuce! (3/4)" from `Orders.Missing`.
- Tested: Big Stack finished at 5 for the Mage (burnt patty -> "Charcoal Special"), a plate with patty+bun+cheese+lettuce
  claimed the Tower (best match) and stayed open at 4/6, the lesson's Classic -> 5 stars "Classic", review cases.
- Testing trap: editing/compiling scripts while in play mode reloads the domain and nulls PlayerFSM's state (NREs in
  Update) - stop play before any compile.

### Days, goals, break cards, daily treat, lighter start (2026-10-12)

Asked after the Basic Launch metrics (playtime 4.5 min, "bored after the 5th customer"): day structure and goals,
frequent novelty in the first 10 minutes, a daily reward, a shorter load.

- **Days** (`DayGoals`, added at runtime by `GameManager.Awake`): each shift is "Day N" (N = `ShiftsPlayed + 1`) with
  one goal, shown by the start toast ("Day 5   Reach a rush of x3") and under the customers card (`UIDayGoal`: "DAY 5
  GOAL", bar + count, teal check when met, cross if lost; hidden during the lesson). Days 1-6 are fixed: Perfect(2),
  Earn, Special, NoWalkouts, Rush x3, Average >= 4; later days are drawn from the day number (same day = same goal),
  never the same kind twice in a row. Targets: Perfect 2 (+1 every 4 days, max 4); Earn min(35 + 5·day, 90,
  customers × 7); Special = one of the newest unlocked recipes, serve 2 made exactly as ordered (3 after day 10) and
  `Orders.Special` gets weight 4 that day; Rush x3 (x4 after day 10). `OnLevelStart` can fire twice: the day starts once.
- **Day result** (`DayGoals.Finish`, called by ShiftFlow): day stars 1 = finished, 2 = goal met, 3 = goal met and
  average >= 4 (for the Average goal: and nobody left hungry). Met goal pays 10 + 2·day coins (max 40). A missed goal
  costs nothing. GA `day_end` (day, goal, goal_met, day_stars); 3 stars -> `HappyTime`.
- **Break cards** (`UIBreakCard` under HUD, step 15 `CafeDaysBuilder`, menu "15 Days: Goals, Break Cards and Daily
  Treat"): replace the summary toast. ShiftFlow plays, then sends the next wave (min `Breather` 3.5 s, then midgame ad):
  1. "DAY N DONE" + TitleFor title + 3 stars + rows (goal ✓/✗ +$bonus, average rating, earned today) + footer
     "12 more stars to level 3: Garden & Deluxe" (`ShiftFlow.NewsAt`).
  2. one "CAFE LEVEL N / New in the cafe!" card per level gained that day: tiles for new recipes (drawn with the order
     layer sprites via `UIOrderTicket.Stack`), the new customer kind (portrait, quirk) and shop items (max 4 tiles,
     "+n more in the shop").
  3. the daily treat when due.
  Each card ~2.6 s (+ per star/tile), a tap on the card moves on, paused while timeScale is 0, no scrim (non-modal),
  `CanvasGroup.blocksRaycasts` off while hidden so it never eats kitchen taps. Rows/tiles are clones of templates.
- **Customer portraits** `Graphics/Sprites/Customers/customer_<kind>.png`: rendered by step 15 from the Npc prefabs
  (idle pose, IconCamera/RenderMatted like the shop icons). **Banknote icon** `icon_money.png` from `Tools/UI/icon_money.py`.
- **Daily treat** (`DailyTreat`): paid on the first day finished each calendar day (`Cafe.DailyDate` yyyyMMdd,
  `Cafe.DailyStreak`); streak continues if the last claim was yesterday, else restarts at 1; rewards 20/30/40/50/70/90/150,
  week repeats. Card shows the week (today's tile yellow) and "Come back tomorrow for +$X". GA `daily_treat`. Keys are
  cleared by Reset Cafe Progress.
- **Lighter start** (step 14 + `Boot`): the game scene loads async while the SDK starts (activated once the SDK answers
  or after 6 s); title screen art and TMP's LiberationSans/EmojiOne left the build (Fredoka is TMP's default font, no
  default sprite sheet - don't use emoji/★ glyphs in TMP text). Measure the load on the next build.
- Tested in play mode: goal card, Day 6 card (missed goal, 1 star), daily treat card, a level-up during the day ->
  "CAFE LEVEL 2" card (Onion Burger stack, Knight portrait, Toque, Second Stove, "+1 more in the shop"), next day
  started as Day 3 with "Serve 2 Onion Burgers".

### Chairs, Mochi's tomato fight, the lesson's guide; money and voice taken back (2026-10-12)

User report + requests: customers walked through chairs and ended up two on one seat; the lesson pointed at a
customer hidden under the UI; Mochi's carried burger shrank; Mochi could have a voice; customers could throw
tomatoes at Mochi and he answers aggressively; the lesson should be fun as well as teach.

- **Chairs carve the navmesh** (`Chair` adds a carving `NavMeshObstacle` box round its meshes + the seat at
  runtime). The bake never had chairs in it. The customer heading to a chair opens it for themselves
  (`ISedile.Open(who, open)`), and on sitting down (`NpcFsm.Seat`) their agent is switched **off** (a seated agent was
  shoved off its seat by walkers) and the chair shuts behind them; on getting up (`NpcFsm.LeaveChair`) the chair
  opens, they wait until the navmesh is back under them, the agent comes on and they walk; the chair shuts again 1.8 m
  later. Tested: a whole shift with zero walk-throughs / off-seat / overlaps; chef and Mochi still reach the tables.
  TRAP (cost two builds): the obstacle box is a FIXED size (0.8 x 1.2 x 1.07 m, centre 0.115 forward). Built from the
  chair's MeshFilter it worked in the editor, but in a build static-batched chairs hold the scene's combined mesh, the
  box covered the whole cafe and carved the navmesh away: customers froze at the door, every frame logged
  "GetRemainingDistance can only be called on an active agent...". `NpcFsm.OnTheFloor` now re-places an agent that
  is off the navmesh and logs `[NpcFsm] ... off the navmesh` once. Always run the WebGL build locally and read the
  browser console before handing it over.
- **Lesson seats only in clear view** (`Tutorial.SeatInClearView`: on screen, not under CustomersCard / GoalCard /
  top buttons / ChefCard / Coach). `NpcSpawnController` prefers such chairs while `Tutorial.Running`; `Waiting()`
  points at a visible customer first.
- **Mochi carries the burger at full size** with a squash-and-stretch spring of its own on the tray (`Waiter.Bounce`,
  messier burgers jiggle more).
- **Mochi is silent.** A synthesised voice (kitten-like mews, a yowl, a hiss) was tried and removed: the user found
  it unnatural, grating and too loud ("if he can't mew quietly, calmly and sweetly, he shouldn't mew at all"). Don't
  add a voice back without a real, soft recording the user approves. Keep the game quiet in general: one sound per
  event, not one per projectile.
- **Tomatoes at Mochi** (`FoodFight.MochiInRange`): after the lesson, a customer's tomato goes at Mochi 30% of the
  time (60% if he is nearer than the chef). "MRRAOW!" pop over him (`FoodFight.OnMochiSplatted` -> UISplatPop), his fur
  puffs up and shakes (`Waiter.Puff`), then **three quick tomatoes back** at the thrower (`FoodFight.MochiThrowsBack`,
  one throw sound and one splat for the volley), who says "Bad kitty!" / "Not the cat!"... Costs nothing. Rangers
  still shoot the chef.
- **Lesson v3 - as short as it goes** (`Tutorial`; user: "it feels even worse than before... test it a lot, find
  bugs, logic errors, reasons players quit; make it as short as possible", and "not only obedient players"). Played
  with a test harness that does exactly what the arrow says, plus disobedient players. Found and fixed:
  - The first six steps were a fixed sequence (buns, oven, patty, pan, tomato, board): a raw bun put straight on the
    plate left "Into the oven!" pointing at an empty oven forever (dead end); same for a patty taken before the bun.
    Now the whole lesson is ONE dynamic step (`WorkOutStack`: the single next tap from where things stand) + serve +
    a 2.2 s "Your first burger! Keep them coming!" with confetti and the jingle. Order: start every missing layer whose
    station is free first, then collect what is ready (the stations work in parallel).
  - The order is `Orders.LessonRecipe` (Cheeseburger: bun, patty, cheese - shortest real burger using all three
    stations); cooking x3 during the lesson. 14 taps for an obedient player (was 18 + ~14 s of watching at the end).
  - Serve ends the moment the customer starts eating (not when they finish and pay); if the burger is binned the
    step goes back (`Beat.Lost`) instead of "Served!" forever.
  - A held item the order does not need -> "Not needed - bin it!" (a tomato used to be chopped and put on the plate,
    spoiling the burger).
  - Throwing food at customers is off during the lesson (a new player "giving the tomato to the customer" got a
    tomato in the face): `Tutorial.Say("They want a burger!")` instead. `Tutorial.Say` = a line for a moment.
  - Removed: Mochi walking about as a guide (distracting; blocked the lettuce crate; appeared then vanished), the card
    sliding side to side (it covered customers and bubbles), the end tomato joke (punishment right after success),
    "Hire me in the shop!". The day's goal toast waits until the lesson ends (`UIToast.ShowObjectiveOnStart`).
  - Chairs: NON-carving obstacles were tried and are wrong (paths still cross chairs, avoidance pushed a customer in
    circles, never seated); carving stays. Testing trap: the bridge's `tapon` on an NpcFsm taps a child collider that can
    be off screen (x = -295): tap customers at their head (`npcscreen` + `tap x y`, scratchpad taptarget.sh).
  - Scenarios that all complete now: obedient, give tomato to customer, raw bun on plate, patty/cheese first, whole
    cheese on plate, bin the burger, burger onto another plate, tap customer/plate/bin empty-handed.
- Other quit reasons found: the LAST ORDER name broke mid-word ("Cheesebu/rger") -> `UIOrderVerdict` auto-sizes; Boot
  showed a flat colour for seconds while the game scene loaded -> a quiet "Loading..." (Boot builds it at runtime).
- **Money piles v2** (`MoneyPiles`, user: "the money suddenly flying to the player looks like a bug"): stacks stand
  still (no turning), thicker notes (0.06) and taller stacks (amount/2 + 2 notes, 3-12), the first soft colours again
  (`Tools/FX/money_art.py`: paper 170,226,178, oval 214,245,214, ink 44,120,86; `BillEdge` 0.86,0.95,0.86 in step 12).
  Cashing in happens **in place**: the stack lifts a little and shrinks away while "+$6" rises over it and fades
  (0.9 s); the wallet counts it at that moment. Triggered by the chef walking past (2.1 m) or after 20 s by itself -
  never flying across the room. Rebuild with `CafeGrowthBuilder.BuildMoneyPiles` after changing the art.

### Burger freedom, a calmer day's end, felt rewards (2026-10-14)

User: "we lost the freedom to make the burger we want" (the plate closed itself at the order's size: no six-layer
burger for a cheeseburger customer, no bigger order made first); order bubbles too big (customers hidden); rewards
"given without noticing" ("Double it" not understood); day-end cards too fast to read; Help closing; Mochi's burger
"like jelly".

- **The player closes the burger.** `Plate.Target` is always 6 (closes itself only at six). Empty hands on a plate
  with layers = "done!": `PlayerFSM.CloseBurger` -> `Plate.Close()` (top bun) -> the chef picks it up a frame later
  (`PickUpNextFrame`: a burger made this frame has not run Start, so `isLastPiece`/collider are not ready) - or, when
  Mochi is bought and free (`Waiter.Free`), it waits on the plate and Mochi runs it out. Same tap count as before
  (it used to be one tap to take the auto-closed burger). A plate with layers keeps its collider on with empty
  hands (`Plate.Update`; placeables switch theirs off while hands are empty). `Plate.CoversAnOrder` (all of some
  waiting order is on it, extras allowed) pulses the plate as a hint and lets walk-up use close it.
  `Orders.BestFor` no longer requires the order to be at least as big as the stack (extras only lower the score).
  Lesson: "Done! Tap the plate!" (serve beat). Tested: 6 layers on a cheeseburger plate, closing a 2-layer burger,
  the lesson end to end, Mochi delivering a closed burger.
- **Order bubble** 0.8x the eating bubble (was 1.35x), `CafeOrdersBuilder` (step 13).
- **Day's end** (`UIBreakCard`, step 15): no timers - each card waits for its button: "Next" / "Next day" (teal,
  a copy of Help's Back button). "Double it" moved from the HUD into the day's card ("Double +$58", video icon,
  `UIRewardedBonus` with offerSeconds 0, the HUD BonusBTN is moved into `BreakCard/Buttons/DoubleBTN`); a watched ad
  fires `UIRewardedBonus.Doubled` and the "Earned today" row counts up in yellow with "x2!".
- **Felt rewards:** the wallet counts up with a punch (`EarningTextController`); a perfect burger / a rush of 3+
  pops a cheer over the customer (`NpcFsm.OnRated` -> `UICheerPop`, see below).
- **Help:** any tap that is not a button closes it; "Show me again" closes it as it starts the lesson (`HelpPanel`).
- **Mochi's burger** no longer squashes/stretches (jelly); it sways side to side more than in the chef's hands
  (`Waiter.Wobble`: rock x1.8, looser damping, bigger clamp).

### Cheers, goal done, tomorrow's treat, the bun goes under (2026-10-14)

- **Perfect / Rush are not splats** (user: the comic "SPLAT!" burst suits a tomato, not these). `UICheerPop`
  (`HUD/CheerPop`, built by `CafeDaysBuilder.BuildCheerPop`, step 15): a warm-white pill from the HUD's family with
  a deep-yellow star + "Perfect!" (a teal bolt + "Rush x3!" for a rush; both: "Perfect!  Rush x3!" with the rush
  smaller in dark teal), springs in (back-out), floats 70 px up, fades at 1.6 s, six small stars/bolts burst out of
  it. A perfect also dings (`GameSfx.Cue.Ready`). `UISplatPop` shows splats only again.
- **The day's goal says when it is met** (it sat silently in its corner): `DayGoals.GoalMet` (static event, once a
  day) -> LevelUp jingle, toast "Goal done!  +$N at closing" (check icon, teal, 3 s), the goal card's label turns
  "GOAL DONE!" in teal; GA `day_goal_met`. `DayGoals.Bonus` = min(40, 10 + 2 x day).
- **Today's play pays tomorrow** (user: a reason to come back, but stay as long as possible today): every day
  (shift) finished puts +$10 into tomorrow's treat (`DailyTreat.Save`, up to +$100 a calendar day), paid with the
  next treat (`Claim` returns the savings from an earlier day; today's own savings wait). The day's card has a row
  "Tomorrow's treat  +10 $50" (berry), so each extra day visibly grows it; the treat card's footer says "Includes
  +$X you saved by playing yesterday" or "Every day you finish today adds +$10 to tomorrow's". Keys
  `Cafe.TreatSaved` / `Cafe.TreatSavedDate` (cleared by Reset Cafe Progress).
- **The first bun is the bottom bun** whenever it is put on (`Plate.UseFood` -> `Restack`): lettuce then bun ends
  bun, lettuce. Only the first bun; a second one stacks where it is put.

### A day is a real day; shifts live inside it (2026-10-14)

User: "playing the second shift of today it says DAY 2 GOAL, as if I came back the next day - 12 or 24 hours
should pass; shifts on the same day must not look like different days." `CafeDay` (Managers/CafeDay.cs) keeps the
cafe's real day: a new day begins with the first shift after the **calendar date has changed and at least 12 hours
have passed** since the day began (a session running past midnight stays the same day; two sessions in one
afternoon are never two days). Keys `Cafe.Day`, `Cafe.DayStart` (unix seconds), `Cafe.DayShifts`, `Cafe.DayGap`.
- HUD goal card: "DAY 1 · SHIFT 2"; start toast "Day 1 · Shift 2   <goal>"; shift card kicker
  "SHIFT 2 DONE · DAY 1", "Earned this shift", the last card's button "Next shift".
- `DayGoals.Day` is still the shift's number over the whole game (it picks the goal and sizes the bonus), so every
  shift keeps a fresh goal; `CafeDayNumber`/`ShiftOfDay` are for display (`DayGoals.Label`, `DayGoals.Now`).
- The daily treat goes by cafe days (`Cafe.TreatDay`; old calendar-stamp saves are migrated): paid on the first
  shift of each cafe day; the row continues when the previous cafe day began the calendar day before
  (`CafeDay.Gap` 1). Kicker "DAILY TREAT · 2 DAYS IN A ROW". Every shift adds +$10 to tomorrow's
  (`Cafe.TreatSavedDay`). Tested: shift 2 of day 1 (no second treat, "+10 $50"), day 2 after 30 h (treat +$50
  incl. +$20 saved), a day begun 2 h ago is not over.

- **Snack tip fixes** (user report): the tip never points at food that is still cooking (a tap took the patty out
  raw, the arrow sent it back to the pan and pointed at it again): something ready, else lettuce; held unready food
  -> its station + "Then toss lettuce!". It only starts when a hungry customer is in clear view, and points at the
  hungriest one in clear view (never one under the coach or chef card) - `Tutorial.SnackableInView`.

### Double feedback, Mochi's loose layers, covers from the game itself (2026-10-14)

- **Double button:** the video icon sits at the face's left edge and the words fill the rest (it was at a fixed
  -66 px, made for "Double it", and ran into "Double +$71"; `CafeDaysBuilder`, step 15). A watched video now also
  toasts "Doubled!  +$71 to your wallet" (`UIToast.OnDoubled`) besides the card's yellow count-up. During
  CrazyGames' Basic Launch there are no ads (`adsDisabledBasicLaunch`): the button says "No videos yet" for 2.5 s and
  leaves for the session (it used to vanish without a word - that was the "nothing happened" the user saw).
- **Mochi's burger:** each layer rides on a little spring (`Waiter.Jiggle`): every landing throws the upper
  layers up a touch (higher = more) and they drop back with a small twist, and they slide towards the side the tray
  leans; the bottom layer stays, nothing falls. The taller the burger, the less the whole stack leans as one block.
  Trap: a Hamburger is scaled (1, 0.1, 1) with its layers scaled back up (1, 10, 1) - its own up and sideways
  units differ 10x, so offsets are converted (`sideGap`) and layers are never tipped about a level axis (that
  shears them); the plate under the burger is left out.
- **Store covers v4** (`Desktop/ChibiCovers/v4`): the AI-painted covers had fries, drinks, bunny/cat customers, a
  menu board, lamps and a second cook. Now a real render inside GameScene (`Assets/Editor/StoreCoverRenderer.cs`,
  `RenderAll(dir)` via bridge scall; DontSave staging, nothing saved): the chef (Cheer pose) lifting a tall burger
  on a plate beside his head, two Big Tables with Barbarian, Mage, a Regular, Rogue, Ranger, Knight and burgers,
  the kitchen behind with gaussian depth of field, a kitchen table up front with patties in the pan, tomato slices,
  cheese, ketchup and mustard; the wall's "CHIBI BURGER" letters hidden. `Tools/Covers/compose_store.py` adds the
  game's own logo (GameTitle with the ink outline) and a soft light behind it.

### The long goal: a Grill House (2026-10-14, phase 1 of 2)

User: "an ultimate goal - opening a pizzeria, a grill restaurant or an ice-cream shop with money saved over all the
days". Chosen: a **Grill House** (the KayKit pack already has the roast dinner, stew, steak, ham, potatoes, carrot
and a steak crate: no extra download; pizza and ice cream have no models), saved in **the same wallet** as the
shop (spending in the shop sets it back - that is the choice), **goal first, restaurant later**.
- `GrandGoal` (static): $2,000 (~5-6 days of ~5 shifts with the shop bought), keys `Cafe.GrillTeased` /
  `Cafe.GrillReached`. After the player's 2nd shift a card "NEXT BIG GOAL / Open a Grill House!" with the three
  dishes (rendered by step 15 into `Graphics/Sprites/Grill/`: roast dinner, hearty stew, big steak).
- `UIGrandGoal` (the user's choice after three tries): its FIRST design - dish left, "NEXT: GRILL HOUSE", "$660 /
  $2,000", a bar - 300x100 in the top-right, right edge on the 32 px screen margin (the buttons' edge), top at 132 px
  (a clear gap under the buttons). Rejected: right-aligned to the wallet (off-axis), a tab under the wallet (tiny),
  a twin of the day's goal card. HUD frame: status top-left, wallet+buttons+Grill House top-right, Last order
  bottom-left, order ticket bottom-right; the middle (kitchen, tables, tomato and burger statues) stays clear.
- **Frosted cards** (`UIFrostedCard`): the order ticket (idle 0.55) and the Grill House card (0.7) fade only their
  background, never their words; solid while the pointer is over them and for 2 s after a change (a layer on the
  plate / money coming in). Fading whole cards washed the text out.
- Each shift card has a row "Grill House  $X / $2,000" ("Ready!" when full); once full, a card "You saved $2,000!"
  (3 stars) - "Your Grill House opens soon - the money stays yours". It takes no money until phase 2 exists.
- GA: `grand_goal_teased`, `grand_goal_reached`.
- **Phase 2 (next):** the Grill House itself - a second kitchen (re-dressed from this one) where the grill cooks
  steaks (cook/burn like the patty), carrots are chopped, plates are composed (main + side), with its own orders and
  customers; switching between the two restaurants; then a third goal for later.
- Mochi's burger sway was toned down again (rock x0.5, max 8 degrees, damping 10): the user wants it gentle.

### Basic Launch data: the first reward came too late (2026-10-15)

CrazyGames Basic Launch (10/10-14/10, 454 plays, desktop 3/15, needs 9): average play 3m10s (score 2 at 6m15s),
D1 1.03% (2 at 1.30%), gameplay conversion 47.6% (2 at 55%), CTR 1.2% (bottom 20%), loading 13.6 s, rating 10/10
(13 likes, 0 dislikes). With 8 customers a shift plus the lesson, the first break card came at ~5 minutes: most
players never saw a single reward. Note: three "score 2"s make 6, still under 9.
- **Shifts ramp up**: 3, 4, 5, 6, 7, then 8 customers (`ScoreManager.CustomersForShift(ShiftsPlayed)`, plus tables).
  The first shift is the lesson's customer and two more; first card at ~2.5-3 min for a real player (3:34 with the
  slow test harness).
- **Cafe levels** lowered to match (stars 8/30/60/100/150, was 15/40/80/130/190): level 2 still at the first break.
- **Day 1 goal** is "Serve a perfect burger" (1): the lesson's burger meets it, "Goal done!" as the lesson ends
  instead of a red "missed" on the first card.
- **Download** (`WebSizeTrimmer`): Visual Scripting package removed (unused, its runtime was compiled into the wasm);
  URP's ten film-grain noise textures (2.5 MB, ~1.4 MB after Brotli - noise doesn't compress) replaced by a 4x4
  stand-in in both places URP keeps them (`StubFilmGrain` for the global settings - an EMPTY list is refilled
  from the package, a non-empty one is not - and `LeanPostProcess`, a renderer copy `PostProcessData_Web`). Result: 16.27 -> 14.93 MB
  download (data 9.68 -> 8.35 MB; the wasm did not change - stripping had already dropped Visual Scripting).
  Measured, NOT worth it or no effect: the music (WebGL audio ignores mono/quality: same size), the shop/customer
  icons (RGBA32, 4.6 MB raw but 510 KB after Brotli - DXT would be larger) and the shadowmasks (ARGB4444 is forced
  by WebGL's lightmap encoding; mostly flat, compress well). `WebSizeTrimmer.Sizes(n)` lists stored texture sizes.
  After any `git checkout` of UniversalRenderPipelineGlobalSettings.asset, re-run `WebSizeTrimmer.StubFilmGrain`.

### The order card: what goes in, and that you're free (2026-10-15)

User: perfect burgers were hard because the bubble's little burger (layer strips in a 66 px ring) can't be read,
and the bubble can't grow without hiding the customers; "the player should easily know what to use - and still
know they are free". `UIOrderCard` (`HUD/OrderCard`, step 15) is a horizontal ticket in the BOTTOM-RIGHT corner,
440 px, mirroring the "Last order" card bottom-left (what you served / what you're making). Header: customer
portrait, "ORDER", recipe name, extras in grey on the right ("+ Tomato · extra" - allowed, never red). A tile per
ingredient left to right as stacked: the ingredient rendered from the game's models (`Sprites/Ingredients/`), its
name and how it must be made ("Patty / cooked", "Tomato / sliced", "Bun / baked", "Lettuce / fresh"); faded until
it is on the plate, then a teal tick, or a red badge if on but not ready (flame raw, knife-fork whole, cross
burnt). Footer: "Exactly this = Perfect · any burger still sells" / "All there - close it for a Perfect!".
Shows the burger in the chef's hands, else the plate with most layers, else the longest-waiting customer. 80%
opaque, fully visible with the pointer over it (user's choice) and for 2 s after any change (touch). Hidden in the
lesson and during the break cards. Placement history: right-edge column covered the onion crate and took its taps;
left column covered the big tomato statue the user likes; the bottom-right corner is empty floor with every
upgrade (measured tappable screen rects). Fredoka has no star glyph - write "Perfect", not a star.

### Next ideas (not built yet)

- Money earned while away (the waiter makes this honest now) - tomorrow's treat covers part of this.
- Orders with exact layers in the speech bubble (the 6-layer plate already supports duplicates,
  e.g. "double patty, no onion"); a Mage asking for a specific burger.
- More kitchen: an auto-chopper, a warming shelf that keeps a finished burger from going cold.
- Group customers (a party of adventurers taking the Big Table together).
- Juice: camera nudge on a 5★ serve, coin burst on rush x3.
