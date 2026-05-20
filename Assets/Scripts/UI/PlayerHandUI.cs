using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Displays a player's hand and handles card selection
/// </summary>
public class PlayerHandUI : MonoBehaviour
{
    private Player player;
    private GameFlowController gameFlowController;
    private Transform cardContainer;
    private GameObject cardUIPrefab;
    private CardTextureDatabase cardTextureDatabase;
    private Dictionary<Card, CardUI> cardUIs = new Dictionary<Card, CardUI>();
    private DaaluuPieceName? janliiPiece = null;

    public void Initialize(Player playerRef, GameObject cardPrefab, GameFlowController flowController)
    {
        player = playerRef;
        cardUIPrefab = cardPrefab;
        gameFlowController = flowController;

        // Create container for cards if needed
        cardContainer = transform.Find("Cards");
        if (cardContainer == null)
        {
            GameObject containerObj = new GameObject("Cards");
            containerObj.transform.SetParent(transform, false);
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(900f, 110f);

            HorizontalLayoutGroup layoutGroup = containerObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 10f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            cardContainer = containerObj.transform;
        }
    }

    public void SetCardTextureDatabase(CardTextureDatabase database)
    {
        cardTextureDatabase = database;
    }

    public void RefreshHandDisplay()
    {
        if (player == null) return;

        List<Card> selectedCards = new List<Card>();
        foreach (var kvp in cardUIs)
        {
            if (kvp.Value != null && kvp.Value.IsSelected())
                selectedCards.Add(kvp.Key);
        }

        // Clear old card UIs
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
        cardUIs.Clear();

        // Create new card UIs
        List<Card> hand = player.GetHandCopy();
        hand.Sort(CompareCards);
        for (int i = 0; i < hand.Count; i++)
        {
            Card card = hand[i];
            GameObject cardObj = Instantiate(cardUIPrefab, cardContainer);
            cardObj.SetActive(true);

            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.Initialize(card, player, gameFlowController);
                if (cardTextureDatabase != null)
                    cardUI.SetTextureDatabase(cardTextureDatabase);
                cardUI.SetJanlii(janliiPiece.HasValue && card.pieceName == janliiPiece.Value);
                cardUIs[card] = cardUI;
            }

            // Layout cards in a row
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(70f, 96f);
            }
        }

        if (selectedCards.Count > 0)
            SetSelectedCards(selectedCards);
    }

    private int CompareCards(Card left, Card right)
    {
        if (left == null && right == null)
            return 0;
        if (left == null)
            return 1;
        if (right == null)
            return -1;

        bool leftIsJanlii = janliiPiece.HasValue && left.pieceName == janliiPiece.Value;
        bool rightIsJanlii = janliiPiece.HasValue && right.pieceName == janliiPiece.Value;
        if (leftIsJanlii != rightIsJanlii)
            return rightIsJanlii.CompareTo(leftIsJanlii);

        int valueCompare = right.value.CompareTo(left.value);
        if (valueCompare != 0)
            return valueCompare;

        int pieceCompare = left.pieceName.CompareTo(right.pieceName);
        if (pieceCompare != 0)
            return pieceCompare;

        return left.color.CompareTo(right.color);
    }

    public void SetJanliiPiece(DaaluuPieceName? pieceName)
    {
        janliiPiece = pieceName;
    }

    public void SetSelectedCards(List<Card> selectedCards)
    {
        if (cardUIs.Count == 0)
            return;

        foreach (var kvp in cardUIs)
        {
            bool isSelected = selectedCards != null && selectedCards.Contains(kvp.Key);
            kvp.Value.SetSelected(isSelected);
        }
    }
}
