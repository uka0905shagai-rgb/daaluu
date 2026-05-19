using System.Collections.Generic;
using UnityEngine;

public static class GameRules
{
    public const int TOTAL_PLAYERS = 5;
    public const int TOTAL_CARDS = 60;
    public const int TOTAL_TEA_CARDS = 10;
    public const int REGULAR_CARDS = 50;
    public const int STARTING_HAND_SIZE = 10;
    public const int TEA_CARDS_PER_PLAYER = 2;
    public const int MINIMUM_SINGLE_CARD_EYES = 8;

    public static readonly DaaluuPieceName[] TeaPieces =
    {
        DaaluuPieceName.Daaluu,
        DaaluuPieceName.Uuluu,
        DaaluuPieceName.Arav,
        DaaluuPieceName.Bajgar,
        DaaluuPieceName.Chavgants,
        DaaluuPieceName.Shor,
        DaaluuPieceName.Chans,
        DaaluuPieceName.Buluu,
        DaaluuPieceName.Oims,
        DaaluuPieceName.Band
    };

    public static readonly DaaluuPieceName[] JanliiCandidates =
    {
        DaaluuPieceName.Ys,
        DaaluuPieceName.Degee,
        DaaluuPieceName.Dungu,
        DaaluuPieceName.Murui,
        DaaluuPieceName.Shanaga,
        DaaluuPieceName.Sarlag,
        DaaluuPieceName.Nohoi,
        DaaluuPieceName.Chuu
    };

    public static bool IsTeaPiece(DaaluuPieceName pieceName)
    {
        foreach (DaaluuPieceName teaPiece in TeaPieces)
        {
            if (teaPiece == pieceName)
                return true;
        }

        return false;
    }

    public static bool IsJanliiCandidate(DaaluuPieceName pieceName)
    {
        foreach (DaaluuPieceName candidate in JanliiCandidates)
        {
            if (candidate == pieceName)
                return true;
        }

        return false;
    }

    public static Card CreatePiece(DaaluuPieceName pieceName, CardType cardType = CardType.Regular)
    {
        PieceInfo info = GetPieceInfo(pieceName);
        return new Card(pieceName, info.Color, info.Value, cardType, info.Description);
    }

    public static PieceInfo GetPieceInfo(DaaluuPieceName pieceName)
    {
        return pieceName switch
        {
            DaaluuPieceName.Daaluu => new PieceInfo(12, DaaluuColor.Red, "Даалуу"),
            DaaluuPieceName.Uuluu => new PieceInfo(11, DaaluuColor.White, "Үүлүү, Хуутуу"),
            DaaluuPieceName.Bajgar => new PieceInfo(10, DaaluuColor.White, "Бажгар, Хар арав"),
            DaaluuPieceName.Arav => new PieceInfo(10, DaaluuColor.Red, "Улаан арав, Сийлүү"),
            DaaluuPieceName.Ys => new PieceInfo(9, DaaluuColor.Red, "Гавал ес, Улаан ес, Данх ес"),
            DaaluuPieceName.Degee => new PieceInfo(9, DaaluuColor.White, "Дэгээ ес, Тахир ес, Цагаан ес"),
            DaaluuPieceName.Chavgants => new PieceInfo(8, DaaluuColor.Red, "Чавганц, Хуньд найм"),
            DaaluuPieceName.Dungu => new PieceInfo(8, DaaluuColor.White, "Дөнгө найм"),
            DaaluuPieceName.Murui => new PieceInfo(8, DaaluuColor.White, "Муруй найм, Савар, Тэмээ"),
            DaaluuPieceName.Shor => new PieceInfo(7, DaaluuColor.Red, "Шор долоо"),
            DaaluuPieceName.Shanaga => new PieceInfo(7, DaaluuColor.Red, "Шанага долоо"),
            DaaluuPieceName.Sarlag => new PieceInfo(7, DaaluuColor.White, "Сарлаг долоо"),
            DaaluuPieceName.Chans => new PieceInfo(6, DaaluuColor.White, "Чанс зургаа"),
            DaaluuPieceName.Buluu => new PieceInfo(6, DaaluuColor.Red, "Булуу зургаа"),
            DaaluuPieceName.Nohoi => new PieceInfo(6, DaaluuColor.White, "Нохой зургаа"),
            DaaluuPieceName.Chuu => new PieceInfo(5, DaaluuColor.White, "Чүү тав"),
            DaaluuPieceName.Oims => new PieceInfo(4, DaaluuColor.Red, "Оймс"),
            DaaluuPieceName.Band => new PieceInfo(4, DaaluuColor.White, "Ванд, Банд"),
            DaaluuPieceName.Yoz => new PieceInfo(2, DaaluuColor.Red, "Ёоз"),
            _ => new PieceInfo(0, DaaluuColor.Red, "?")
        };
    }

    public static string GetPieceDisplayName(DaaluuPieceName pieceName)
    {
        return pieceName switch
        {
            DaaluuPieceName.Daaluu => "Даалуу",
            DaaluuPieceName.Uuluu => "Үүлүү",
            DaaluuPieceName.Bajgar => "Бажгар",
            DaaluuPieceName.Arav => "Арав",
            DaaluuPieceName.Ys => "Ес",
            DaaluuPieceName.Degee => "Дэгээ",
            DaaluuPieceName.Chavgants => "Чавганц",
            DaaluuPieceName.Dungu => "Дөнгө",
            DaaluuPieceName.Murui => "Муруй",
            DaaluuPieceName.Shor => "Шор",
            DaaluuPieceName.Shanaga => "Шанага",
            DaaluuPieceName.Sarlag => "Сарлаг",
            DaaluuPieceName.Chans => "Чанс",
            DaaluuPieceName.Buluu => "Булуу",
            DaaluuPieceName.Nohoi => "Нохой",
            DaaluuPieceName.Chuu => "Чүү",
            DaaluuPieceName.Oims => "Оймс",
            DaaluuPieceName.Band => "Банд",
            DaaluuPieceName.Yoz => "Ёоз",
            _ => "?"
        };
    }

    public static string GetColorLabel(DaaluuColor color)
    {
        return color == DaaluuColor.Red ? "Улаан" : "Цагаан";
    }

    public readonly struct PieceInfo
    {
        public readonly int Value;
        public readonly DaaluuColor Color;
        public readonly string Description;

        public PieceInfo(int value, DaaluuColor color, string description)
        {
            Value = value;
            Color = color;
            Description = description;
        }
    }
}
