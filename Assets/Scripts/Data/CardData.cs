using System;
using UnityEngine;

public enum DaaluuPieceName
{
    Daaluu,
    Uuluu,
    Bajgar,
    Arav,
    Ys,
    Degee,
    Chavgants,
    Dungu,
    Murui,
    Shor,
    Shanaga,
    Sarlag,
    Chans,
    Buluu,
    Nohoi,
    Chuu,
    Oims,
    Band,
    Yoz
}

public enum DaaluuColor
{
    Red,
    White
}

public enum CardType
{
    Regular,
    Tea
}

[Serializable]
public class Card
{
    public DaaluuPieceName pieceName;
    public DaaluuColor color;
    public CardType cardType;
    public int value;
    public int eyeCount;
    public string displayName;
    public string description;

    public Card(DaaluuPieceName pieceName, DaaluuColor color, int value, CardType cardType = CardType.Regular, string description = "")
    {
        this.pieceName = pieceName;
        this.color = color;
        this.value = value;
        this.eyeCount = value;
        this.cardType = cardType;
        this.displayName = GameRules.GetPieceDisplayName(pieceName);
        this.description = description;
    }

    public override string ToString()
    {
        string typeLabel = IsTeaCard() ? "Tea" : "Hand";
        return $"{displayName} ({value}, {color}, {typeLabel})";
    }

    public string GetSimpleString()
    {
        return $"{displayName} {value}";
    }

    public bool IsTeaCard()
    {
        return cardType == CardType.Tea;
    }

    public bool IsJanliiCandidate()
    {
        return GameRules.IsJanliiCandidate(pieceName);
    }
}
