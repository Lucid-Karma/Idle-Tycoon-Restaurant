using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// The opening titles' font (LessonIntro, 2026-10-21; user: "something more anime"): Bangers (OFL, Google Fonts),
// the manga sound-effect face, as a STATIC TextMeshPro font asset holding only the capitals, digits and a few marks -
// one 512x512 atlas, so it adds little to the download - with its material's outline and underlay switched on, so
// the build keeps that shader variant (the titles set their colours at runtime; a keyword switched on only at runtime
// would find its variant stripped). Rerun after changing the characters. Tools/Chibi Cafe/16.
public static class TitleFontBuilder
{
    const string Ttf = "Assets/Project/[GAME]/Graphics/Font/Bangers/Bangers-Regular.ttf";
    const string Out = "Assets/Project/[GAME]/Graphics/Font/Bangers/Bangers SDF.asset";
    const string Chars = " !'\",.-?&:0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    [MenuItem("Tools/Chibi Cafe/16 Build Title Font")]
    public static void BuildMenu() => Debug.Log("[TitleFont] " + Build());

    public static string Build()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(Ttf);
        if (font == null) return "no " + Ttf;
        // SDF16 at 96 pt: the faster SDFAA at 64 pt drew a wobbly, "pencil" outline and soft pixelated edges at
        // title size (user, 2026-10-21); the padding leaves room for the outline and the offset shadow.
        var asset = TMP_FontAsset.CreateFontAsset(font, 96, 12, GlyphRenderMode.SDF16, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        asset.name = "Bangers SDF";
        asset.TryAddCharacters(Chars, out string missing);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.DeleteAsset(Out);
        AssetDatabase.CreateAsset(asset, Out);
        asset.atlasTexture.name = "Bangers SDF Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
        var material = asset.material;
        material.name = "Bangers SDF Material";
        material.EnableKeyword("OUTLINE_ON");
        material.EnableKeyword("UNDERLAY_ON");
        material.SetFloat("_OutlineWidth", 0.3f);
        material.SetColor("_OutlineColor", new Color32(0x10, 0x26, 0x3A, 0xFF));
        material.SetColor("_UnderlayColor", new Color32(0xE0, 0x45, 0x7B, 0xFF));
        material.SetFloat("_UnderlayOffsetX", 0.6f);
        material.SetFloat("_UnderlayOffsetY", -0.6f);
        material.SetFloat("_UnderlayDilate", 0.3f);
        ShaderUtilities.UpdateShaderRatios(material);
        AssetDatabase.AddObjectToAsset(material, asset);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(Out);
        return $"{asset.characterTable.Count} characters, missing '{missing}', atlas {asset.atlasTexture.width}x{asset.atlasTexture.height}";
    }
}
