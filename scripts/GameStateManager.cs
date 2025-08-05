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
    // 遊戲事件信號
    [Signal]
    public delegate void GameStateChangedEventHandler();

    [Signal]
    public delegate void PlayerTurnChangedEventHandler(int playerIndex);

    [Signal]
    public delegate void CardPlayedEventHandler(Card card, int playerIndex);

    [Signal]
    public delegate void CardDrawnEventHandler(Card card, int playerIndex);

    [Signal]
    public delegate void GamePhaseChangedEventHandler(int newPhase);

    [Signal]
    public delegate void SpecialCardEffectEventHandler(Card card, int effectType);

    [Signal]
    public delegate void GameOverEventHandler(int winnerPlayerIndex);

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

    // 事件觸發方法
    private void EmitGameStateChanged()
    {
        EmitSignal(nameof(GameStateChanged));
    }

    private void EmitPlayerTurnChanged(int playerIndex)
    {
        EmitSignal(nameof(PlayerTurnChanged), playerIndex);
    }

    private void EmitCardPlayed(Card card, int playerIndex)
    {
        // 創建Card的副本以避免對象釋放問題
        var cardInfo = new Card();
        cardInfo.SetCard(card.Color, card.CardValue, card.Type);
        EmitSignal(nameof(CardPlayed), cardInfo, playerIndex);
    }

    private void EmitCardDrawn(Card card, int playerIndex)
    {
        // 創建Card的副本以避免對象釋放問題
        var cardInfo = new Card();
        cardInfo.SetCard(card.Color, card.CardValue, card.Type);
        EmitSignal(nameof(CardDrawn), cardInfo, playerIndex);
    }

    private void EmitGamePhaseChanged(GamePhase newPhase)
    {
        EmitSignal(nameof(GamePhaseChanged), (int)newPhase);
    }

    private void EmitSpecialCardEffect(Card card, CardType effectType)
    {
        // 創建Card的副本以避免對象釋放問題
        var cardInfo = new Card();
        cardInfo.SetCard(card.Color, card.CardValue, card.Type);
        EmitSignal(nameof(SpecialCardEffect), cardInfo, (int)effectType);
    }

    private void EmitGameOver(int winnerPlayerIndex)
    {
        EmitSignal(nameof(GameOver), winnerPlayerIndex);
    }

    // 玩家管理方法
    public void NextPlayer()
    {
        // 輪換到下一個玩家
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerCount;
        GD.Print($"輪換到下一個玩家: {GetCurrentPlayerName()}");
        GD.Print($"當前玩家索引: {CurrentPlayerIndex}");
        
        // 觸發玩家回合改變事件
        EmitPlayerTurnChanged(CurrentPlayerIndex);
        EmitGameStateChanged();
    }

    public void NextPlayerWithoutComputerTurn()
    {
        // 輪換到下一個玩家，但不執行電腦玩家回合
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerCount;
        GD.Print($"輪換到下一個玩家: {GetCurrentPlayerName()}");
        
        // 觸發玩家回合改變事件
        EmitPlayerTurnChanged(CurrentPlayerIndex);
        EmitGameStateChanged();
    }

    public string GetCurrentPlayerName()
    {
        if (CurrentPlayerIndex == 0)
        {
            return "玩家1 (你)";
        }
        else
        {
            int computerPlayerIndex = CurrentPlayerIndex - 1;
            if (computerPlayerIndex < ComputerPlayers.Count)
            {
                return ComputerPlayers[computerPlayerIndex].PlayerName;
            }
            else
            {
                return $"玩家{CurrentPlayerIndex + 1}";
            }
        }
    }

    public string GetAllPlayersCardsInfo()
    {
        var info = new List<string>();

        // 人類玩家手牌數量
        info.Add($"你: {PlayerHand.Count}張");

        // 電腦玩家手牌數量
        for (int i = 0; i < ComputerPlayers.Count; i++)
        {
            var computerPlayer = ComputerPlayers[i];
            info.Add($"{computerPlayer.PlayerName}: {computerPlayer.Hand.Count}張");
        }

        return string.Join(" | ", info);
    }

    // 遊戲邏輯方法
    public void HandleSpecialCardEffect(Card card)
    {
        switch (card.Type)
        {
            case CardType.Skip:
                GD.Print("跳過下一個玩家的回合");
                EmitSpecialCardEffect(card, CardType.Skip);
                NextPlayer(); // 跳過一個玩家
                break;
            case CardType.Reverse:
                GD.Print("遊戲方向改變");
                // 反轉遊戲方向
                IsClockwiseDirection = !IsClockwiseDirection;
                EmitSpecialCardEffect(card, CardType.Reverse);
                // 反轉牌需要輪換到下一個玩家
                NextPlayer();
                break;
            case CardType.DrawTwo:
                GD.Print("下一個玩家抽兩張牌");
                // 設置抽兩張牌狀態
                IsDrawTwoActive = true;
                EmitSpecialCardEffect(card, CardType.DrawTwo);
                // 讓下一個玩家抽兩張牌
                NextPlayerWithoutComputerTurn();
                DrawTwoCardsForCurrentPlayer();
                // 重置抽兩張牌狀態
                IsDrawTwoActive = false;
                // 抽牌後再輪換到下一個玩家
                NextPlayer();
                break;
            case CardType.Wild:
                GD.Print("萬能牌，輪換到下一個玩家");
                EmitSpecialCardEffect(card, CardType.Wild);
                NextPlayer();
                break;
            case CardType.WildDrawFour:
                GD.Print("下一個玩家抽四張牌");
                // 設置抽四張牌狀態
                IsDrawFourActive = true;
                EmitSpecialCardEffect(card, CardType.WildDrawFour);
                // 讓下一個玩家抽四張牌
                NextPlayerWithoutComputerTurn();
                DrawFourCardsForCurrentPlayer();
                // 重置抽四張牌狀態
                IsDrawFourActive = false;
                // 抽牌後再輪換到下一個玩家
                NextPlayer();
                break;
        }
    }

    public void DrawTwoCardsForCurrentPlayer()
    {
        if (CurrentPlayerIndex == 0)
        {
            // 人類玩家抽兩張牌
            for (int i = 0; i < 2; i++)
            {
                if (DrawPile.Count > 0)
                {
                    var card = DrawPile[0];
                    DrawPile.RemoveAt(0);
                    PlayerHand.Add(card);
                    
                    // 更新統計數據
                    TotalCardsDrawn++;
                    
                    // 觸發抽牌事件
                    EmitCardDrawn(card, CurrentPlayerIndex);
                    
                    GD.Print($"你抽到: {card.Color} {card.CardValue}");
                }
            }
        }
        else
        {
            // 電腦玩家抽兩張牌
            int computerPlayerIndex = CurrentPlayerIndex - 1;
            if (computerPlayerIndex < ComputerPlayers.Count)
            {
                var computerPlayer = ComputerPlayers[computerPlayerIndex];
                for (int i = 0; i < 2; i++)
                {
                    if (DrawPile.Count > 0)
                    {
                        var card = DrawPile[0];
                        DrawPile.RemoveAt(0);
                        computerPlayer.Hand.Add(card);
                        
                        // 更新統計數據
                        TotalCardsDrawn++;
                        
                        // 觸發抽牌事件
                        EmitCardDrawn(card, CurrentPlayerIndex);
                        
                        GD.Print($"電腦玩家 {computerPlayer.PlayerName} 抽到: {card.Color} {card.CardValue}");
                    }
                }
            }
        }
        UpdatePlayerCardCounts();
        EmitGameStateChanged();
    }

    public void DrawFourCardsForCurrentPlayer()
    {
        if (CurrentPlayerIndex == 0)
        {
            // 人類玩家抽四張牌
            for (int i = 0; i < 4; i++)
            {
                if (DrawPile.Count > 0)
                {
                    var card = DrawPile[0];
                    DrawPile.RemoveAt(0);
                    PlayerHand.Add(card);
                    
                    // 更新統計數據
                    TotalCardsDrawn++;
                    
                    // 觸發抽牌事件
                    EmitCardDrawn(card, CurrentPlayerIndex);
                    
                    GD.Print($"你抽到: {card.Color} {card.CardValue}");
                }
            }
        }
        else
        {
            // 電腦玩家抽四張牌
            int computerPlayerIndex = CurrentPlayerIndex - 1;
            if (computerPlayerIndex < ComputerPlayers.Count)
            {
                var computerPlayer = ComputerPlayers[computerPlayerIndex];
                for (int i = 0; i < 4; i++)
                {
                    if (DrawPile.Count > 0)
                    {
                        var card = DrawPile[0];
                        DrawPile.RemoveAt(0);
                        computerPlayer.Hand.Add(card);
                        
                        // 更新統計數據
                        TotalCardsDrawn++;
                        
                        // 觸發抽牌事件
                        EmitCardDrawn(card, CurrentPlayerIndex);
                        
                        GD.Print($"電腦玩家 {computerPlayer.PlayerName} 抽到: {card.Color} {card.CardValue}");
                    }
                }
            }
        }
        UpdatePlayerCardCounts();
        EmitGameStateChanged();
    }

    // 狀態檢查方法
    public bool CanPlayCard(Card card)
    {
        if (CurrentTopCard == null) return true;
        return card.CanPlayOn(CurrentTopCard);
    }

    public bool IsGameOver()
    {
        // 檢查是否有玩家手牌為0
        if (PlayerHand.Count == 0) 
        {
            EmitGameOver(0); // 人類玩家獲勝
            return true;
        }
        
        foreach (var computerPlayer in ComputerPlayers)
        {
            if (computerPlayer.Hand.Count == 0) 
            {
                // 找到獲勝的電腦玩家索引
                int winnerIndex = ComputerPlayers.IndexOf(computerPlayer) + 1;
                EmitGameOver(winnerIndex);
                return true;
            }
        }
        
        return false;
    }

    public bool IsValidMove(Card card)
    {
        return CanPlayCard(card);
    }

    // 工具方法
    public string GetColorText(CardColor color)
    {
        return color switch
        {
            CardColor.Red => "紅色",
            CardColor.Blue => "藍色",
            CardColor.Green => "綠色",
            CardColor.Yellow => "黃色",
            _ => "未知"
        };
    }

    public CardColor GetCardColorFromString(string colorString)
    {
        return colorString.ToLower() switch
        {
            "red" or "紅色" => CardColor.Red,
            "blue" or "藍色" => CardColor.Blue,
            "green" or "綠色" => CardColor.Green,
            "yellow" or "黃色" => CardColor.Yellow,
            _ => CardColor.Red
        };
    }

    public void UpdatePlayerCardCounts()
    {
        // 更新所有玩家的手牌數量統計
        PlayerCardCounts.Clear();
        
        // 人類玩家手牌數量
        PlayerCardCounts[0] = PlayerHand.Count;
        
        // 電腦玩家手牌數量
        for (int i = 0; i < ComputerPlayers.Count; i++)
        {
            PlayerCardCounts[i + 1] = ComputerPlayers[i].Hand.Count;
        }
    }

    // 新增方法：設置遊戲階段
    public void SetGamePhase(GamePhase newPhase)
    {
        CurrentGamePhase = newPhase;
        EmitGamePhaseChanged(newPhase);
        EmitGameStateChanged();
    }

    // 新增方法：打出卡牌
    public void PlayCard(Card card, int playerIndex)
    {
        // 從玩家手牌中移除這張牌
        if (playerIndex == 0)
        {
            PlayerHand.Remove(card);
        }
        else
        {
            int computerPlayerIndex = playerIndex - 1;
            if (computerPlayerIndex < ComputerPlayers.Count)
            {
                ComputerPlayers[computerPlayerIndex].Hand.Remove(card);
            }
        }

        // 將牌添加到棄牌堆
        DiscardPile.Add(card);
        CurrentTopCard = card;

        // 更新統計數據
        TotalCardsPlayed++;
        UpdatePlayerCardCounts();

        // 觸發出牌事件
        EmitCardPlayed(card, playerIndex);
        EmitGameStateChanged();
    }

    // 新增方法：抽牌
    public Card DrawCard(int playerIndex)
    {
        if (DrawPile.Count == 0) return null;

        var card = DrawPile[0];
        DrawPile.RemoveAt(0);

        // 將牌添加到玩家手牌
        if (playerIndex == 0)
        {
            PlayerHand.Add(card);
        }
        else
        {
            int computerPlayerIndex = playerIndex - 1;
            if (computerPlayerIndex < ComputerPlayers.Count)
            {
                ComputerPlayers[computerPlayerIndex].Hand.Add(card);
            }
        }

        // 更新統計數據
        TotalCardsDrawn++;
        UpdatePlayerCardCounts();

        // 觸發抽牌事件
        EmitCardDrawn(card, playerIndex);
        EmitGameStateChanged();

        return card;
    }
}