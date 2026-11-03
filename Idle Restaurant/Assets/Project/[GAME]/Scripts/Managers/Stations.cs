using UnityEngine;

// The kitchen's later stations (2026-10-28, user: "go on with the bacon and potato stations"): the bacon griddle opens
// at level 3 and the fryer at level 5 - the levels between the tiers' news (2, 4, 6...), so each of the first six levels
// brings something. A station opens by itself (the cafe grows; nothing to buy first) and is introduced the way the
// genre's best do it, inside the game: a card between levels ("NEW STATION"), its picture on the road, its recipe as
// the level's special, its first order coming in at once, and a guided first go with Mochi's card and the pointing
// hand (Tutorial.BeginStationTip). Its recipes are only ordered while it is open (Orders).
public static class Stations
{
    public enum Kind { Bacon, Fries }
    public static readonly Kind[] All = { Kind.Bacon, Kind.Fries };

    public static int OpensAt(Kind kind) => kind == Kind.Bacon ? 3 : 5;

    // A save from before levels whose cafe tier was already past this level: open from the start, without news.
    private static bool OpenBefore(Kind kind) => CafeProgress.LegacyTier > CafeProgress.TierAt(OpensAt(kind));

    public static bool Open(Kind kind) => CafeProgress.LevelNumber >= OpensAt(kind) || OpenBefore(kind);

    // The station that opens on reaching this level, if one does (the break cards' news, the road's picture).
    public static bool OpensOn(int levelNumber, out Kind kind)
    {
        foreach (var k in All)
            if (OpensAt(k) == levelNumber && !OpenBefore(k)) { kind = k; return true; }
        kind = default;
        return false;
    }

    // Opened at this level or the one before: its things come up more often (Orders.Pick), so it gets used.
    public static bool Fresh(Kind kind) => Open(kind) && CafeProgress.LevelNumber - OpensAt(kind) <= 1;

    public static string Title(Kind kind) => kind == Kind.Bacon ? "Bacon Griddle" : "Fryer";
    public static string Blurb(Kind kind) => kind == Kind.Bacon ? "Crispy bacon, new burgers" : "Fries on the side";
    // A word for the road and the next-unlock line ("Level 5: Fries").
    public static string Word(Kind kind) => kind == Kind.Bacon ? "Bacon" : "Fries";

    // A recipe with bacon in it is only on the menu once the griddle is open.
    public static bool Serves(Recipe recipe) => !recipe.NeedsBacon || Open(Kind.Bacon);

    // The guided first go is shown once per station (and again if it was walked away from: Tutorial).
    public static string TaughtKey(Kind kind) => "Cafe.StationTip." + kind;
    public static bool Taught(Kind kind) => GamePlatform.GetInt(TaughtKey(kind), 0) == 1;

    public static void MarkTaught(Kind kind, bool taught)
    {
        GamePlatform.SetInt(TaughtKey(kind), taught ? 1 : 0);
        GamePlatform.Flush();
    }

    // Open but its first go not given yet: its first order is due (Orders, NpcFsm) and the tip waits for it.
    public static bool TipPending(Kind kind) => Open(kind) && !Taught(kind) && CafeProgress.Taught;
}
