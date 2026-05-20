using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Daaluu/Card Texture Database", fileName = "CardTextureDatabase")]
public class CardTextureDatabase : ScriptableObject
{
    [Serializable]
    public class CardSpriteEntry
    {
        public DaaluuPieceName pieceName;
        public UnityEngine.Object frontAsset;

        [NonSerialized] private Sprite cachedSprite;

        public Sprite GetSprite()
        {
            if (frontAsset is Sprite sprite)
                return sprite;

            if (frontAsset is Texture2D texture)
            {
                if (cachedSprite == null || cachedSprite.texture != texture)
                    cachedSprite = CreateSpriteFromTexture(texture);
                return cachedSprite;
            }

            return null;
        }
    }

    public UnityEngine.Object defaultFrontAsset;
    public UnityEngine.Object teaCardAsset;
    public List<CardSpriteEntry> cardSpriteEntries = new List<CardSpriteEntry>(16);

    public Sprite GetSpriteForCard(Card card)
    {
        if (card == null)
            return null;

        if (card.IsTeaCard())
        {
            Sprite teaSprite = GetSpriteFromAsset(teaCardAsset);
            if (teaSprite != null)
                return teaSprite;
        }

        if (cardSpriteEntries != null)
        {
            foreach (var entry in cardSpriteEntries)
            {
                if (entry != null && entry.pieceName == card.pieceName)
                {
                    Sprite sprite = entry.GetSprite();
                    if (sprite != null)
                        return sprite;
                }
            }
        }

        return GetSpriteFromAsset(defaultFrontAsset);
    }

    private Sprite GetSpriteFromAsset(UnityEngine.Object asset)
    {
        if (asset is Sprite sprite)
            return sprite;

        if (asset is Texture2D texture)
            return CreateSpriteFromTexture(texture);

        return null;
    }

    private static Sprite CreateSpriteFromTexture(Texture2D texture)
    {
        if (texture == null)
            return null;

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
