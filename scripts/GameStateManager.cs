using System.Collections.Generic;
using Godot;

public enum GamePhase
{
    Initializing,
    Dealing,
    Playing,
    ColorSelection,
    GameOver
}

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

    // 動畫狀態
    public bool IsAnimating { get; set; } = false;

    // 手牌選擇狀態
    public Card SelectedCard { get; set; } = null;
    public int SelectedCardIndex { get; set; } = -1;

    // 手牌滾動狀態
    public int CurrentHandScrollIndex { get; set; } = 0;
    public int MaxVisibleCards { get; set; } = 8;

    // 訊息系統狀態
    public bool IsScrolling { get; set; } = false;

    // 遊戲邏輯相關變數
    // 遊戲方向（順時針/逆時針）
    public bool IsClockwiseDirection { get; set; } = true;

    // 遊戲階段
    public GamePhase CurrentGamePhase { get; set; } = GamePhase.Initializing;

    // 特殊牌效果狀態
    public bool IsWaitingForColorSelection { get; set; } = false;
    public bool IsDrawTwoActive { get; set; } = false;
    public bool IsDrawFourActive { get; set; } = false;

    // 遊戲統計數據
    public int TotalCardsPlayed { get; set; } = 0;
    public int TotalCardsDrawn { get; set; } = 0;
    public Dictionary<int, int> PlayerCardCounts { get; set; } = new Dictionary<int, int>();

    // 遊戲配置設定
    public int InitialCardsPerPlayer { get; set; } = 7;
    public bool EnableAnimations { get; set; } = true;
    public float AnimationSpeed { get; set; } = 1.0f;
}