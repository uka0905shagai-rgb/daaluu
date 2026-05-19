using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class MirrorSetup
{
    [MenuItem("Mirror/Setup NetworkManager (safe)")]
    public static void SetupNetworkManager()
    {
        // Try to find Mirror's NetworkManager type via reflection
        Type nmType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == "Mirror.NetworkManager" || t.Name == "NetworkManager");

        if (nmType == null)
        {
            Debug.LogWarning("Mirror types not found. Install Mirror via Package Manager and restart the editor.");
            return;
        }

        // Create NetworkManager GameObject
        GameObject nmObj = GameObject.Find("NetworkManager");
        if (nmObj == null)
        {
            nmObj = new GameObject("NetworkManager");
            nmObj.transform.position = Vector3.zero;
            nmObj.AddComponent(nmType);
            Debug.Log("NetworkManager gameobject created.");
        }
        else
        {
            Debug.Log("NetworkManager already exists in scene.");
        }

        // Try to add NetworkManagerHUD if available
        Type hudType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == "Mirror.NetworkManagerHUD" || t.Name == "NetworkManagerHUD");

        if (hudType != null && nmObj.GetComponent(hudType) == null)
        {
            nmObj.AddComponent(hudType);
            Debug.Log("NetworkManagerHUD added.");
        }

        // Save scene
        if (!EditorSceneManager.GetActiveScene().isDirty)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Mirror setup complete (scaffold created). Configure the NetworkManager in inspector.");
    }
}
#endif
