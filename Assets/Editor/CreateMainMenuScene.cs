using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public static class CreateMainMenuScene
{
    [MenuItem("Tools/Create Main Menu Scene")]
    public static void CreateScene()
    {
        // Create a new empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Main Camera
        GameObject camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        // Canvas
        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Event System: prefer the new Input System UI module when available, otherwise fall back to StandaloneInputModule
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
        // Try to find InputSystemUIInputModule type via loaded assemblies
        System.Type inputSystemModuleType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            inputSystemModuleType = asm.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputSystemModuleType != null) break;
        }
        if (inputSystemModuleType != null)
        {
            es.AddComponent(inputSystemModuleType);
        }
        else
        {
            es.AddComponent(typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // Mirror NetworkManager (if Mirror is available in the project)
        TryCreateNetworkManager();

        // Create a container for the main menu and attach MainMenuUI
        GameObject mainMenu = new GameObject("MainMenu");
        mainMenu.transform.SetParent(canvasGO.transform, false);
        RectTransform rt = mainMenu.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.2f);
        rt.anchorMax = new Vector2(0.8f, 0.8f);

        // Attach the MainMenuUI component if it's available
        var menuComp = mainMenu.AddComponent<MainMenuUI>();

        // Ensure the Assets/Scenes directory exists
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        string path = "Assets/Scenes/MainMenu.unity";

        // Save the scene
        if (EditorSceneManager.SaveScene(scene, path))
        {
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Main Menu Scene", $"Scene created at {path}", "OK");
        }
        else
        {
            Debug.LogError("Failed to save scene to " + path);
        }
    }

    private static void TryCreateNetworkManager()
    {
        System.Type networkManagerType = null;
        System.Type transportType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (networkManagerType == null)
                networkManagerType = asm.GetType("Mirror.NetworkManager");

            if (transportType == null)
                transportType = asm.GetType("Mirror.TelepathyTransport");

            if (transportType == null)
                transportType = asm.GetType("Mirror.KcpTransport");

            if (networkManagerType != null && transportType != null)
                break;
        }

        if (networkManagerType == null)
        {
            Debug.LogWarning("Mirror.NetworkManager not found. Skipping NetworkManager creation.");
            return;
        }

        GameObject existing = GameObject.Find("NetworkManager");
        if (existing != null && existing.GetComponent(networkManagerType) != null)
        {
            return;
        }

        GameObject nm = new GameObject("NetworkManager");
        nm.AddComponent(networkManagerType);

        if (transportType != null && nm.GetComponent(transportType) == null)
        {
            nm.AddComponent(transportType);
        }
    }
}
