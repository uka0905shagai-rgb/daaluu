using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    private readonly List<Card> deck = new List<Card>();
    private readonly List<Card> teaPieces = new List<Card>();
    private int drawIndex = 0;

    public void InitializeDeck()
    {
        deck.Clear();
        teaPieces.Clear();
        drawIndex = 0;

        CreateTeaPieces();
        CreateHandPieces();
        ShuffleDeck();
    }

    private void CreateTeaPieces()
    {
        foreach (DaaluuPieceName pieceName in GameRules.TeaPieces)
        {
            teaPieces.Add(GameRules.CreatePiece(pieceName, CardType.Tea));
        }
    }

    private void CreateHandPieces()
    {
        foreach (DaaluuPieceName pieceName in GameRules.JanliiCandidates)
        {
            AddCopies(pieceName, 2);
        }

        AddCopies(DaaluuPieceName.Daaluu, 3);
        AddCopies(DaaluuPieceName.Uuluu, 3);
        AddCopies(DaaluuPieceName.Bajgar, 3);
        AddCopies(DaaluuPieceName.Arav, 3);
        AddCopies(DaaluuPieceName.Chavgants, 3);
        AddCopies(DaaluuPieceName.Shor, 3);
        AddCopies(DaaluuPieceName.Chans, 3);
        AddCopies(DaaluuPieceName.Buluu, 3);
        AddCopies(DaaluuPieceName.Oims, 3);
        AddCopies(DaaluuPieceName.Band, 3);
        AddCopies(DaaluuPieceName.Yoz, 4);
    }

    private void AddCopies(DaaluuPieceName pieceName, int count)
    {
        for (int i = 0; i < count; i++)
        {
            deck.Add(GameRules.CreatePiece(pieceName));
        }
    }

    public void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }

        drawIndex = 0;
    }

    public Card DrawCard()
    {
        if (drawIndex >= deck.Count)
        {
            Debug.LogWarning("No more Daaluu pieces in deck!");
            return null;
        }

        Card card = deck[drawIndex];
        drawIndex++;
        return card;
    }

    public List<Card> DrawCards(int count)
    {
        List<Card> cards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            Card card = DrawCard();
            if (card != null)
                cards.Add(card);
        }

        return cards;
    }

    public List<Card> GetTeaPieces()
    {
        return new List<Card>(teaPieces);
    }

    public int RemainingCards()
    {
        return deck.Count - drawIndex;
    }

    public void ResetDeck()
    {
        InitializeDeck();
    }
}
