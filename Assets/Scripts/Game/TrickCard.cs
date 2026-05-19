using UnityEngine;

/// <summary>
/// Represents a card played in a trick/round
/// </summary>
public class TrickCard
{
    public Card card;
    public Player playedBy;
    public int playOrder;

    public TrickCard(Card card, Player player, int order)
    {
        this.card = card;
        this.playedBy = player;
        this.playOrder = order;
    }
}
