using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardTextureDatabase))]
public class CardTextureDatabaseEditor : Editor
{
    private SerializedProperty defaultFrontAssetProp;
    private SerializedProperty teaCardAssetProp;
    private SerializedProperty cardSpriteEntriesProp;

    private void OnEnable()
    {
        defaultFrontAssetProp = serializedObject.FindProperty("defaultFrontAsset");
        teaCardAssetProp = serializedObject.FindProperty("teaCardAsset");
        cardSpriteEntriesProp = serializedObject.FindProperty("cardSpriteEntries");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Card Texture Database", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(defaultFrontAssetProp, new GUIContent("Default Front Asset"));
        EditorGUILayout.PropertyField(teaCardAssetProp, new GUIContent("Tea Card Asset"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Card Sprite Entries", EditorStyles.boldLabel);

        if (cardSpriteEntriesProp != null)
        {
            for (int i = 0; i < cardSpriteEntriesProp.arraySize; i++)
            {
                SerializedProperty entryProp = cardSpriteEntriesProp.GetArrayElementAtIndex(i);
                SerializedProperty pieceNameProp = entryProp.FindPropertyRelative("pieceName");
                SerializedProperty frontAssetProp = entryProp.FindPropertyRelative("frontAsset");

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.PropertyField(pieceNameProp);
                EditorGUILayout.PropertyField(frontAssetProp, new GUIContent("Front Asset"));
                EditorGUILayout.EndVertical();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
