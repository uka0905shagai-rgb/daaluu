using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple AI for bot players - makes basic strategic decisions
/// </summary>
public class BotAI : MonoBehaviour
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public Difficulty difficulty = Difficulty.Medium;
    private Player botPlayer;
    private TrickManager trickManager;

    public void Initialize(Player player, TrickManager tricks, Difficulty diff = Difficulty.Medium)
    {
        botPlayer = player;
        trickManager = tricks;
        difficulty = diff;
    }

    public Card DecideCardToPlay(List<Card> validMoves)
    {
        if (validMoves.Count == 0)
        {
            Debug.LogWarning($"{botPlayer.playerName} has no valid moves!");
            return null;
        }

        List<Card> beatingMoves = GetBeatingMoves(validMoves);
        if (beatingMoves.Count > 0)
            validMoves = beatingMoves;

        List<Card> preferredMoves = FilterAvoidJanlii(validMoves);
        if (preferredMoves.Count == 0)
            preferredMoves = validMoves;

        if (preferredMoves.Count == 1)
            return preferredMoves[0];

        return difficulty switch
        {
            Difficulty.Easy => ChooseRandomCard(preferredMoves),
            Difficulty.Medium => ChooseMediumCard(preferredMoves),
            Difficulty.Hard => ChooseHardCard(preferredMoves),
            _ => ChooseRandomCard(preferredMoves)
        };
    }

    public List<Card> DecidePairToPlay(List<List<Card>> validPairs)
    {
        if (validPairs == null || validPairs.Count == 0)
            return null;

        List<List<Card>> beatingPairs = GetBeatingPairs(validPairs);
        if (beatingPairs.Count > 0)
            validPairs = beatingPairs;

        List<List<Card>> preferredPairs = FilterAvoidJanliiPairs(validPairs);
        if (preferredPairs.Count == 0)
            preferredPairs = validPairs;

        return preferredPairs[Random.Range(0, preferredPairs.Count)];
    }

    private List<Card> GetBeatingMoves(List<Card> moves)
    {
        List<Card> result = new List<Card>();
        if (trickManager == null)
            return result;

        DaaluuColor? leadColor = trickManager.GetLeadColor();
        DaaluuPieceName? janliiPiece = trickManager.GetJanliiPiece();
        Card winningCard = trickManager.GetCurrentWinningCard();

        foreach (Card card in moves)
        {
            if (GameRulesValidator.CanBeat(card, winningCard, leadColor ?? card.color, janliiPiece))
                result.Add(card);
        }

        return result;
    }

    private List<List<Card>> GetBeatingPairs(List<List<Card>> pairs)
    {
        List<List<Card>> result = new List<List<Card>>();
        if (trickManager == null)
            return result;

        foreach (List<Card> pair in pairs)
        {
            if (trickManager.CanBeatCurrentPlay(pair))
                result.Add(pair);
        }

        return result;
    }

    private List<Card> FilterAvoidJanlii(List<Card> moves)
    {
        List<Card> result = new List<Card>();
        DaaluuPieceName? janliiPiece = trickManager != null ? trickManager.GetJanliiPiece() : null;

        if (!janliiPiece.HasValue)
            return moves;

        foreach (Card card in moves)
        {
            if (card != null && card.pieceName != janliiPiece.Value)
                result.Add(card);
        }

        return result;
    }

    private List<List<Card>> FilterAvoidJanliiPairs(List<List<Card>> pairs)
    {
        List<List<Card>> result = new List<List<Card>>();
        DaaluuPieceName? janliiPiece = trickManager != null ? trickManager.GetJanliiPiece() : null;

        if (!janliiPiece.HasValue)
            return pairs;

        foreach (List<Card> pair in pairs)
        {
            if (pair.Count == 0)
                continue;

            if (pair[0].pieceName != janliiPiece.Value)
                result.Add(pair);
        }

        return result;
    }

    private Card ChooseRandomCard(List<Card> validMoves)
    {
        return validMoves[Random.Range(0, validMoves.Count)];
    }

    private Card ChooseMediumCard(List<Card> validMoves)
    {
        // Medium AI: Try to win if possible, otherwise discard lowest value card
        List<Card> teaCards = new List<Card>();
        List<Card> otherCards = new List<Card>();

        foreach (Card card in validMoves)
        {
            if (card.IsTeaCard())
                teaCards.Add(card);
            else
                otherCards.Add(card);
        }

        // If we have tea cards, play one
        if (teaCards.Count > 0)
            return teaCards[0];

        // Otherwise play lowest value card (to save high cards)
        Card lowestCard = otherCards[0];
        foreach (Card card in otherCards)
        {
            if (card.value < lowestCard.value)
                lowestCard = card;
        }

        return lowestCard;
    }

    private Card ChooseHardCard(List<Card> validMoves)
    {
        // Hard AI: More sophisticated strategy
        List<Card> teaCards = new List<Card>();
        List<Card> winningCards = new List<Card>();
        List<Card> otherCards = new List<Card>();

        DaaluuColor? leadColor = trickManager.GetLeadColor();

        foreach (Card card in validMoves)
        {
            if (card.IsTeaCard())
            {
                teaCards.Add(card);
            }
            else if (leadColor.HasValue && card.color == leadColor.Value)
            {
                winningCards.Add(card);
            }
            else
            {
                otherCards.Add(card);
            }
        }

        // If we have tea cards and need them, play one
        if (teaCards.Count > 0 && botPlayer.GetTeaCardsCollected() < GameRules.TOTAL_TEA_CARDS - 1)
            return teaCards[0];

        // If we can win the trick with lead suit, play high card
        if (winningCards.Count > 0)
        {
            // Check if it's worth winning
            List<TrickCard> trickCards = trickManager.GetTrickCards();
            if (trickCards.Count > 0 && ContainsValueableCard(trickCards))
            {
                // Play highest to win
                Card highest = winningCards[0];
                foreach (Card card in winningCards)
                {
                    if (card.value > highest.value)
                        highest = card;
                }
                return highest;
            }
        }

        // Otherwise discard lowest card
        Card lowestCard = validMoves[0];
        foreach (Card card in validMoves)
        {
            if (card.value < lowestCard.value)
                lowestCard = card;
        }

        return lowestCard;
    }

    private bool ContainsValueableCard(List<TrickCard> trickCards)
    {
        foreach (TrickCard tc in trickCards)
        {
            if (tc.card.IsTeaCard() || tc.card.value >= 10)
                return true;
        }
        return false;
    }

    public DaaluuPieceName DeclareBestJanlii()
    {
        DaaluuPieceName[] candidates = GameRules.JanliiCandidates;
        if (candidates == null || candidates.Length == 0)
            return DaaluuPieceName.Ys;

        return candidates[Random.Range(0, candidates.Length)];
    }
}
