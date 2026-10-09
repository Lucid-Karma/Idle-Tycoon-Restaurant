using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Step 7 (re-runnable): sounds, the chef's slip, and the phone-battery settings that live in the scene.
// - <<<Controllers>>>/Sfx: GameSfx with the cue → clip table below (Free UI Click Sound Effects Pack, and the
//   kitchen's own synthesised sounds in Graphics/Audio/Kitchen).
// - PlayerAnim.controller: "Slip" trigger → Slip (fall on his back) → StandUp → Idle.
// - The apron rides on the hips bone (it floated upright while the chef lay on the floor).
// - UI: no pixel-perfect on the HUD canvas, and the parts that animate all the time get a canvas of their own,
//   so a moving portrait or title doesn't rebuild the whole HUD every frame.
// - WebGL template "ChibiCafe" (frame limiter + pixel-ratio cap on phones).
public static partial class CafeGrowthBuilder
{
    const string ClickPack = "Assets/Models/Free UI Click Sound Effects Pack/AUDIO/";
    const string PlayerController = Game + "Animations/CharacterAnims/PlayerAnims/PlayerAnim.controller";

    // cue, clips, volume, pitch, pitch jitter
    static readonly (GameSfx.Cue cue, string[] clips, float volume, float pitch, float jitter)[] Sounds =
    {
        (GameSfx.Cue.Coin, new[] { "Plastic/SFX_UI_Click_Designed_Plastic_Pop_Coin_Generic_1.wav" }, 0.8f, 1f, 0.04f),
        (GameSfx.Cue.Tip, new[] { "Plastic/SFX_UI_Click_Designed_Plastic_Pop_Coin_Generic_1.wav" }, 0.55f, 1.25f, 0.04f),
        (GameSfx.Cue.Seated, new[] { "Plastic/SFX_UI_Click_Organic_Plastic_Bouncy_Generic_1.wav" }, 0.35f, 1f, 0.08f),
        (GameSfx.Cue.LeftHungry, new[] { "Plastic/SFX_UI_Click_Designed_Plastic_Metallic_Negative_Close_1.wav" }, 0.7f, 1f, 0f),
        (GameSfx.Cue.CustomerThrow, new[] { "Plastic/SFX_UI_Click_Organic_Plastic_Mouth_Generic_1.wav" }, 0.6f, 0.9f, 0.08f),
        (GameSfx.Cue.ChefThrow, new[] { "Plastic/SFX_UI_Click_Organic_Plastic_Mouth_Generic_1.wav" }, 0.45f, 1.25f, 0.08f),
        (GameSfx.Cue.Splat, new[] { "Liquid/SFX_UI_Click_Designed_Liquid_Thick_Generic_1.wav", "Liquid/SFX_UI_Click_Designed_Liquid_Thick_Generic_2.wav" }, 0.9f, 0.85f, 0.06f),
        (GameSfx.Cue.Slip, new[] { "Liquid/SFX_UI_Click_Designed_Liquid_Airy_Thick_Negative_Error_1.wav" }, 0.8f, 0.9f, 0.03f),
        (GameSfx.Cue.Bonk, new[] { "Plastic/SFX_UI_Click_Organic_Plastic_Negative_1.wav" }, 0.6f, 1f, 0.06f),
        (GameSfx.Cue.BurgerDone, new[] { "Pop/SFX_UI_Click_Designed_Pop_Mallet_Open_1.wav" }, 0.6f, 1f, 0.04f),
        (GameSfx.Cue.LevelUp, new[] { "../../../Project/[GAME]/Graphics/Audio/RewardedAdSound.mp3" }, 0.8f, 1f, 0f),
        (GameSfx.Cue.ArrowShot, new[] { "Metallic/SFX_UI_Click_Organic_Metallic_Thin_Select_1.wav" }, 0.55f, 0.8f, 0.05f),
        (GameSfx.Cue.ArrowHit, new[] { "Liquid/SFX_UI_Click_Organic_Liquid_Wooden_1.wav" }, 0.9f, 0.85f, 0.05f),
        // The kitchen (no pack has these: synthesised, Graphics/Audio/Kitchen). Quiet: they happen all the time.
        (GameSfx.Cue.Pickup, new[] { Kitchen + "kitchen_pickup.wav" }, 0.3f, 1f, 0.08f),
        (GameSfx.Cue.PutDown, new[] { Kitchen + "kitchen_putdown.wav" }, 0.35f, 1f, 0.08f),
        (GameSfx.Cue.Chop, new[] { Kitchen + "kitchen_chop_1.wav", Kitchen + "kitchen_chop_2.wav", Kitchen + "kitchen_chop_3.wav", Kitchen + "kitchen_chop_4.wav" }, 0.55f, 1f, 0.05f),
        (GameSfx.Cue.Ready, new[] { Kitchen + "kitchen_ready.wav" }, 0.45f, 1f, 0f),
        (GameSfx.Cue.Burnt, new[] { Kitchen + "kitchen_burnt.wav" }, 0.35f, 1f, 0.03f),   // soft: a louder puff made the user jump
        (GameSfx.Cue.Trash, new[] { "Plastic/SFX_UI_Click_Organic_Plastic_Boxy_Negative_1.wav" }, 0.45f, 0.9f, 0.05f),
    };
    const string Kitchen = "../../../Project/[GAME]/Graphics/Audio/Kitchen/";
    const string SizzleLoop = Game + "Graphics/Audio/Kitchen/kitchen_sizzle_loop.wav";

    const string ArrowFbx = Game + "ThirdPartyPackages/KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/arrow_bow.fbx";
    const string BowFbx = Game + "ThirdPartyPackages/KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/bow_withString.fbx";

    [MenuItem("Tools/Chibi Cafe/7 Sounds, Slip and Performance")]
    public static void Polish()
    {
        BuildSfx();
        BuildBurnSmoke();
        BuildSlipAnimation();
        ApronOnHips();
        LighterCanvases();
        // Meshes combined in the scene without lightmap UVs baked garbage shadowmask: a black bin, blotchy onions.
        foreach (var path in new[] { "Level/Selectables/Bin", "Level/Selectables/Onion" })
            Get<RealtimeLit>(GameObject.Find(path));
        PlayerSettings.WebGL.template = "PROJECT:ChibiCafe";
        // A Ranger shoots an arrow instead of throwing a tomato.
        var fight = new SerializedObject(Object.FindFirstObjectByType<FoodFight>());
        fight.FindProperty("arrowModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowFbx);
        fight.FindProperty("bowModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(BowFbx);
        fight.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(GameObject.Find("<<<Controllers>>>").scene);
        Debug.Log("[CafeGrowth] sounds, slip and performance settings applied");
    }

    // Play mode with the phone settings (frame cap, cheaper shadows/AA/bloom) to see what phones get.
    [MenuItem("Tools/Chibi Cafe/Play As Phone")]
    static void TogglePlayAsPhone() =>
        EditorPrefs.SetBool(PerformanceGovernor.PlayAsPhonePref, !EditorPrefs.GetBool(PerformanceGovernor.PlayAsPhonePref, false));

    [MenuItem("Tools/Chibi Cafe/Play As Phone", true)]
    static bool TogglePlayAsPhoneCheck()
    {
        Menu.SetChecked("Tools/Chibi Cafe/Play As Phone", EditorPrefs.GetBool(PerformanceGovernor.PlayAsPhonePref, false));
        return !Application.isPlaying;
    }

    static void BuildSfx()
    {
        var controllers = GameObject.Find("<<<Controllers>>>").transform;
        var old = controllers.Find("Sfx");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject("Sfx");
        go.transform.SetParent(controllers, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        var sfx = go.AddComponent<GameSfx>();

        var so = new SerializedObject(sfx);
        var list = so.FindProperty("sounds");
        list.arraySize = Sounds.Length;
        for (int i = 0; i < Sounds.Length; i++)
        {
            var (cue, clips, volume, pitch, jitter) = Sounds[i];
            var e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("cue").enumValueIndex = (int)cue;
            var clipList = e.FindPropertyRelative("clips");
            clipList.arraySize = clips.Length;
            for (int c = 0; c < clips.Length; c++)
            {
                var path = System.IO.Path.GetFullPath(ClickPack + clips[c]).Replace('\\', '/');
                path = "Assets" + path.Substring(Application.dataPath.Length);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path) ?? throw new System.Exception("no clip " + path);
                clipList.GetArrayElementAtIndex(c).objectReferenceValue = clip;
            }
            e.FindPropertyRelative("volume").floatValue = volume;
            e.FindPropertyRelative("pitch").floatValue = pitch;
            e.FindPropertyRelative("pitchJitter").floatValue = jitter;
        }
        so.FindProperty("sizzleLoop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(SizzleLoop) ?? throw new System.Exception("no clip " + SizzleLoop);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Smoke over a burnt bun or patty (BurnSmoke builds its particle systems at runtime from this material).
    // A copy of the Hyper Casual FX particle material (its shader is the one that draws in this project: a
    // URP Particles/Unlit material made from code drew nothing) with our own soft puff texture
    // (Graphics/Sprites/FX/smoke_puff.png, Tools/FX/smoke_puff.py) instead of the pack's thin ring.
    const string SmokeMaterialFrom = "Assets/Lana Studio/Hyper Casual FX/Materials/Circles_AB.mat";
    const string SmokeTexture = Game + "Graphics/Sprites/FX/smoke_puff.png";
    const string SmokeMaterial = Game + "Graphics/Materials/BurnSmoke.mat";

    static void BuildBurnSmoke()
    {
        AssetDatabase.DeleteAsset(SmokeMaterial);
        var mat = new Material(AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialFrom));
        mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(SmokeTexture) ?? throw new System.Exception("no texture " + SmokeTexture));
        AssetDatabase.CreateAsset(mat, SmokeMaterial);

        var controllers = GameObject.Find("<<<Controllers>>>").transform;
        var old = controllers.Find("BurnSmoke");
        var go = old != null ? old.gameObject : new GameObject("BurnSmoke");
        go.transform.SetParent(controllers, false);
        var smoke = go.GetComponent<BurnSmoke>();
        if (smoke == null) smoke = go.AddComponent<BurnSmoke>();
        var so = new SerializedObject(smoke);
        so.FindProperty("particleMaterial").objectReferenceValue = mat;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Slip: Death_A (backwards onto the floor) then Lie_StandUp, sped up to fit FoodFight.slipSeconds (2.1 s).
    static void BuildSlipAnimation()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerController);
        var clips = AssetDatabase.LoadAllAssetsAtPath(SkullFbx).OfType<AnimationClip>().ToArray();
        var fall = clips.First(c => c.name == "Death_A");
        var getUp = clips.First(c => c.name == "Lie_StandUp");

        if (!controller.parameters.Any(p => p.name == "Slip"))
            controller.AddParameter("Slip", AnimatorControllerParameterType.Trigger);
        var sm = controller.layers[0].stateMachine;
        foreach (var old in sm.states.Where(s => s.state.name == "Slip" || s.state.name == "StandUp").ToList())
            sm.RemoveState(old.state);
        foreach (var t in sm.anyStateTransitions.Where(t => t.destinationState == null).ToList())
            sm.RemoveAnyStateTransition(t);
        var idle = sm.states.First(s => s.state.name == "Idle").state;

        var slip = sm.AddState("Slip", new Vector3(300f, 300f));
        slip.motion = fall;
        slip.speed = 1.2f;
        var standUp = sm.AddState("StandUp", new Vector3(300f, 380f));
        standUp.motion = getUp;
        standUp.speed = 1.7f;

        var toSlip = sm.AddAnyStateTransition(slip);
        toSlip.AddCondition(AnimatorConditionMode.If, 0f, "Slip");
        toSlip.duration = 0.08f;
        toSlip.hasExitTime = false;
        toSlip.canTransitionToSelf = false;

        var fallen = slip.AddTransition(standUp);
        fallen.hasExitTime = true;
        fallen.exitTime = 1f;
        fallen.duration = 0.12f;

        var up = standUp.AddTransition(idle);
        up.hasExitTime = true;
        up.exitTime = 0.95f;
        up.duration = 0.15f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    static void ApronOnHips()
    {
        var model = GameObject.Find("Player/Model").transform;
        var apron = model.GetComponentsInChildren<Transform>(true).First(t => t.name == "Kitchen Apron");
        var hips = model.Find("Rig/root/hips");
        if (apron.parent != hips) apron.SetParent(hips, true);
    }

    static void LighterCanvases()
    {
        var canvas = GameObject.Find(Canvas).GetComponent<Canvas>();
        canvas.pixelPerfect = false;
        // Parts that move on their own get a nested canvas: their changes rebuild only themselves.
        OwnCanvas("Welcome/Poster", interactive: true);
        OwnCanvas("InGamePanel/HUD/ChefCard/Portrait", interactive: false);
        OwnCanvas("InGamePanel/HUD/Toast", interactive: false);
        OwnCanvas("InGamePanel/HUD/SplatPop", interactive: false);
    }

    static void OwnCanvas(string path, bool interactive)
    {
        var t = GameObject.Find(Canvas).transform.Find(path) ?? throw new System.Exception("no " + path);
        Get<Canvas>(t.gameObject);
        if (interactive) Get<GraphicRaycaster>(t.gameObject);
    }
}
