
using Godot;
using System;
using System.Collections.Generic;

// 電腦玩家類
public class ComputerPlayer
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public List<Card> Hand { get; set; } = new List<Card>();

    public ComputerPlayer(int id)
    {
        PlayerId = id;
        PlayerName = $"電腦玩家{id}";
    }

    // 電腦玩家的出牌邏輯
    public Card ChooseCardToPlay(Card topCard)
    {
        GD.Print($"電腦玩家 {PlayerName} 開始選擇要出的牌");
        GD.Print($"手牌數量: {Hand.Count}");
        GD.Print($"頂牌: {topCard?.Color} {topCard?.CardValue}");

        // 簡單的AI邏輯：找到第一張可以出的牌
        foreach (var card in Hand)
        {
            GD.Print($"檢查手牌: {card.Color} {card.CardValue}");
            if (card.CanPlayOn(topCard))
            {
                GD.Print($"找到可以出的牌: {card.Color} {card.CardValue}");
                return card;
            }
            else
            {
                GD.Print($"這張牌不能出: {card.Color} {card.CardValue}");
            }
        }
        GD.Print("沒有找到可以出的牌");
        return null; // 沒有可以出的牌
    }

    // 電腦玩家的顏色選擇邏輯（用於萬能牌）
    public CardColor ChooseColor()
    {
        // 簡單的AI邏輯：選擇手牌中最多的顏色
        var colorCounts = new Dictionary<CardColor, int>();
        colorCounts[CardColor.Red] = 0;
        colorCounts[CardColor.Blue] = 0;
        colorCounts[CardColor.Green] = 0;
        colorCounts[CardColor.Yellow] = 0;

        foreach (var card in Hand)
        {
            if (card.Type == CardType.Number || card.Type == CardType.Skip ||
                card.Type == CardType.Reverse || card.Type == CardType.DrawTwo)
            {
                colorCounts[card.Color]++;
            }
        }

        // 找到最多的顏色
        CardColor bestColor = CardColor.Red;
        int maxCount = 0;
        foreach (var kvp in colorCounts)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                bestColor = kvp.Key;
            }
        }

        return bestColor;
    }
}