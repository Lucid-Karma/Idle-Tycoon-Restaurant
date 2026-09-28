---
name: chibi-burger-cafe-ui
description: UI/UX design direction and the implemented UI system for Chibi Burger Cafe (the Idle Restaurant Unity project). Use before creating, restyling or reviewing any UI in this project — HUD, panels, menus, popups, buttons, icons, fonts, colors, world-space bars — or when asked to make the UI look more professional/polished.
---

# Chibi Burger Cafe --- UI/UX Design Skill

## Purpose

This skill defines the UI/UX direction for **Chibi Burger Cafe**, a cozy
3D burger-cooking/cafe game.

The goal is to make the interface feel **professionally designed,
cohesive, readable, and game-ready** while preserving the game's
existing personality: colorful, cute, playful, slightly pastel, and
approachable.

The UI should look like it was designed by a professional game UI/UX
designer---not like a collection of default Unity UI elements, generic
mobile-game buttons, or decorative "cute" assets placed around the
screen.

------------------------------------------------------------------------

## 1. Core Design Direction

Use this design equation:

> **Professional cozy game UI + playful diner/cafe personality +
> restrained cuteness**

The game is cute, but the interface must **not become excessively
sugary, childish, pink, bubbly, or princess-like**.

### Desired qualities

-   Cozy
-   Charming
-   Playful
-   Clean
-   Polished
-   Friendly
-   Legible
-   Tactile
-   Slightly whimsical
-   Modern enough to feel commercially produced

### Avoid

-   Excessive pink
-   Excessive hearts, bows, flowers, sparkles, stars, or kawaii
    decorations
-   Giant rounded rectangles everywhere
-   Random pastel colors with no hierarchy
-   Generic mobile-game UI
-   Default Unity buttons
-   Excessive outlines
-   Excessive drop shadows
-   Too many UI elements competing for attention
-   Decorative elements that reduce gameplay visibility
-   UI that looks like a children's educational game
-   UI that visually competes with the 3D kitchen

The **3D game world is the star**. UI should frame and support it.

------------------------------------------------------------------------

# 2. Reference Image Analysis

The current screenshot has a strong visual identity:

-   Isometric 3D kitchen
-   Pink and white checkerboard floor
-   Teal floor/background area
-   Pink/purple environmental palette
-   Chunky stylized food
-   Cute small player character
-   Hand-drawn/organic typography
-   Large burger and food props
-   Playful circular UI containers
-   Strong visual personality

Do **not** redesign the game's 3D art direction.

Instead, bring the UI into the same visual language while making its
hierarchy and interaction design substantially more professional.

### Important existing strengths to preserve

-   Pink + teal complementary palette
-   White UI surfaces
-   Dark navy/ink color for icons and text
-   Hand-drawn personality
-   Food-related visual motifs
-   Soft rounded shapes
-   Friendly, toy-like physicality

------------------------------------------------------------------------

# 3. UI Visual System

Create a coherent UI design system before designing individual screens.

## 3.1 Color hierarchy

Use a restrained palette.

Suggested foundation:

-   **Ink / Navy:** `#10263A`
-   **Warm White:** `#FFF9F4`
-   **Soft Pink:** `#F6AFC2`
-   **Light Pink:** `#F9D6E0`
-   **Muted Berry:** `#B96D8B`
-   **Teal:** `#239FA4`
-   **Dark Teal:** `#176C73`
-   **Warm Yellow:** `#F4C95D`
-   **Tomato Red:** `#E96A5F`
-   **Soft Cream:** `#F5E7D0`

Do not use every color simultaneously.

### Rule

Each screen should have:

1.  A neutral/base surface
2.  One primary accent
3.  One secondary accent
4.  Dark ink for important information
5.  Optional status colors

Pink should not automatically be the background of every UI element.

------------------------------------------------------------------------

# 4. Shape Language

Use a consistent shape vocabulary.

### Primary UI containers

Prefer:

-   Soft rounded rectangles
-   Organic rounded cards
-   Pill-shaped controls when appropriate
-   Circular controls only when the function naturally fits a circular
    interaction

Avoid putting every element inside a circle.

### Corner radius

Use a small number of radius levels rather than arbitrary values.

Suggested conceptual system:

-   Small: 8--12 px
-   Medium: 16--20 px
-   Large: 24--32 px
-   Circular: only for radial/icon controls

The exact pixel size should scale appropriately with the target
resolution.

------------------------------------------------------------------------

# 5. Depth and Material

The current game has a physical, toy-like 3D aesthetic.

The UI should feel like it belongs to that world.

Use:

-   Very subtle shadows
-   Soft highlights
-   Slight bevel/edge treatment
-   Layered surfaces
-   Occasional small offset/depth effects

Do NOT use:

-   Heavy black drop shadows
-   Glossy mobile-game buttons
-   Excessive gradients
-   Metallic UI
-   Strong neon effects

A useful mental model:

> UI elements should feel like small physical objects sitting on top of
> the game, not flat web controls.

------------------------------------------------------------------------

# 6. Typography

Typography is one of the biggest opportunities for making the interface
feel professional.

## Display typography

The current hand-drawn title style can remain as a **personality/display
font**.

Use it for:

-   Game title
-   Major section headings
-   Special event labels
-   Occasional playful callouts

Do not use the display font for every piece of text.

## Functional typography

Use a highly readable rounded sans-serif for:

-   Counters
-   Buttons
-   Instructions
-   Money
-   Timers
-   Orders
-   Settings
-   Item descriptions
-   System messages

The functional font should feel friendly but highly legible.

### Typography hierarchy

Every screen should clearly distinguish:

1.  Screen title
2.  Primary objective / important information
3.  Secondary information
4.  Supporting text
5.  Micro labels

Do not make everything large and bold.

------------------------------------------------------------------------

# 7. Iconography

Icons should share one visual language.

Preferred style:

-   Simple
-   Bold enough to read at gameplay scale
-   Slightly rounded
-   Minimal internal detail
-   Dark ink or a carefully selected accent
-   Consistent stroke weight

Avoid mixing:

-   Thin line icons
-   Thick cartoon icons
-   Emoji
-   Realistic icons
-   Different icon families

The current hand-drawn star/speaker/cart style can inspire the icon
language, but icons should be standardized.

------------------------------------------------------------------------

# 8. HUD Redesign Principles

The current screenshot has UI placed around the entire screen perimeter.

The redesign should reduce visual noise.

### General HUD rule

> Put information where the player naturally looks, not merely where
> there is empty screen space.

The 3D kitchen must remain visible.

### Recommended HUD structure

#### Top-left

Use for:

-   Day/level
-   Restaurant status
-   Optional small objective

Keep compact.

#### Top-center

Use only for:

-   Current objective
-   Order status
-   Important temporary information

Avoid permanently occupying the center with a large decorative title.

#### Top-right

Use for:

-   Currency
-   Settings
-   Pause
-   Other persistent system controls

Group related system information.

#### Bottom area

Use for:

-   Contextual actions
-   Inventory
-   Ingredient/tool selection
-   Interaction controls

Only show controls that are currently useful.

### Important

Do not redesign the HUD by simply replacing each existing circle with a
prettier circle.

Reconsider the **information architecture**.

------------------------------------------------------------------------

# 9. Existing UI Elements

The screenshot contains elements such as:

-   Hosted customer counter
-   Currency/money
-   Help button
-   Sound button
-   Shopping/cart button
-   Chef/character portrait
-   Bottom star/progress indicators
-   Large decorative burger
-   Other decorative food elements

Treat these as a UX system rather than independent decorations.

### Priority hierarchy

A useful hierarchy is:

**Gameplay-critical** - Customer/order state - Current objective -
Interaction/action - Money/reward when relevant

**Important** - Progress - Level/day - Inventory - Store

**Secondary** - Help - Sound - Settings

**Decorative** - Large burger - Character portrait when not conveying
information - Decorative food - Background UI ornaments

Gameplay-critical information should visually dominate decorative
information.

------------------------------------------------------------------------

# 10. Character Portraits

The chef portrait can be retained as a personality element.

However:

-   Do not make the portrait unnecessarily large.
-   Give it a clear functional purpose.
-   If it is only decorative, reduce its visual dominance.
-   If it represents dialogue, mood, tutorial, or feedback, design it as
    a proper dialogue/status component.

The portrait should not compete with the kitchen.

------------------------------------------------------------------------

# 11. Progress / Rating System

The current bottom star system should be treated as a meaningful game
mechanic, not merely decoration.

Design it as a clear feedback component.

Possible visual structure:

-   Current performance/rating
-   Progress toward next reward
-   Small contextual feedback
-   Clear filled/unfilled states

Use animation and micro-interactions to make rewards feel satisfying.

Do not make the progress bar unnecessarily huge.

------------------------------------------------------------------------

# 12. Buttons

Buttons should communicate:

-   What is clickable
-   What happens when clicked
-   Whether the action is available
-   Whether the action is currently selected

Each important button should have states:

1.  Default
2.  Hover
3.  Pressed
4.  Disabled
5.  Selected/toggled
6.  Optional attention/notification state

### Button feedback

Use subtle:

-   Scale
-   Shadow/depth change
-   Color change
-   Icon movement
-   Sound
-   Tiny bounce

Avoid aggressive animation.

------------------------------------------------------------------------

# 13. Micro-interactions

Professional polish should come from interaction quality, not decorative
clutter.

Use micro-interactions for:

-   Button press
-   Purchase
-   Customer arrival
-   Successful burger completion
-   Money earned
-   Order accepted
-   Star/rating increase
-   New item unlocked
-   Error / failed action

Animations should generally be:

-   Short
-   Responsive
-   Easy to understand
-   Consistent

A useful feel:

> Press → tiny physical compression → release → subtle bounce.

------------------------------------------------------------------------

# 14. Motion Guidelines

UI animation should feel like a physical toy.

Preferred:

-   100--250 ms interaction feedback
-   Small overshoot/bounce
-   Smooth easing
-   Gentle transitions
-   Scale changes around 0.95--1.05 rather than huge movement

Avoid:

-   Slow dramatic transitions for basic actions
-   Excessive bouncing
-   Constant idle animations
-   Every element moving simultaneously

Motion should communicate state.

------------------------------------------------------------------------

# 15. Panels and Menus

Menus should feel like part of the restaurant world.

For example:

### Shop

Use a clean panel resembling:

-   A diner menu
-   A recipe card
-   A small cafe order board
-   A collectible food catalog

Rather than a generic mobile store.

### Settings

Use a simple, calm panel.

Do not overdecorate functional menus.

### Order panel

Make orders immediately understandable:

-   Customer
-   Requested food
-   Quantity
-   Time/urgency if applicable
-   Reward
-   Current completion state

Information should be scannable in under a second.

------------------------------------------------------------------------

# 16. Information Density

The game should feel relaxed.

Do not fill the screen simply because there is available space.

Use whitespace deliberately.

### Rule

If an element does not:

-   communicate information,
-   enable interaction,
-   provide feedback,
-   establish hierarchy,

consider removing it or making it much smaller.

------------------------------------------------------------------------

# 17. Mobile / Resolution Considerations

The UI must remain usable at different aspect ratios.

Design around:

-   Safe areas
-   Touch targets
-   Camera visibility
-   Different screen widths
-   Landscape orientation if applicable

Interactive touch targets should generally be comfortably tappable.

Do not place important controls too close to screen edges or corners.

Do not let UI overlap important gameplay interactions.

------------------------------------------------------------------------

# 18. Touch UX

For mobile:

-   Buttons must be easy to tap without precision.
-   Avoid tiny icons.
-   Avoid relying on hover.
-   Use visual press feedback immediately.
-   Avoid accidental taps caused by overlapping controls.
-   Keep destructive actions separated from common actions.

Touch interaction should feel forgiving.

------------------------------------------------------------------------

# 19. UI Composition

Think in terms of visual hierarchy:

### Level 1 --- Gameplay

The kitchen, customers, ingredients, and character.

### Level 2 --- Current objective

What the player needs to do right now.

### Level 3 --- Feedback

Orders, rewards, progress, success/failure.

### Level 4 --- Navigation

Shop, settings, help, inventory.

### Level 5 --- Decoration

Small visual flourishes.

Never allow Level 5 to overpower Level 1--3.

------------------------------------------------------------------------

# 20. Design Consistency Rules

Every new UI component must answer:

-   What is its purpose?
-   What is its priority?
-   Why is it located here?
-   What visual family does it belong to?
-   What are its interaction states?
-   What happens when it is unavailable?
-   What feedback does the player receive?

Do not add UI simply because the screen looks empty.

------------------------------------------------------------------------

# 21. When Editing the Existing Project

Before changing UI:

1.  Inspect the existing UI hierarchy.
2.  Identify reusable components.
3.  Identify the current canvas/camera setup.
4.  Identify anchors and scaling.
5.  Identify existing fonts, sprites, and icons.
6.  Identify whether the UI is world-space, screen-space, or mixed.
7.  Preserve working gameplay logic.
8.  Change presentation before rewriting gameplay systems.
9.  Avoid unnecessary architectural rewrites.

Do not destroy functional systems just to make the UI prettier.

------------------------------------------------------------------------

# 22. Implementation Philosophy

When working in Unity:

Prefer reusable components such as:

-   `UIButton`
-   `UIIconButton`
-   `UIPanel`
-   `UIBadge`
-   `UIProgressBar`
-   `UIOrderCard`
-   `UICurrency`
-   `UIToast`
-   `UIRewardPopup`

Use shared:

-   Colors
-   Typography
-   Spacing
-   Corner radii
-   Shadows
-   Animation timings

Do not manually style every UI element independently.

Create a small visual design system and reuse it.

------------------------------------------------------------------------

# 23. Design Tokens

Establish centralized tokens.

Example conceptual system:

``` text
Colors
  Ink
  Cream
  White
  Pink
  LightPink
  Teal
  DarkTeal
  Yellow
  Tomato

Spacing
  XS
  S
  M
  L
  XL

Radius
  Small
  Medium
  Large

Motion
  Fast
  Normal
  Slow

Typography
  Display
  Heading
  Body
  Caption
  Numeric
```

If the project already has equivalent systems, extend them instead of
creating duplicates.

------------------------------------------------------------------------

# 24. Visual Balance

The screenshot contains strong visual elements such as the giant burger,
vegetables, chef portrait, title, and multiple circular controls.

The redesign should create **visual breathing room**.

Use:

-   Fewer large UI objects
-   More intentional grouping
-   Smaller secondary elements
-   Strong alignment
-   Consistent margins
-   Better contrast hierarchy

The goal is not to make the interface "minimal."

The goal is:

> **Visually rich but structurally simple.**

------------------------------------------------------------------------

# 25. Professional Polish Checklist

Before considering a UI screen finished, verify:

### Hierarchy

-   Can I immediately identify the main objective?
-   Is important information more prominent than decorative information?

### Consistency

-   Do buttons share the same design language?
-   Are icon sizes consistent?
-   Are corner radii consistent?
-   Are spacing values consistent?

### Readability

-   Can I read the UI while the game is moving?
-   Is text large enough?
-   Is contrast sufficient?

### Interaction

-   Does every interactive element have feedback?
-   Are disabled states obvious?
-   Are touch targets large enough?

### Composition

-   Is the gameplay area still the visual focus?
-   Are there unnecessary elements?
-   Is the screen balanced?

### Personality

-   Does it still feel like Chibi Burger Cafe?
-   Is it cute without becoming overly childish?
-   Does the UI feel connected to the game's 3D art?

### Production quality

-   Does anything look like a default Unity component?
-   Are there inconsistent icons?
-   Are there random colors?
-   Are shadows/gradients used inconsistently?

------------------------------------------------------------------------

# 26. Claude's Working Method

When asked to improve this game's UI/UX, do NOT immediately start
changing random elements.

Follow this sequence:

### Step 1 --- Audit

Analyze the current screen and existing implementation.

Identify:

-   Visual hierarchy problems
-   UX problems
-   Alignment problems
-   Redundant elements
-   Inconsistent components
-   Typography problems
-   Color problems
-   Touch problems

### Step 2 --- Define the system

Establish:

-   Color tokens
-   Typography
-   Spacing
-   Shape language
-   Icon style
-   Button states
-   Panel style
-   Animation style

### Step 3 --- Redesign the hierarchy

Decide what should remain visible during gameplay and what should move
into contextual menus.

### Step 4 --- Create reusable components

Build the visual system as reusable Unity components rather than one-off
objects.

### Step 5 --- Implement incrementally

Change one logical group at a time.

### Step 6 --- Review in-game

Evaluate the UI over the actual 3D kitchen, not only inside the Unity
editor.

### Step 7 --- Polish

Add subtle motion, feedback, depth, and spacing refinements.

------------------------------------------------------------------------

# 27. Critical Design Principle

**Do not interpret "make it more professional" as "make it more
corporate" or "make it more minimal."**

The target is:

> **A polished indie cozy game with a distinctive cafe identity.**

It should feel commercially presentable while still having charm.

Think:

-   cozy indie game
-   polished mobile game
-   cute cafe
-   toy-like physical UI
-   restrained pastel palette
-   strong usability

Not:

-   corporate SaaS
-   generic hypercasual mobile game
-   children's learning app
-   excessive kawaii UI
-   sterile minimalism

------------------------------------------------------------------------

# 28. Final Visual Target

The finished UI should make a player think:

> "This game has a real visual identity."

rather than:

> "This is a Unity project with some cute buttons."

Preserve the charm of the existing Chibi Burger Cafe art while making
the interface feel intentional, cohesive, readable, and professionally
produced.

------------------------------------------------------------------------

# 29. Project Implementation (this repository)

The sections above are the design direction. This section records how it
is already implemented here, so new UI extends the system instead of
re-inventing it. Unity project root: `Idle Restaurant/`. `[GAME]` below
means `Assets/Project/[GAME]`.

## 29.1 Tokens

`[GAME]/Scripts/UI/Theme/UITokens.cs` is the single source of truth for
colors (§3.1 palette plus derived `SecondaryEdge`, `SubtleFace/Edge`,
`StarEmpty`, `Scrim`, `Shadow`), spacing, radii, typography sizes and
motion (spring constants, press/hover/punch scales). Never hard-code a
color or timing in a component; add a token.

Per-screen accent rule as applied: warm-white surfaces, **teal** = primary
action, **yellow** = money/rating, **ink** = information, pink only as
light tint (empty pips/stars, portrait background, subtle buttons).

## 29.2 Typography

- Display: `Permanent Marker` (existing) — screen titles only ("How to
  play", "Shop", "Shift complete!") and the logo image.
- Functional: `Fredoka` (SIL OFL, `[GAME]/Graphics/Font/Fredoka/`, license
  in `OFL.txt`) as dynamic TMP assets `Fredoka-SemiBold SDF` (numbers,
  buttons, labels) and `Fredoka-Medium SDF` (body copy).
- Micro labels: SemiBold 22, uppercase, character spacing 8, `InkMuted`.
- Canvas is 1920×1080 reference, match height; the WebGL frame is 16:10
  (≈1728 ref units wide) and often only ~600 px tall, so nothing below 22
  ref px.

## 29.3 Sprites and icons

`[GAME]/Graphics/Sprites/UI/` — procedurally baked (SDF) white shapes,
tinted via `Image.color`:
`ui_rounded` (9-slice, radius 40 → set `pixelsPerUnitMultiplier = 40 / r`),
`ui_pill` (9-slice, multiplier `128 / height`), `ui_circle`, `ui_ring`,
`ui_shadow` (soft 9-slice card shadow), `ui_shadow_round`, `ui_ring_bubble`,
`ui_tail`, icons `icon_cart`, `icon_person`, `icon_star`, `icon_check`,
`icon_close`, `icon_sound_on/off`, `icon_burger`, `icon_cutlery`, `icon_flame`,
`icon_bolt` (rush), mood faces `icon_face_love` / `icon_face_laugh` /
`icon_face_shock` (filled face, features cut out, tinted by
`UITokens.MoodColor`), and poster effects `fx_sunburst` (rays fading to the
rim), `fx_speedline`, `fx_drop`, `fx_splat` (tomato splat).
Key art (the chef sprinting with the Chaos Burger on his head) is a 3D render
from the game's own models: `Graphics/Sprites/KeyArt/keyart_rush.png`
(`Assets/Editor/KeyArtRenderer.cs`, Tools/Chibi UI/Render Key Art;
transparent, trimmed). Store covers `cover_square.png` / `cover_wide.png` in
the same folder come from `PosterLayout` (Tools/Chibi UI/Render Covers).
All of them are generated by `Assets/Editor/UISpriteBaker.cs`
(menu **Tools/Chibi UI/Rebake UI Sprites**). Add new shapes/icons there
(128 px canvas, ~14 px rounded strokes) instead of importing mismatched art.

## 29.4 Reusable components (`[GAME]/Scripts/UI/Components/`)

| Component | Use |
|---|---|
| `UIPressable` | Every button. Face sinks onto a darker edge on press, spring bounce on release, hover scale, dimmed disabled state. Unscaled time (works while paused). |
| `UIPanelIntro` | On a panel's card: fade + small scale pop when the panel is shown (CanvasGroup or SetActive). |
| `UIValuePunch` | On counters (money, customers): bounce when the text changes. |
| `UIToast` | Top-center transient feedback: shift objective ("Rush hour!"), served burger's name + earnings + RUSH xN / SPEEDY (from `ScoreManager.LastOrder` on `EventManager.OnOrderRated`, mood face icon), purchase, customer left. |
| `UIOrderVerdict` | Chef card: name of the last served burger ("Chaos Burger"), tomato when the rating is poor. |
| `UICustomerReaction` | World-space speech pill over a customer (in `Npc_WS_Canvas/Reaction`): the `BurgerReview` line with a mood face while they eat, then a yellow "+$N" pill that floats up; "Too slow!" when they give up. Width fits the text. |
| `UIShiftHighlights` | Result card line under the title: "Signature burger: … · Best rush xN". |
| `UITitleMotion` | Title poster motion (unscaled): chef stride bob, streaks rushing past, sweat, slowly turning sun-burst, bobbing callout. |
| `UICustomerPips` | Shift progress pips; count from `ScoreManager.CustomersPerLevel`. |
| `UIMusicToggleIcon` | Sound button shows on/muted state. |
| `UIAffordability` | Shop buy button disabled + hint until affordable; price from `BuyButton.Price`. |
| `UIWorldBubble` | World-space customer bubble. Reads a gameplay timer through `IProgress01` (never owns it): patience ring shows time *left* in teal → yellow (<50%) → tomato (<25%, gentle pulse); eating ring fills in teal. Pops in when shown. |
| `UICookingBubble` | World-space cooking indicator over the pan/oven. Cooking: flame + ring filling in teal. Done: pop, check icon, and the ring becomes a "take it out" countdown with the same urgency colours until the food burns. Reads `CookingProgressBar`. |

Ring rule for world-space bubbles: a ring that *fills* = progress (eating,
cooking); a ring that *empties* = time left (patience, burn window) and uses
`UITokens.UrgencyColor`.

Button anatomy: root (transparent raycast `Image`, `Button` with
transition None, `UIPressable`) → `Shadow` (icon buttons over the world
only) → `Edge` (shifted down by depth 5–6) → `Face` (holds label/icon).
Styles: Primary (teal/dark teal, warm-white label), Icon (warm white over
the 3D world), IconAccent (teal), Subtle (pink-tinted, for secondary
actions on warm-white cards).

Card anatomy: root → `Shadow` (`ui_shadow`, `Shadow` token, expanded
~24 px, shifted down 8) → `Surface` (`ui_rounded`, warm white) → content.
Modal panels use a full-screen `Scrim` so the paused kitchen stays
visible behind the card.

## 29.5 Screen map (GameScene → `<<<UI>>>/StaticCanvas`)

- `Welcome` → `Poster` (full-screen title = the key art: pink field, chibi
  pattern, sun-burst, `Hero` with the chef render, streaks, sweat and the
  "Perfection optional!" callout; `Brand` with logo, "Stack fast. Serve
  faster." and the Start button). Built by `Assets/Editor/PosterLayout.cs`
  (Tools/Chibi UI/Build Title Poster, re-runnable). The old centred
  `TitleCard` is kept inactive (the rebuild moves `StartBTN` back and forth).
- `InGamePanel/HUD` → `CustomersCard` (top-left), `AudioControls` + `HelpBTN`
  (top-right), `Wallet` pill (top-right, tucked under the shop button),
  `ChefCard` (bottom-left: animated chef portrait + last-order stars +
  verdict), `Toast` (top-center).
- `InGamePanel/Purchase` → shop button slot (`purchaseImg/purchaseBTN`)
  and `PurchasePanel/ShopCard`.
- `HelpPanel/HelpCard`, `ScorePanel/BackgroundIMG/ResultCard`,
  `FarewellPanel` (not redesigned yet).
- Customer bubbles: `Prefabs/CharacterPrefabs/NpcPrefabs/Npc_WS_Canvas.prefab`
  (nested in Npc 1/2/3; `Npc.prefab` is an unused older copy).
  `OrderProgressBackground` / `EatProgressBackground` are toggled by
  `NpcCanvasController`; each holds the invisible timer driver
  `Mask_CircleProgressBar` and a `Bubble` (Shadow, Tail, Surface, Track,
  Ring, Icon) authored in UI pixels at scale 0.0135.

## 29.6 Constraints that are easy to break

- Chef animation clips target the path `ChefBackgroundIMG_white/ChibiIMG`
  (sprite + anchoredPosition.y). Never rename/re-parent inside
  `ChefBackgroundIMG`; resize by scaling that root.
- `BuyButton.PurchaseObject` is the whole `Purchase` root (hidden after the
  one-time purchase) — the shop button must stay under it.
- Panels are shown/hidden via `Panel` CanvasGroup alpha from `GamePanels`;
  gameplay pauses with `Time.timeScale = 0` → UI motion must use unscaled
  time.
- `EventManager` events are static; always unsubscribe with the same
  delegate in `OnDisable`.
- The ad SDK was removed: `ClaimBTN` ("Free with ad") is kept but hidden;
  Replay is wired directly to `RestartButton.Restart`.
- After Replay there are two `<<<Audio>>>` roots: the persistent one plays
  music, the reloaded scene's copy has its `Audio` component disabled but
  stays alive so scene references into it remain valid (intended).
- Customer bubble timers are gameplay: `NpcWaitProgressBar` (the customer's
  `NpcFsm.Patience`, 100–130 s, then they protest and leave) and `NpcEatProgressBar` (3 s, then the order
  is rated). Their GameObjects and `Image` must stay (image disabled, it only
  receives `fillAmount`); style the `Bubble` sibling, not the driver.
- `OrderProgressBackground` / `EatProgressBackground` are rotated 180° on Z in
  the prefab — anything placed under them needs a compensating 180° rotation.
- Cooking indicators live in `Prefabs/FoodPrefabs/Bun.prefab`
  (`BunCookProgressBar`) and `Uncooked_Burger.prefab` (`BurgerCookProgressBar`);
  `Prefabs/UIPrefabs/BurgerCookProgressBar.prefab` is an unused old copy.
  `CookingProgressBar` is presentation-only (its own 10 s cooked / 20 s burned
  clock mirroring `Pan`/`Oven`), but `Bun`/`Burger` call its
  `ResetProgressBar()`, so keep it (image disabled) under the canvas.
- `FarewellPanel` is still in the old style.
- World-space UI must not sit under non-uniformly scaled, rotated parents —
  that shears it. Food on utensils is parented to a runtime `<utensil> FoodSlot`
  (unscaled, aligned; `NonStackBase.UseFood`) for this reason; the oven object
  is scaled 1.41 × 0.01 × 1.15. Food bubbles follow the food at a fixed world
  offset (`Bun/BurgerCanvasController.worldOffset`), never accumulated offsets.
- Any "is the pointer over UI?" check must use `PointerUtility.IsOverUI()`
  (covers touches); `EventSystem.IsPointerOverGameObject()` alone lets taps on
  HUD buttons fall through to the world on phones.
- Serving: with a burger in hand, a tap near a customer on screen (they have no
  collider) or near a free service point serves there (`PlayerFSM.HandleScreenTap`);
  service points accept only burgers.
- "What can I tap" is `HighlightController`, and it must work without hover
  (phones). Both states are material swaps, never a glow/translucent overlay
  (the user rejected a glowing yellow overlay as "a light"):
  **yellow** (`HighlightMat`, solid) = mouse hover and the tap target until the
  chef arrives; **teal pulse** = "use it next" hints — where the held food can
  go, ready food, waiting customers + their table when holding a burger, the
  ingredient crates when hands are empty and nothing is ready. Hinted URP Lit
  materials are swapped for runtime copies using `Chibi/HintLit`
  (`Graphics/Shaders/HintLit.shader`: URP Lit + `lerp(albedo, _HintColor,
  _HintAmount)`), and the globals breathe 0 → 0.9 → 0 once a second, restarting
  when the hinted set changes — the object keeps its texture and shading and
  eases into teal and back. A *steady* teal was rejected ("looks like a bug"),
  so keep it pulsing. `HintPulseMat` (in the scene via the controller) keeps
  the shader's base variant in builds; hintable materials have no keywords —
  if one gains keywords (e.g. `_EMISSION`), add a template material with them.
  Teal was chosen because yellow disappears on cheese, the cutting board and
  white plates. In the editor the first frame after a new variant compiles
  shows Unity's flat cyan placeholder (async shader compilation) — not a bug.
  Only during a shift, never while paused. Materials are swapped (same count)
  and restored exactly; don't add other code that changes those renderers'
  materials.
- Tappables stay "Batching Static" (user requirement). A material *swap* is
  safe with static batching; *adding* a material is not (a batched renderer
  draws a slice of a combined mesh, so an extra material painted unrelated
  props) — never highlight by appending materials.
- The oven is tappable as a whole: `Level/Selectables/oven` has a BoxCollider
  + `TapProxy` (target = the tray `OvenStuff`'s `Oven`). `PlayerFSM.ResolveTap`
  turns a proxy hit into the utensil (food in hand) or the food inside (empty
  hands); the highlight lights the proxy object too. Use `TapProxy` for any
  other thin/awkward target.
- Food fight presentation: `UISplatPop` (HUD/SplatPop, `fx_splat` + "SPLAT!" in
  Permanent Marker) listens to `FoodFight.OnChefSplatted`; snack/bonk lines and
  "+$1" come through `UICustomerReaction` (raised above the order bubble while
  it shows). The splash particles use the Hyper Casual FX `Circles_AB`
  material (a URP Particles/Unlit material made in code didn't show up).
- `Npc_WS_Canvas/EatProgressBackground/Bubble` is inactive on purpose (the
  eating ring was replaced by the reaction bubble); its timer driver
  `Mask_CircleProgressBar` must stay active.
- EventManager listeners must be named methods (not lambdas): lambdas can't be
  removed, and after Replay a destroyed listener throws inside the event and
  aborts it (this broke dropping food via `PlaceableBase`).

## 29.7 Verifying UI changes

Review in Play mode over the real kitchen at 16:10 (e.g. Game view
1440×900) as well as 16:9, for every screen: title, HUD (empty and
mid-shift), help, shop (affordable and not), result, and after Replay.
For customer bubbles, let customers arrive naturally and fast-forward
`Time.timeScale` to see each patience band; world-space UI is small in a
full screenshot, so crop and enlarge the area around the bubble. For cooking,
drive the real click path (`PlayerFSM`: raycast → gather components → walk
→ `Interact`) to put a patty on the pan and a bun in the oven. Preview new
icons at their on-screen size (~26–30 px) before shipping — a symmetric
flame first read as a water drop.
