using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles Daaluu move validation.
/// </summary>
public class GameRulesValidator
{
    public static bool CanPlaySingleCard(Card card, DaaluuPieceName? janliiPiece)
    {
        if (card == null)
            return false;

        return IsJanlii(card, janliiPiece) || card.eyeCount >= GameRules.MINIMUM_SINGLE_CARD_EYES;
    }

    public static bool IsValidCardForRoundStart(Card card, DaaluuPieceName? janliiPiece)
    {
        return CanPlaySingleCard(card, janliiPiece);
    }

    public static bool IsValidPairForRoundStart(List<Card> cards, DaaluuPieceName? janliiPiece)
    {
        if (cards == null || cards.Count != 2)
            return false;

        if (cards[0].pieceName != cards[1].pieceName)
            return false;

        return true;
    }

    public static List<Card> GetValidMoves(Player player, TrickManager trickManager, bool isRoundStart)
    {
        List<Card> hand = player.GetHandCopy();
        List<Card> validMoves = new List<Card>();

        if (isRoundStart)
        {
            foreach (Card card in hand)
            {
                if (IsValidCardForRoundStart(card, trickManager.GetJanliiPiece()))
                    validMoves.Add(card);
            }

            if (validMoves.Count == 0)
                validMoves.AddRange(hand);

            return validMoves;
        }

        DaaluuColor? leadColor = trickManager.GetLeadColor();
        DaaluuPieceName? janliiPiece = trickManager.GetJanliiPiece();
        Card winningCard = trickManager.GetCurrentWinningCard();

        if (!leadColor.HasValue)
            return hand;

        List<Card> sameColorCards = player.GetCardsOfColor(leadColor.Value);
        List<Card> beatingCards = new List<Card>();

        foreach (Card card in sameColorCards)
        {
            if (CanBeat(card, winningCard, leadColor.Value, janliiPiece))
                beatingCards.Add(card);
        }

        foreach (Card card in hand)
        {
            if (!sameColorCards.Contains(card) && CanBeat(card, winningCard, leadColor.Value, janliiPiece))
                beatingCards.Add(card);
        }

        if (beatingCards.Count > 0)
            return beatingCards;

        validMoves.AddRange(hand);
        return validMoves;
    }

    public static List<List<Card>> GetValidPairMoves(Player player, TrickManager trickManager, bool isRoundStart)
    {
        List<List<Card>> validPairs = new List<List<Card>>();
        List<Card> hand = player.GetHandCopy();

        Dictionary<DaaluuPieceName, List<Card>> grouped = new Dictionary<DaaluuPieceName, List<Card>>();
        foreach (Card card in hand)
        {
            if (!grouped.ContainsKey(card.pieceName))
                grouped[card.pieceName] = new List<Card>();

            grouped[card.pieceName].Add(card);
        }

        foreach (var kvp in grouped)
        {
            if (kvp.Value.Count >= 2)
                validPairs.Add(new List<Card> { kvp.Value[0], kvp.Value[1] });
        }

        if (isRoundStart || trickManager == null)
            return validPairs;

        DaaluuColor? leadColor = trickManager.GetLeadColor();
        if (!leadColor.HasValue)
            return validPairs;

        List<List<Card>> matchingColorPairs = new List<List<Card>>();
        foreach (List<Card> pair in validPairs)
        {
            if (pair.Count == 2 && pair[0].color == leadColor.Value)
                matchingColorPairs.Add(pair);
        }

        // Janlii pairs can always be played, even if the lead color is different.
        DaaluuPieceName? janliiPiece = trickManager.GetJanliiPiece();
        if (janliiPiece.HasValue)
        {
            foreach (List<Card> pair in validPairs)
            {
                if (pair.Count == 2 && pair[0].pieceName == janliiPiece.Value)
                {
                    if (!matchingColorPairs.Contains(pair))
                        matchingColorPairs.Add(pair);
                }
            }
        }

        if (matchingColorPairs.Count > 0)
            return matchingColorPairs;

        if (trickManager.GetLeadPlaySize() == 2)
        {
            List<List<Card>> anyTwo = new List<List<Card>>();
            for (int i = 0; i < hand.Count - 1; i++)
            {
                for (int j = i + 1; j < hand.Count; j++)
                {
                    anyTwo.Add(new List<Card> { hand[i], hand[j] });
                }
            }

            return anyTwo;
        }

        return validPairs;
    }

    public static bool CanBeat(Card challenger, Card currentWinner, DaaluuColor leadColor, DaaluuPieceName? janliiPiece)
    {
        if (challenger == null)
            return false;

        if (currentWinner == null)
            return true;

        bool challengerIsJanlii = IsJanlii(challenger, janliiPiece);
        bool winnerIsJanlii = IsJanlii(currentWinner, janliiPiece);

        if (challengerIsJanlii)
        {
            if (!winnerIsJanlii)
                return true;

            return challenger.value > currentWinner.value;
        }

        if (winnerIsJanlii)
            return false;

        bool challengerIsLead = challenger.color == leadColor;
        bool winnerIsLead = currentWinner.color == leadColor;

        if (challengerIsLead && !winnerIsLead)
            return true;

        if (!challengerIsLead)
            return false;

        return challenger.value > currentWinner.value;
    }

    public static bool IsJanlii(Card card, DaaluuPieceName? janliiPiece)
    {
        return card != null && janliiPiece.HasValue && card.pieceName == janliiPiece.Value;
    }
}

/// <summary>
/// Manages a Daaluu trick / гэр.
/// </summary>
public class TrickManager : MonoBehaviour
{
    private readonly List<TrickCard> trickCards = new List<TrickCard>();
    private DaaluuColor? leadColor = null;
    private DaaluuPieceName? janliiPiece = null;
    private Player roundStarter = null;
    private int playOrder = 0;
    private int playCount = 0;
    private int leadPlaySize = 0;

    public void SetJanliiPiece(DaaluuPieceName pieceName)
    {
        janliiPiece = pieceName;
        Debug.Log($"Janlii named: {GameRules.GetPieceDisplayName(pieceName)}");
    }

    public DaaluuPieceName? GetJanliiPiece()
    {
        return janliiPiece;
    }

    public void StartNewTrick(Player starter)
    {
        trickCards.Clear();
        leadColor = null;
        roundStarter = starter;
        playOrder = 0;
        playCount = 0;
        leadPlaySize = 0;
    }

    public void PlayCard(Card card, Player player)
    {
        if (card == null || player == null)
        {
            Debug.LogWarning("Attempted to play null Daaluu piece or null player!");
            return;
        }

        PlayCards(new List<Card> { card }, player);
    }

    public void PlayCards(List<Card> cards, Player player)
    {
        if (cards == null || cards.Count == 0 || player == null)
        {
            Debug.LogWarning("Attempted to play empty Daaluu pieces or null player!");
            return;
        }

        if (leadPlaySize == 0)
            leadPlaySize = cards.Count;

        foreach (Card card in cards)
        {
            trickCards.Add(new TrickCard(card, player, playOrder));
        }

        if (playCount == 0 && cards.Count > 0)
            leadColor = cards[0].color;

        playOrder++;
        playCount++;
    }

    public List<TrickCard> GetTrickCards()
    {
        return new List<TrickCard>(trickCards);
    }

    public Dictionary<int, List<TrickCard>> GetPlaysByOrder()
    {
        Dictionary<int, List<TrickCard>> plays = new Dictionary<int, List<TrickCard>>();
        foreach (TrickCard trickCard in trickCards)
        {
            if (!plays.ContainsKey(trickCard.playOrder))
                plays[trickCard.playOrder] = new List<TrickCard>();

            plays[trickCard.playOrder].Add(trickCard);
        }

        return plays;
    }

    public int ComparePlays(List<TrickCard> firstPlay, List<TrickCard> secondPlay)
    {
        if (firstPlay == null && secondPlay == null)
            return 0;
        if (firstPlay == null)
            return -1;
        if (secondPlay == null)
            return 1;

        DaaluuColor activeLeadColor = leadColor ?? (trickCards.Count > 0 ? trickCards[0].card.color : DaaluuColor.Red);

        if (leadPlaySize == 2)
        {
            if (PairBeats(firstPlay, secondPlay, activeLeadColor, janliiPiece))
                return 1;
            if (PairBeats(secondPlay, firstPlay, activeLeadColor, janliiPiece))
                return -1;
            return 0;
        }

        Card firstCard = firstPlay.Count > 0 ? firstPlay[0].card : null;
        Card secondCard = secondPlay.Count > 0 ? secondPlay[0].card : null;

        if (GameRulesValidator.CanBeat(firstCard, secondCard, activeLeadColor, janliiPiece))
            return 1;
        if (GameRulesValidator.CanBeat(secondCard, firstCard, activeLeadColor, janliiPiece))
            return -1;
        return 0;
    }

    public List<TrickCard> GetCurrentWinningPlay()
    {
        Dictionary<int, List<TrickCard>> plays = GetPlaysByOrder();
        if (plays.Count == 0)
            return new List<TrickCard>();

        List<TrickCard> winningPlay = null;
        foreach (List<TrickCard> play in plays.Values)
        {
            if (play == null || play.Count == 0)
                continue;

            if (winningPlay == null)
            {
                winningPlay = play;
                continue;
            }

            if (ComparePlays(play, winningPlay) > 0)
                winningPlay = play;
        }

        return winningPlay ?? new List<TrickCard>();
    }

    public bool CanBeatCurrentPlay(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return false;

        if (playCount == 0)
            return true;

        DaaluuColor activeLeadColor = leadColor ?? cards[0].color;

        if (leadPlaySize == 2)
        {
            List<TrickCard> winningPlay = GetCurrentWinningPlay();
            List<TrickCard> challengerPlay = BuildPlayFromCards(cards);
            return PairBeats(challengerPlay, winningPlay, activeLeadColor, janliiPiece);
        }

        return GameRulesValidator.CanBeat(cards[0], GetCurrentWinningCard(), activeLeadColor, janliiPiece);
    }

    public DaaluuColor? GetLeadColor()
    {
        return leadColor;
    }

    public Card GetCurrentWinningCard()
    {
        TrickCard winner = GetWinningTrickCard();
        return winner?.card;
    }

    public int GetTrickCardCount()
    {
        return trickCards.Count;
    }

    public int GetTrickPlayCount()
    {
        return playCount;
    }

    public int GetLeadPlaySize()
    {
        return leadPlaySize;
    }

    public Player GetRoundStarter()
    {
        return roundStarter;
    }

    public List<Card> ResolveTrick()
    {
        List<Card> collectedCards = new List<Card>();
        foreach (TrickCard tc in trickCards)
        {
            collectedCards.Add(tc.card);
        }

        return collectedCards;
    }

    public Player CalculateTrickWinner()
    {
        TrickCard winner = GetWinningTrickCard();
        return winner != null ? winner.playedBy : null;
    }

    public bool CanPlayCard(Card card, Player player)
    {
        if (trickCards.Count == 0)
            return GameRulesValidator.IsValidCardForRoundStart(card, janliiPiece);

        List<Card> validMoves = GameRulesValidator.GetValidMoves(player, this, false);
        return validMoves.Contains(card);
    }

    private TrickCard GetWinningTrickCard()
    {
        if (trickCards.Count == 0)
            return null;

        if (leadPlaySize == 2)
            return GetWinningPairTrickCard();

        DaaluuColor activeLeadColor = leadColor ?? trickCards[0].card.color;
        TrickCard winner = trickCards[0];

        foreach (TrickCard trickCard in trickCards)
        {
            if (GameRulesValidator.CanBeat(trickCard.card, winner.card, activeLeadColor, janliiPiece))
                winner = trickCard;
        }

        return winner;
    }

    private TrickCard GetWinningPairTrickCard()
    {
        Dictionary<int, List<TrickCard>> plays = GetPlaysByOrder();
        DaaluuColor activeLeadColor = leadColor ?? trickCards[0].card.color;
        TrickCard winner = null;
        List<TrickCard> winnerPlay = null;

        foreach (var kvp in plays)
        {
            List<TrickCard> playCards = kvp.Value;
            if (playCards.Count == 0)
                continue;

            if (!IsValidPairPlay(playCards))
                continue;

            if (winner == null)
            {
                winner = playCards[0];
                winnerPlay = playCards;
                continue;
            }

            if (PairBeats(playCards, winnerPlay, activeLeadColor, janliiPiece))
            {
                winner = playCards[0];
                winnerPlay = playCards;
            }
        }

        return winner ?? trickCards[0];
    }

    private bool IsValidPairPlay(List<TrickCard> playCards)
    {
        if (playCards == null || playCards.Count != 2)
            return false;

        return playCards[0].card.pieceName == playCards[1].card.pieceName;
    }

    private bool PairBeats(List<TrickCard> challengerPlay, List<TrickCard> winnerPlay, DaaluuColor lead, DaaluuPieceName? janlii)
    {
        if (challengerPlay == null || winnerPlay == null)
            return false;

        if (!IsValidPairPlay(challengerPlay))
            return false;

        if (!IsValidPairPlay(winnerPlay))
            return true;

        Card challengerCard = challengerPlay[0].card;
        Card winnerCard = winnerPlay[0].card;

        bool challengerIsJanlii = GameRulesValidator.IsJanlii(challengerCard, janlii);
        bool winnerIsJanlii = GameRulesValidator.IsJanlii(winnerCard, janlii);

        if (challengerIsJanlii)
        {
            if (!winnerIsJanlii)
                return true;

            return challengerCard.value > winnerCard.value;
        }

        if (winnerIsJanlii)
            return false;

        bool challengerIsLead = challengerCard.color == lead;
        bool winnerIsLead = winnerCard.color == lead;

        if (challengerIsLead && !winnerIsLead)
            return true;

        if (!challengerIsLead)
            return false;

        return challengerCard.value > winnerCard.value;
    }

    private List<TrickCard> BuildPlayFromCards(List<Card> cards)
    {
        List<TrickCard> play = new List<TrickCard>();
        if (cards == null)
            return play;

        foreach (Card card in cards)
        {
            if (card != null)
                play.Add(new TrickCard(card, null, 0));
        }

        return play;
    }
}
