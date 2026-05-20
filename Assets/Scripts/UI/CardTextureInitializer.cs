using UnityEngine;

public class CardTextureInitializer : MonoBehaviour
{
    private void Awake()
    {
        GameBoardUI gameBoardUI = FindObjectOfType<GameBoardUI>();
        if (gameBoardUI != null)
        {
            // Try to load the CardTextureDatabase asset
            CardTextureDatabase database = Resources.Load<CardTextureDatabase>("CardTextureDatabase");
            
            if (database == null)
            {
                Debug.LogWarning("CardTextureDatabase not found in Resources folder. Please assign it in the Inspector.");
                return;
            }

            gameBoardUI.SetCardTextureDatabase(database);
            Debug.Log("CardTextureDatabase auto-assigned to GameBoardUI");
        }
    }
}
