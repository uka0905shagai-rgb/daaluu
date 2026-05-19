using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int playerID;
    public bool isHuman;
    public string playerName;

    // Property accessors
    public bool IsHuman => isHuman;

    // Game state
    private List<Card> hand = new List<Card>();
    private List<Card> collectedCards = new List<Card>();
    private DaaluuPieceName? declaredJanlii = null;
    private bool hasTeaCards = false;
    private int teaCardsCollected = 0;
    private int tricksWon = 0;

    // Current trick state
    private List<Card> currentTrick = new List<Card>();
    private bool canPlaySingleCard = false;

    public void Initialize(int id, bool isHumanPlayer, string name = "")
    {
        playerID = id;
        isHuman = isHumanPlayer;
        if (name != null && name.Length > 0)
            playerName = name;
        else
            playerName = isHuman ? "" : $"Bot_{id}";
        ResetPlayerState();
    }

    public void ResetPlayerState()
    {
        hand.Clear();
        collectedCards.Clear();
        declaredJanlii = null;
        hasTeaCards = false;
        teaCardsCollected = 0;
        tricksWon = 0;
        currentTrick.Clear();
        canPlaySingleCard = false;
    }

    public void ResetForNewGame(bool preserveTea)
    {
        hand.Clear();
        collectedCards.Clear();
        declaredJanlii = null;
        currentTrick.Clear();
        canPlaySingleCard = false;
        tricksWon = 0;

        if (!preserveTea)
        {
            hasTeaCards = false;
            teaCardsCollected = 0;
        }
        else
        {
            hasTeaCards = teaCardsCollected > 0;
        }
    }

    // Hand management
    public void AddCardToHand(Card card)
    {
        if (card == null) return;
        hand.Add(card);
    }

    public void AddCardsToHand(List<Card> cards)
    {
        foreach (Card card in cards)
        {
            AddCardToHand(card);
        }
    }

    public bool HasCard(Card card)
    {
        return hand.Contains(card);
    }

    public bool PlayCard(Card card)
    {
        if (!hand.Contains(card))
        {
            Debug.LogWarning($"{playerName} tried to play a card they don't have!");
            return false;
        }

        hand.Remove(card);
        currentTrick.Add(card);
        return true;
    }

    public List<Card> GetHand()
    {
        return new List<Card>(hand);
    }

    public List<Card> GetHandCopy()
    {
        return new List<Card>(hand);
    }

    public int GetHandSize()
    {
        return hand.Count;
    }

    // Trick and collection management
    public void CollectCards(List<Card> cards)
    {
        foreach (Card card in cards)
        {
            collectedCards.Add(card);
            if (card.IsTeaCard())
            {
                teaCardsCollected++;
            }
        }
        currentTrick.Clear();
        if (GameManager.Instance != null)
            GameManager.Instance.HandleTeaGain(this, 0);
    }

    public void AddTeaCards(int count)
    {
        teaCardsCollected += count;
        hasTeaCards = teaCardsCollected > 0;
        if (GameManager.Instance != null)
            GameManager.Instance.HandleTeaGain(this, count);
    }

    public void RemoveTeaCards(int count)
    {
        if (count <= 0)
            return;

        teaCardsCollected = Mathf.Max(0, teaCardsCollected - count);
        hasTeaCards = teaCardsCollected > 0;
    }

    public List<Card> GetCollectedCards()
    {
        return new List<Card>(collectedCards);
    }

    public int GetCollectedCardCount()
    {
        return collectedCards.Count;
    }

    public int GetTeaCardsCollected()
    {
        return teaCardsCollected;
    }

    public void AddTrickWin(int count = 1)
    {
        if (count <= 0)
            return;

        tricksWon += count;
    }

    public int GetTricksWon()
    {
        return tricksWon;
    }

    public bool HasAllTeaCards()
    {
        return teaCardsCollected == GameRules.TOTAL_TEA_CARDS;
    }

    // Rank declaration
    public void DeclareJanlii(DaaluuPieceName pieceName)
    {
        declaredJanlii = pieceName;
    }

    public DaaluuPieceName? GetDeclaredJanlii()
    {
        return declaredJanlii;
    }

    public bool HasDeclaredRank()
    {
        return declaredJanlii.HasValue;
    }

    // Card query methods
    public List<Card> GetCardsOfColor(DaaluuColor color)
    {
        List<Card> colorCards = new List<Card>();
        foreach (Card card in hand)
        {
            if (card.color == color)
                colorCards.Add(card);
        }
        return colorCards;
    }

    public List<Card> GetCardsOfPiece(DaaluuPieceName pieceName)
    {
        List<Card> matchingCards = new List<Card>();
        foreach (Card card in hand)
        {
            if (card.pieceName == pieceName)
                matchingCards.Add(card);
        }
        return matchingCards;
    }

    public List<Card> GetTeaCards()
    {
        List<Card> teaCards = new List<Card>();
        foreach (Card card in hand)
        {
            if (card.IsTeaCard())
                teaCards.Add(card);
        }
        return teaCards;
    }

    public bool HasTeaCardsInHand()
    {
        return GetTeaCards().Count > 0;
    }

    public int GetTotalEyeCount()
    {
        int total = 0;
        foreach (Card card in hand)
        {
            total += card.eyeCount;
        }
        return total;
    }

    public override string ToString()
    {
        return $"{playerName} (P{playerID}) - Hand: {hand.Count}, Collected: {collectedCards.Count}, Tea: {teaCardsCollected}/{GameRules.TOTAL_TEA_CARDS}, Tricks: {tricksWon}";
    }
}
