using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a single card in the UI.
/// </summary>
public class CardUI : MonoBehaviour
{
    [SerializeField] private CardTextureDatabase textureDatabase;
    [SerializeField] private Sprite defaultCardSprite;
    [SerializeField] private bool hideTextWhenUsingTexture = true;

    private Card card;
    private Player owner;
    private GameFlowController gameFlowController;
    private Button cardButton;
    private TextMeshProUGUI cardText;
    private Image cardImage;
    private Color baseColor;
    private Color defaultBaseColor;
    private readonly Color selectedColor = new Color(1f, 0.92f, 0.42f, 1f);
    private readonly Color janliiColor = new Color(1f, 0.82f, 0.2f, 1f);
    private bool isSelected = false;

    public void Initialize(Card cardRef, Player ownerRef, GameFlowController flowController, CardTextureDatabase database = null)
    {
        card = cardRef;
        owner = ownerRef;
        gameFlowController = flowController;
        if (database != null)
            textureDatabase = database;

        cardButton = GetComponent<Button>();
        if (cardButton == null)
            cardButton = gameObject.AddComponent<Button>();

        cardImage = GetComponent<Image>();
        if (cardImage == null)
            cardImage = gameObject.AddComponent<Image>();

        defaultBaseColor = cardImage.color;
        baseColor = defaultBaseColor;

        cardText = GetComponentInChildren<TextMeshProUGUI>();
        if (cardText == null)
            cardText = CreateCardText();

        cardButton.onClick.RemoveListener(OnCardClicked);
        cardButton.onClick.AddListener(OnCardClicked);

        ApplyCardSprite();
        UpdateDisplay();
    }

    private void ApplyCardSprite()
    {
        if (cardImage == null || card == null)
            return;

        Sprite spriteToUse = GetCardSprite(card);
        if (spriteToUse != null)
        {
            cardImage.sprite = spriteToUse;
            cardImage.color = Color.white;
            cardImage.type = Image.Type.Simple;
            if (hideTextWhenUsingTexture && cardText != null)
                cardText.enabled = false;
        }
        else if (defaultCardSprite != null)
        {
            cardImage.sprite = defaultCardSprite;
            cardImage.color = Color.white;
            cardImage.type = Image.Type.Simple;
            if (hideTextWhenUsingTexture && cardText != null)
                cardText.enabled = false;
        }
        else
        {
            cardImage.sprite = null;
            cardImage.color = baseColor;
            if (cardText != null)
                cardText.enabled = true;
        }
    }

    private Sprite GetCardSprite(Card card)
    {
        if (textureDatabase != null)
        {
            Sprite sprite = textureDatabase.GetSpriteForCard(card);
            if (sprite != null)
                return sprite;
        }

        return defaultCardSprite;
    }

    private void UpdateDisplay()
    {
        if (cardText == null || card == null)
            return;

        if (card.IsTeaCard())
        {
            cardText.text = $"Цай\n{card.displayName}";
        }
        else
        {
            cardText.text = $"{card.displayName}\n{card.value} {GetColorLabel(card.color)}";
        }
    }

    private void OnCardClicked()
    {
        if (card == null || owner == null || gameFlowController == null)
            return;

        if (owner.IsHuman)
        {
            gameFlowController.SelectCardForPlayer(card);
            Debug.Log($"Player selected {card}");
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (cardImage == null)
            return;

        this.isSelected = isSelected;
        cardImage.color = isSelected ? selectedColor : baseColor;
    }

    public void SetJanlii(bool isJanlii)
    {
        baseColor = isJanlii ? janliiColor : defaultBaseColor;
        if (!this.isSelected && cardImage != null)
            cardImage.color = baseColor;
    }

    public void SetTextureDatabase(CardTextureDatabase database)
    {
        textureDatabase = database;
        ApplyCardSprite();
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    private TextMeshProUGUI CreateCardText()
    {
        GameObject textObj = new GameObject("CardText");
        textObj.transform.SetParent(transform, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.08f, 0.06f, 0.04f, 1f);
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 26f;
        text.raycastTarget = false;

        return text;
    }

    private string GetColorLabel(DaaluuColor color)
    {
        return color == DaaluuColor.Red ? "R" : "W";
    }
}
