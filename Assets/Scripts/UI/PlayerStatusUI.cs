using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a player's status (hand size, collected cards, etc.)
/// </summary>
public class PlayerStatusUI : MonoBehaviour
{
    private Player player;
    private TextMeshProUGUI playerNameText;
    private TextMeshProUGUI handSizeText;
    private TextMeshProUGUI teaCardsText;
    private Image playerHighlight;

    public void Initialize(Player playerRef)
    {
        player = playerRef;

        // Find or create UI elements
        playerNameText = GetOrCreateText("PlayerName", new Vector2(0f, 16f), new Vector2(140f, 20f), 16f);
        handSizeText = GetOrCreateText("HandSize", new Vector2(-32f, -10f), new Vector2(64f, 18f), 12f);
        teaCardsText = GetOrCreateText("TeaCards", new Vector2(32f, -10f), new Vector2(64f, 18f), 12f);
        playerHighlight = GetComponent<Image>();

        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (player == null) return;

        if (playerNameText != null)
            playerNameText.text = player.playerName;

        if (handSizeText != null)
            handSizeText.text = $"Ger: {player.GetTricksWon()}";

        if (teaCardsText != null)
            teaCardsText.text = $"Tea: {player.GetTeaCardsCollected()}/{GameRules.TOTAL_TEA_CARDS}";

        // Highlight if player is human
        if (playerHighlight != null)
        {
            // Bright yellow for human, dark gray for bots — opaque
            playerHighlight.color = player.IsHuman ? new Color(1f, 0.84f, 0.04f, 1f) : new Color(0.18f, 0.18f, 0.18f, 1f);
        }
    }

    private TextMeshProUGUI GetOrCreateText(string childName, Vector2 position, Vector2 size, float fontSize)
    {
        TextMeshProUGUI text = transform.Find(childName)?.GetComponent<TextMeshProUGUI>();
        if (text != null)
            return text;

        GameObject textObj = new GameObject(childName);
        textObj.transform.SetParent(transform, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;

        return text;
    }
}
