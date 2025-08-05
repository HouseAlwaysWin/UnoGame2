using System.Collections.Generic;
using Godot;

public partial class GameStateManager : Node
{
// 遊戲狀態
    public List<Card> DrawPile = new List<Card>(); // 抽牌堆
    public List<Card> DiscardPile = new List<Card>(); // 棄牌堆
    public List<Card> PlayerHand = new List<Card>(); // 玩家手牌
    public Card CurrentTopCard; // 當前頂牌

    // 玩家管理
    public int PlayerCount { get; set; } = 4; // 遊玩人數，預設4人
    public List<ComputerPlayer> ComputerPlayers = new List<ComputerPlayer>(); // 電腦玩家列表
    public int CurrentPlayerIndex = 0; // 當前玩家索引（0為人類玩家）
}