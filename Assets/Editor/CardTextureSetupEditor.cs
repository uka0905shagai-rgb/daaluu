#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CardTextureSetupEditor
{
    private static Dictionary<string, DaaluuPieceName> filenameTopiece = new Dictionary<string, DaaluuPieceName>()
    {
        { "daaluu", DaaluuPieceName.Daaluu },
        { "vvlvv", DaaluuPieceName.Uuluu },
        { "bajgar", DaaluuPieceName.Bajgar },
        { "rarav", DaaluuPieceName.Arav },
        { "r9", DaaluuPieceName.Ys },
        { "w9", DaaluuPieceName.Degee },
        { "tsawgants", DaaluuPieceName.Chavgants },
        { "dungu", DaaluuPieceName.Dungu },
        { "murui", DaaluuPieceName.Murui },
        { "shor", DaaluuPieceName.Shor },
        { "shanaga", DaaluuPieceName.Shanaga },
        { "sarlag", DaaluuPieceName.Sarlag },
        { "6w", DaaluuPieceName.Chans },
        { "buluu", DaaluuPieceName.Buluu },
        { "nohoi", DaaluuPieceName.Nohoi },
        { "chuu", DaaluuPieceName.Chuu },
        { "oims", DaaluuPieceName.Oims },
        { "band", DaaluuPieceName.Band },
        { "yoz", DaaluuPieceName.Yoz },
        { "tsesteg", DaaluuPieceName.Buluu }
    };

    [MenuItem("Tools/Daaluu/Setup Card Textures")]
    public static void SetupCardTextures()
    {
        CardTextureDatabase database = AssetDatabase.LoadAssetAtPath<CardTextureDatabase>("Assets/CardTextureDatabase.asset");
        
        if (database == null)
        {
            Debug.LogError("CardTextureDatabase not found at Assets/CardTextureDatabase.asset");
            return;
        }

        database.cardSpriteEntries.Clear();
        string[] imageGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/images" });

        foreach (string guid in imageGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string filename = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower();

            DaaluuPieceName? pieceName = FindMatchingPiece(filename);
            if (pieceName.HasValue)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture != null)
                {
                    var entry = new CardTextureDatabase.CardSpriteEntry();
                    entry.pieceName = pieceName.Value;
                    entry.frontAsset = texture;
                    database.cardSpriteEntries.Add(entry);
                    Debug.Log($"Added {pieceName} with texture {filename}");
                }
            }
            else
            {
                Debug.LogWarning($"No matching piece name found for {filename}");
            }
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log($"CardTextureDatabase setup complete! Added {database.cardSpriteEntries.Count} entries.");
    }

    private static DaaluuPieceName? FindMatchingPiece(string filename)
    {
        foreach (var kvp in filenameTopiece)
        {
            if (filename.Contains(kvp.Key))
                return kvp.Value;
        }
        return null;
    }
}
#endif
