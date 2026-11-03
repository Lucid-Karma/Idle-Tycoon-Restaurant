using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Step 17: what the player can tap can bounce (TapJuice, TapFeedback - user, 2026-10-30: "every interaction should give
// feedback; the ingredient you tap grows and shrinks or hops"). Static batching merges a renderer into one big mesh,
// and then its transform moves nothing on screen: the crates, the oven, the pan, the plates, the board, the bin, the
// customers' chairs and tables and the decoration props come out of static batching here. Only that flag: they stay
// lightmapped and their baked light and shadows are untouched (nothing is rebaked). A few more draw calls; the SRP
// batcher keeps them cheap.
public static class CafeJuiceBuilder
{
    // Not "Decorations": the season's decorations are measured and placed from those props' renderers (the candy under
    // the pumpkin from the giant tomato's), and out of batching their bounds grew - the candy spread far past the map
    // (user, 2026-10-30). They stay batched; the season's own decorations (made at runtime) still bounce.
    private static readonly string[] Groups = { "Selectables", "Plates", "Small Kitchen Utensil", "ChoppingArea", "Bin" };

    [MenuItem("Tools/Chibi Cafe/17 Taps Bounce (unbatch what can be tapped)")]
    public static void Menu() => Debug.Log(Unbatch());

    public static string Unbatch()
    {
        var level = GameObject.Find("Level");
        if (level == null) return "no Level";
        int n = 0;
        foreach (var r in level.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!Wanted(r.transform)) continue;
            var go = r.gameObject;
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            if ((flags & StaticEditorFlags.BatchingStatic) == 0) continue;
            GameObjectUtility.SetStaticEditorFlags(go, flags & ~StaticEditorFlags.BatchingStatic);
            n++;
        }
        EditorSceneManager.MarkSceneDirty(level.scene);
        EditorSceneManager.SaveScene(level.scene);
        return n + " renderers out of static batching";
    }

    // Puts the decoration props back in static batching (undoing an earlier run of step 17 that took them out).
    public static string RebatchDecorations()
    {
        var level = GameObject.Find("Level");
        var decor = level != null ? level.transform.Find("Decorations") : null;
        if (decor == null) return "no Decorations";
        int n = 0;
        foreach (var r in decor.GetComponentsInChildren<MeshRenderer>(true))
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if (!r.gameObject.isStatic || (flags & StaticEditorFlags.BatchingStatic) != 0) continue;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags | StaticEditorFlags.BatchingStatic);
            n++;
        }
        EditorSceneManager.MarkSceneDirty(level.scene);
        EditorSceneManager.SaveScene(level.scene);
        return n + " decoration renderers back in static batching";
    }

    private static bool Wanted(Transform t)
    {
        string own = t.name.ToLower();
        for (var p = t; p != null; p = p.parent)
        {
            foreach (var g in Groups) if (p.name == g) return true;
            // Chairs and tables (the dining room's and the shop's).
            if ((p.name == "CustomerArea" || p.name == "Upgrades") && (own.StartsWith("chair") || own.StartsWith("table") || own.Contains("stool")))
                return true;
        }
        return false;
    }
}
