using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Store covers (landscape 16:9, portrait 2:3, square 1:1) are composed from two kinds of renders of the
// game's own things, both on a transparent background (matted from a black and a white render, as the key art
// is), then laid out by Tools/Covers/compose_covers.py:
//  - RenderCafe: the cafe as it is in play mode right now, seen by the game's camera (stage it first: buy the
//    shop, let customers sit, put a burger in the chef's hands). The world-space bubbles are hidden.
//  - RenderFood: one ingredient's look, posed (a bun top, a cheese slice, a lettuce leaf...), for floating around it.
// Both are static methods taking only simple arguments, so the editor bridge can call them with `scall`.
public static class CoverRenderer
{
    const string Game = KeyArtRenderer.Game;

    // The scene as the game camera sees it, with room to spare, trimmed to what is in it.
    public static string RenderCafe(string path, int width, int height, float margin)
    {
        var main = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(c => c.enabled).ToList();
        foreach (var c in canvases) c.enabled = false;
        var clone = Object.Instantiate(main.gameObject);
        clone.name = "__CoverCamera";
        try
        {
            foreach (var b in clone.GetComponents<MonoBehaviour>()) if (!(b is UniversalAdditionalCameraData)) Object.DestroyImmediate(b);
            foreach (var l in clone.GetComponents<AudioListener>()) Object.DestroyImmediate(l);
            clone.tag = "Untagged";
            var cam = clone.GetComponent<Camera>();
            cam.orthographicSize = main.orthographicSize * margin;
            cam.aspect = width / (float)height;
            cam.enabled = false;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, KeyArtRenderer.RenderMatted(cam, width, height));
        }
        finally
        {
            Object.DestroyImmediate(clone);
            foreach (var c in canvases) if (c != null) c.enabled = true;
        }
        return path;
    }

    // One ingredient, posed by Euler angles, framed to fit; output trimmed. kind: bun | patty | cheese | lettuce |
    // tomatoSlice | onionSlice | tomato | onion.
    public static string RenderFood(string path, string kind, float rx, float ry, float rz, int size)
    {
        var (prefab, field) = kind switch
        {
            "bun" => ("Hamburger", "finishBun"),
            "bottomBun" => ("Bun", "bunSlice"),
            "patty" => ("Uncooked_Burger", "cookedBurger"),
            "cheese" => ("Cheese", "slicedCheese"),
            "lettuce" => ("Lettuce", "purePrefab"),
            "tomatoSlice" => ("Tomato", "slicedTomato"),
            "onionSlice" => ("Onion", "slicedOnion"),
            "tomato" => ("Tomato", "purePrefab"),
            "onion" => ("Onion", "purePrefab"),
            _ => throw new System.ArgumentException(kind),
        };
        var stage = new GameObject("__CoverFoodStage") { hideFlags = HideFlags.DontSave };
        stage.transform.position = KeyArtRenderer.StageOrigin;
        try
        {
            var item = KeyArtRenderer.Place(KeyArtRenderer.Visual(prefab, field), stage.transform, kind);
            item.localPosition = Vector3.zero;
            item.rotation = Quaternion.Euler(rx, ry, rz);
            var bounds = KeyArtRenderer.WorldBounds(item);

            var camGo = new GameObject("Cam") { hideFlags = HideFlags.DontSave };
            camGo.transform.SetParent(stage.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 24f;
            cam.aspect = 1f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            float distance = bounds.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
            // Looking at it from the front and a little above, like the game camera looks at the cafe.
            var dir = Quaternion.Euler(-12f, 0f, 0f) * Vector3.forward;
            camGo.transform.rotation = Quaternion.LookRotation(dir);
            camGo.transform.position = bounds.center - dir * distance;
            cam.enabled = false;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, KeyArtRenderer.RenderMatted(cam, size, size));
        }
        finally { Object.DestroyImmediate(stage); }
        return path;
    }
}
