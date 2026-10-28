using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Step 11 (re-runnable): what CrazyGames needs (docs.crazygames.com/requirements).
// - Scenes/Boot.unity, first in the build: starts the CrazyGames SDK before the cafe reads its save (Boot,
//   GamePlatform), an empty camera in the game's background colour meanwhile.
// - "Double it" on the result card (UIRewardedBonus): a rewarded video pays the shift's earnings again. It sits
//   in the row beside Shop, so "Play again" stays the main button; it hides itself where there are no ads.
// The midgame ad between shifts needs no UI (RestartButton).
public static partial class CafeGrowthBuilder
{
    const string BootScene = Game + "Scenes/Boot.unity";
    const string MainScene = Game + "Scenes/GameScene.unity";

    [MenuItem("Tools/Chibi Cafe/11 CrazyGames: Boot Scene and Ad Button")]
    public static void BuildPlatform()
    {
        UISpriteBaker.BakeVideoIcon();
        var gameScene = SceneManager.GetActiveScene();
        if (gameScene.path != MainScene) throw new System.Exception("Open " + MainScene + " first");
        BuildBonusButton();
        FitWallet();
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);
        BuildBootScene(Camera.main.backgroundColor);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(BootScene, true),
            new EditorBuildSettingsScene(MainScene, true),
        };
        Debug.Log("[CafeGrowth] CrazyGames boot scene and rewarded button built");
    }

    static void BuildBonusButton()
    {
        var cardGo = GameObject.Find("<<<UI>>>/StaticCanvas/ScorePanel/BackgroundIMG/ResultCard");
        // The result card (ScorePanel) was removed from the scene on 2026-10-17: nothing to build into any more.
        if (cardGo == null) { Debug.LogWarning("[CafePlatform] no result card: bonus button not built"); return; }
        var card = cardGo.transform;
        var row = card.Find("InteractionArea/Row");
        var old = row.Find("BonusBTN");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var shopButton = row.Find("ShopBTN");
        var bonus = Object.Instantiate(shopButton.gameObject, row);
        bonus.name = "BonusBTN";
        bonus.transform.SetSiblingIndex(shopButton.GetSiblingIndex() + 1);
        foreach (var badge in bonus.GetComponentsInChildren<UIShopBadge>(true)) Object.DestroyImmediate(badge);
        var badgeObject = bonus.transform.Find("Badge");
        if (badgeObject != null) Object.DestroyImmediate(badgeObject.gameObject);

        var face = bonus.transform.Find("Face");
        var cart = face.Find("Cart");
        var icon = (RectTransform)cart;
        icon.name = "Video";
        icon.sizeDelta = new Vector2(36, 36);
        icon.anchoredPosition = new Vector2(-66, 0);
        Img(icon.gameObject, "icon_video", UITokens.Colors.Ink);

        var label = face.GetComponentInChildren<TMP_Text>(true);
        label.text = "Double it";

        var button = bonus.GetComponent<Button>();
        while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);

        var reward = Get<UIRewardedBonus>(bonus);
        var so = new SerializedObject(reward);
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("card").objectReferenceValue = card.GetComponent<CanvasGroup>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The till grows past $999 with play (and past $9999 for a long-time player); at a fixed 36 pt the digits ran
    // under the shop's cart button. The amount now shrinks to fit the pill (down to 22 pt).
    static void FitWallet()
    {
        var text = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel/HUD/Wallet/Text (TMP)").GetComponent<TMP_Text>();
        var rect = (RectTransform)text.transform;
        rect.sizeDelta = new Vector2(84f, rect.sizeDelta.y);
        text.enableAutoSizing = true;
        text.fontSizeMax = 36f;
        text.fontSizeMin = 22f;
        text.enableWordWrapping = false;
        EditorUtility.SetDirty(text);
    }

    static void BuildBootScene(Color background)
    {
        var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var cam = cameraObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.cullingMask = 0;
        cameraObject.AddComponent<AudioListener>();
        SceneManager.MoveGameObjectToScene(cameraObject, boot);
        var bootObject = new GameObject("Boot");
        bootObject.AddComponent<Boot>();
        SceneManager.MoveGameObjectToScene(bootObject, boot);
        EditorSceneManager.SaveScene(boot, BootScene);
        EditorSceneManager.CloseScene(boot, true);
    }
}
