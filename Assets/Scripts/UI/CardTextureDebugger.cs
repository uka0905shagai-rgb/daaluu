using UnityEngine;

public class CardTextureDebugger : MonoBehaviour
{
    [SerializeField] private CardTextureDatabase cardTextureDatabase;

    private void Start()
    {
        if (cardTextureDatabase == null)
        {
            Debug.LogError("CardTextureDatabase is not assigned!");
            return;
        }

        Debug.Log($"CardTextureDatabase loaded with {cardTextureDatabase.cardSpriteEntries.Count} entries");

        foreach (var entry in cardTextureDatabase.cardSpriteEntries)
        {
            Sprite sprite = entry.GetSprite();
            Debug.Log($"  - {entry.pieceName}: {(sprite != null ? "✓ Loaded" : "✗ Failed")} (Asset: {entry.frontAsset})");
        }
    }

    public void DebugCard(Card card)
    {
        if (card == null || cardTextureDatabase == null)
            return;

        Sprite sprite = cardTextureDatabase.GetSpriteForCard(card);
        Debug.Log($"Card {card.displayName} ({card.pieceName}): {(sprite != null ? "✓ Found sprite" : "✗ No sprite found")}");
    }
}
