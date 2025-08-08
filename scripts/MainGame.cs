using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MainGame : Node2D
{
    private GameStateManager gameStateManager;
    private UIManager uiManager;
    private CardAnimationManager cardAnimationManager;
    private bool isComputerTurnRunning = false;

    private bool ShouldEnableUno()
    {
        return gameStateManager != null &&
               gameStateManager.CurrentPlayerIndex == 0 &&
               gameStateManager.PlayerHand.Count == 1;
    }

    private async Task WaitSeconds(float seconds)
    {
        var timer = GetTree().CreateTimer(seconds);
        await ToSignal(timer, "timeout");
    }

    public override void _Ready()
    {
        GameLogger.Info("主遊戲場景已加載");
        
        GameConfig.LoadConfig();
        gameStateManager = GetNode<GameStateManager>("GameStateManager");

        // 初始化管理器
        InitializeManagers();
        ConnectSignals();
        InitializeGame();
    }

    private void InitializeManagers()
    {
        uiManager = new UIManager();
        AddChild(uiManager);

        var uiUpdateManager = new UIUpdateManager();
        AddChild(uiUpdateManager);
        uiUpdateManager.Initialize(uiManager, gameStateManager);

        cardAnimationManager = new CardAnimationManager();
        AddChild(cardAnimationManager);
    }

    private void ConnectSignals()
    {
        if (uiManager != null)
        {
            uiManager.OnDrawCardPressed += OnDrawCardPressed;
            uiManager.OnPlayCardPressed += OnPlayCardPressed;
            uiManager.OnUnoPressed += OnUnoPressed;
            uiManager.OnBackToMenuPressed += OnBackToMenuPressed;
            uiManager.OnColorSelected += OnColorSelected;
        }

        if (gameStateManager != null)
        {
            gameStateManager.GameStateChanged += OnGameStateChanged;
            gameStateManager.PlayerTurnChanged += OnPlayerTurnChanged;
            gameStateManager.CardPlayed += OnCardPlayed;
            gameStateManager.CardDrawn += OnCardDrawn;
            gameStateManager.GamePhaseChanged += OnGamePhaseChanged;
            gameStateManager.SpecialCardEffect += OnSpecialCardEffect;
            gameStateManager.GameOver += OnGameOver;
        }
    }

    // GameStateManager事件處理
    private void OnGameStateChanged() => GD.Print("遊戲狀態已改變");
    private void OnPlayerTurnChanged(int playerIndex)
    {
        GD.Print($"玩家回合改變: 玩家 {playerIndex}");
        uiManager.UpdateCurrentTurnHighlight(gameStateManager.CurrentPlayerIndex);
    }
    private void OnCardPlayed(Card card, int playerIndex)
    {
        string playerName = playerIndex == 0 ? "你" : gameStateManager.GetCurrentPlayerName();
        string message;
        if (card.Type == CardType.Wild)
        {
            message = $"{playerName} 打出: 萬能牌";
        }
        else if (card.Type == CardType.WildDrawFour)
        {
            message = $"{playerName} 打出: 萬能牌+4";
        }
        else
        {
            message = $"{playerName} 打出: {gameStateManager.GetColorText(card.Color)} {card.CardValue}";
        }
        GD.Print(message);
        uiManager.AddMessage(message);
    }
    private void OnCardDrawn(Card card, int playerIndex)
    {
        // 僅顯示人類玩家抽牌，不顯示電腦抽到什麼
        if (playerIndex == 0)
        {
            string message = $"你 抽到: {gameStateManager.GetColorText(card.Color)} {card.CardValue}";
            GD.Print(message);
            uiManager.AddMessage(message);
        }
    }
    private void OnGamePhaseChanged(int newPhase) => GD.Print($"遊戲階段改變: {(GamePhase)newPhase}");
    private void OnSpecialCardEffect(Card card, int effectType) => GD.Print($"特殊牌效果: {(CardType)effectType}");
    private void OnGameOver(int winnerPlayerIndex)
    {
        string winnerName = winnerPlayerIndex == 0 ? "你" : $"玩家{winnerPlayerIndex + 1}";
        string message = $"遊戲結束！獲勝者: {winnerName}";
        GD.Print(message);
        uiManager.AddMessage(message);
        uiManager.ShowToast(message, 2.2f);
    }

    // UI事件處理
    private void OnDrawCardPressed()
    {
        // 檢查是否是人類玩家的回合
        if (gameStateManager.CurrentPlayerIndex != 0)
        {
            GD.Print("現在不是你的回合，無法抽牌");
            return;
        }

        GameLogger.PlayerAction("玩家", "按下抽牌按鈕");
        if (gameStateManager.IsAnimating) return;

        if (gameStateManager.DrawPile.Count == 0)
        {
            gameStateManager.ReshuffleDiscardPile();
            uiManager.AddMessage("抽牌堆已空，重新洗牌");
        }

        if (gameStateManager.DrawPile.Count > 0)
        {
            StartDrawCardAnimation();
        }
        else
        {
            uiManager.AddMessage("抽牌堆仍然為空，無法抽牌");
        }
    }

    private void OnPlayCardPressed()
    {
        // 檢查是否是人類玩家的回合
        if (gameStateManager.CurrentPlayerIndex != 0)
        {
            GD.Print("現在不是你的回合，無法出牌");
            return;
        }

        if (gameStateManager.SelectedCard != null && gameStateManager.CanPlayCard(gameStateManager.SelectedCard))
        {
            PlayCard(gameStateManager.SelectedCard);
        }
        else
        {
            GD.Print("請先選擇要打出的手牌或這張牌不能打出！");
        }
    }

    private void OnUnoPressed()
    {
        // 檢查是否是人類玩家的回合
        if (gameStateManager.CurrentPlayerIndex != 0)
        {
            GD.Print("現在不是你的回合，無法喊UNO");
            return;
        }

        // 僅當手牌僅剩一張時可以喊 UNO
        if (gameStateManager.PlayerHand.Count != 1)
        {
            GD.Print("只有剩一張牌時才能喊UNO");
            return;
        }

        GD.Print("喊UNO!按鈕被按下");
        uiManager.ShowUnoCallDialog("你");
        // 標記玩家已喊 UNO
        if (gameStateManager.PlayerHasCalledUno.Count > 0)
        {
            gameStateManager.PlayerHasCalledUno[0] = true;
        }
    }
    private void OnBackToMenuPressed() => GetTree().ChangeSceneToFile("res://scenes/main_screen.tscn");

    private void OnColorSelected(string color)
    {
        GD.Print($"選擇顏色: {color}");
        uiManager.SetColorSelectionConfirmed(color);

        var delayTween = CreateTween();
        delayTween.TweenCallback(Callable.From(() =>
        {
            gameStateManager.IsWaitingForColorSelection = false;
            gameStateManager.SetGamePhase(GamePhase.Playing);
            uiManager.HideColorSelectionPanel();

            if (gameStateManager.CurrentTopCard != null && 
                (gameStateManager.CurrentTopCard.Type == CardType.Wild || gameStateManager.CurrentTopCard.Type == CardType.WildDrawFour))
            {
                var newTopCard = new Card();
                newTopCard.SetCard(gameStateManager.GetCardColorFromString(color), 
                                 gameStateManager.CurrentTopCard.CardValue, 
                                 gameStateManager.CurrentTopCard.Type);

                if (gameStateManager.DiscardPile.Count > 0)
                {
                    gameStateManager.DiscardPile[gameStateManager.DiscardPile.Count - 1] = newTopCard;
                }
                gameStateManager.CurrentTopCard = newTopCard;

                UpdateDiscardPileDisplay();
                UpdateGameStatusDisplay();
                HandleSpecialCardEffect(gameStateManager.CurrentTopCard);
                NextPlayer();
            }
        })).SetDelay(1.0f);
    }

    // 動畫相關方法
    private void StartDrawCardAnimation()
    {
        if (gameStateManager.DrawPile.Count == 0) return;

        if (!gameStateManager.EnableAnimations)
        {
            gameStateManager.DrawCard(0);
            UpdateDisplays();
            NextPlayer();
            return;
        }

        gameStateManager.IsAnimating = true;
        uiManager.SetButtonStates(false, false, false); // 動畫期間禁用所有按鈕

        // 不要提前移除牌，等動畫完成後再移除
        var cardToDraw = gameStateManager.DrawPile[0];

        if (cardAnimationManager != null)
        {
            // 計算動畫位置
            Vector2 startPos = Vector2.Zero;
            Vector2 targetPos = Vector2.Zero;
            
            if (uiManager?.DrawPileUI != null)
            {
                startPos = uiManager.DrawPileUI.GlobalPosition + uiManager.DrawPileUI.Size / 2;
            }
            
            if (uiManager?.PlayerHandUI != null)
            {
                float cardWidth = 80.0f;
                float cardSpacing = 10.0f;
                float startX = uiManager.PlayerHandUI.GlobalPosition.X + cardWidth / 2;
                float targetX = startX + (gameStateManager.PlayerHand.Count * (cardWidth + cardSpacing));
                targetPos = new Vector2(targetX, uiManager.PlayerHandUI.GlobalPosition.Y + uiManager.PlayerHandUI.Size.Y / 2);
            }

            cardAnimationManager.CreateDrawCardAnimation(cardToDraw, startPos, targetPos, () =>
            {
                // 動畫完成後才移除牌並添加到手牌
                if (gameStateManager.DrawPile.Count > 0)
                {
                    gameStateManager.DrawPile.RemoveAt(0);
                    gameStateManager.PlayerHand.Add(cardToDraw);
                }
                gameStateManager.IsAnimating = false;
                UpdateDisplays();
                NextPlayer();
            });
        }
        else
        {
            // 如果沒有動畫管理器，直接處理
            if (gameStateManager.DrawPile.Count > 0)
            {
                gameStateManager.DrawPile.RemoveAt(0);
                gameStateManager.PlayerHand.Add(cardToDraw);
            }
            gameStateManager.IsAnimating = false;
            UpdateDisplays();
            NextPlayer();
        }
    }

    private void PlayCard(Card cardToPlay)
    {
        GD.Print($"開始出牌: {cardToPlay.Color} {cardToPlay.CardValue}");
        StartPlayCardAnimation(cardToPlay);
    }

    private void StartPlayCardAnimation(Card cardToPlay)
    {
        if (cardAnimationManager != null)
        {
            // 計算動畫位置
            Vector2 startPos = Vector2.Zero;
            Vector2 targetPos = Vector2.Zero;
            
            // 找到選中卡牌的位置
            if (uiManager.PlayerHandUI != null && uiManager.PlayerHandUI.GetChildCount() > 0 && gameStateManager.SelectedCardIndex >= 0)
            {
                int uiIndex = 0;
                for (int i = 0; i < uiManager.PlayerHandUI.GetChildCount(); i++)
                {
                    var child = uiManager.PlayerHandUI.GetChild(i);
                    if (child is MarginContainer marginContainer && marginContainer.GetChild(0) is Card uiCard)
                    {
                        if (uiIndex == gameStateManager.SelectedCardIndex)
                        {
                            startPos = uiCard.GlobalPosition;
                            break;
                        }
                        uiIndex++;
                    }
                    else if (child is Card cardNode)
                    {
                        if (uiIndex == gameStateManager.SelectedCardIndex)
                        {
                            startPos = cardNode.GlobalPosition;
                            break;
                        }
                        uiIndex++;
                    }
                }
            }

            // 設置目標位置（棄牌堆位置）
            if (uiManager?.DiscardPileUI != null)
            {
                targetPos = uiManager.DiscardPileUI.GlobalPosition;
            }

            cardAnimationManager.CreatePlayCardAnimation(cardToPlay, startPos, targetPos, () =>
            {
                ExecutePlayCard(cardToPlay);
            });
        }
        else
        {
            ExecutePlayCard(cardToPlay);
        }
    }

    private void ExecutePlayCard(Card cardToPlay)
    {
        gameStateManager.PlayCard(cardToPlay, 0);
        gameStateManager.SelectedCard = null;
        gameStateManager.SelectedCardIndex = -1;

        UpdateDisplays();
        uiManager.SetButtonStates(true, false, ShouldEnableUno());

        if (cardToPlay.Type == CardType.Wild || cardToPlay.Type == CardType.WildDrawFour)
        {
            ShowColorSelectionPanel();
        }
        else
        {
            HandleSpecialCardEffect(cardToPlay);
            // 換手規則：
            // - Number/Reverse：這裡手動換一次
            // - Skip：跳過下一位（先換到下一位但不執行，接著再換一次）
            if (cardToPlay.Type == CardType.Number || cardToPlay.Type == CardType.Reverse)
            {
                NextPlayer();
            }
            else if (cardToPlay.Type == CardType.Skip)
            {
                gameStateManager.NextPlayerWithoutComputerTurn(); // 跳過一位
                NextPlayer(); // 換到下一位
            }
        }

        if (gameStateManager.IsGameOver())
        {
            uiManager.AddMessage("遊戲結束！");
        }

        // 如果出完牌後只剩一張，提示可以喊 UNO（自動或提示）
        if (gameStateManager.CurrentPlayerIndex == 0 && gameStateManager.PlayerHand.Count == 1)
        {
            uiManager.AddMessage("你只剩一張牌了，記得喊 UNO!");
            // 重置你的 UNO 旗標為尚未喊，等待本回合喊UNO
            if (gameStateManager.PlayerHasCalledUno.Count > 0)
            {
                gameStateManager.PlayerHasCalledUno[0] = false;
            }
        }
    }

    // 顯示更新方法
    private void UpdateDisplays()
    {
        UpdatePlayerHandDisplay();
        UpdateDrawPileDisplay();
        UpdateDiscardPileDisplay();
        UpdateGameStatusDisplay();
    }

    private void UpdatePlayerHandDisplay()
    {
        if (uiManager != null)
        {
            uiManager.UpdatePlayerHandDisplay(gameStateManager.PlayerHand, gameStateManager.SelectedCardIndex, OnPlayerCardClicked);
        }
    }

    private void UpdateDrawPileDisplay()
    {
        if (uiManager != null)
        {
            uiManager.UpdateDrawPileDisplay(gameStateManager.DrawPile.Count);
        }
    }

    private void UpdateDiscardPileDisplay()
    {
        if (uiManager != null)
        {
            uiManager.UpdateDiscardPileDisplay(gameStateManager.CurrentTopCard);
        }
    }

    private void UpdateGameStatusDisplay()
    {
        string currentPlayerName = gameStateManager.GetCurrentPlayerName();
        string colorText = gameStateManager.CurrentTopCard != null ? 
                          gameStateManager.GetColorText(gameStateManager.CurrentTopCard.Color) : "無";
        // 僅顯示顏色與當前玩家於上方狀態
        uiManager.UpdateGameStatusDisplay(currentPlayerName, colorText, "");
        // 更新順序列上的手牌數
        var counts = new List<int>();
        counts.Add(gameStateManager.PlayerHand.Count);
        foreach (var cpu in gameStateManager.ComputerPlayers)
        {
            counts.Add(cpu.Hand.Count);
        }
        uiManager.UpdateTurnOrderCounts(counts);
    }

    private void OnPlayerCardClicked(Card clickedCard)
    {
        // 檢查是否是人類玩家的回合
        if (gameStateManager.CurrentPlayerIndex != 0)
        {
            GD.Print("現在不是你的回合，無法選擇手牌");
            return;
        }

        int cardIndex = clickedCard.HandIndex;
        GD.Print($"玩家點擊了手牌: {clickedCard.Color} {clickedCard.CardValue}, 索引: {cardIndex}");

        if (gameStateManager.SelectedCardIndex == cardIndex)
        {
            gameStateManager.SelectedCard = null;
            gameStateManager.SelectedCardIndex = -1;
            uiManager.SetButtonStates(true, false, ShouldEnableUno());
            UpdatePlayerHandDisplay();
            return;
        }

        gameStateManager.SelectedCard = clickedCard;
        gameStateManager.SelectedCardIndex = cardIndex;

        bool canPlay = gameStateManager.CanPlayCard(clickedCard);
        uiManager.SetButtonStates(true, canPlay, ShouldEnableUno());
        UpdatePlayerHandDisplay();
    }

    private void ShowColorSelectionPanel()
    {
        gameStateManager.SetGamePhase(GamePhase.ColorSelection);
        gameStateManager.IsWaitingForColorSelection = true;
        uiManager.ShowColorSelectionPanel();
    }

    private void NextPlayer()
    {
        // 在切換前，檢查剛結束回合的玩家是否違規（手牌=1且未喊UNO）
        int previousPlayerIndex = gameStateManager.CurrentPlayerIndex;
        // 人類玩家
        if (previousPlayerIndex == 0)
        {
            if (gameStateManager.PlayerHand.Count == 1)
            {
                bool hasCalled = gameStateManager.PlayerHasCalledUno.Count > 0 && gameStateManager.PlayerHasCalledUno[0];
                if (!hasCalled)
                {
                    uiManager.AddMessage("你沒有喊 UNO，受到懲罰抽一張牌！");
                    var drawnCard = gameStateManager.DrawCard(0);
                    // 懲罰動畫（人類）：從抽牌堆飛到玩家手牌區
                    if (drawnCard != null && cardAnimationManager != null && uiManager?.DrawPileUI != null && uiManager?.PlayerHandUI != null)
                    {
                        Vector2 startPos = uiManager.DrawPileUI.GlobalPosition + uiManager.DrawPileUI.Size / 2;
                        float cardWidth = 80.0f;
                        float cardSpacing = 10.0f;
                        float startX = uiManager.PlayerHandUI.GlobalPosition.X + cardWidth / 2;
                        float targetX = startX + (gameStateManager.PlayerHand.Count * (cardWidth + cardSpacing));
                        Vector2 targetPos = new Vector2(targetX, uiManager.PlayerHandUI.GlobalPosition.Y + uiManager.PlayerHandUI.Size.Y / 2);
                        cardAnimationManager.CreateDrawCardAnimation(drawnCard, startPos, targetPos, () => { });
                    }
                    UpdateDisplays();
                }
            }
            // 重置旗標
            if (gameStateManager.PlayerHasCalledUno.Count > 0)
                gameStateManager.PlayerHasCalledUno[0] = false;
        }
        else
        {
            int cpuIdx = previousPlayerIndex - 1;
            if (cpuIdx >= 0 && cpuIdx < gameStateManager.ComputerPlayers.Count)
            {
                if (gameStateManager.ComputerPlayers[cpuIdx].Hand.Count == 1)
                {
                    bool hasCalled = gameStateManager.PlayerHasCalledUno.Count > previousPlayerIndex && gameStateManager.PlayerHasCalledUno[previousPlayerIndex];
                    if (!hasCalled)
                    {
                        uiManager.AddMessage($"{gameStateManager.ComputerPlayers[cpuIdx].PlayerName} 沒有喊 UNO，受到懲罰抽一張牌！");
                        var drawnCard = gameStateManager.DrawCard(previousPlayerIndex);
                        // 懲罰動畫（電腦）：從抽牌堆飛到棄牌堆上方（代表電腦手牌區）
                        if (drawnCard != null && cardAnimationManager != null && uiManager?.DrawPileUI != null)
                        {
                            Vector2 startPos = uiManager.DrawPileUI.GlobalPosition + uiManager.DrawPileUI.Size / 2;
                            Vector2 targetPos = (uiManager?.DiscardPileUI != null)
                                ? uiManager.DiscardPileUI.GlobalPosition + new Vector2(0, -140)
                                : startPos + new Vector2(0, -140);
                            cardAnimationManager.CreateDrawCardAnimation(drawnCard, startPos, targetPos, () => { });
                        }
                        UpdateGameStatusDisplay();
                    }
                }
                // 重置旗標
                if (gameStateManager.PlayerHasCalledUno.Count > previousPlayerIndex)
                    gameStateManager.PlayerHasCalledUno[previousPlayerIndex] = false;
            }
        }

        gameStateManager.NextPlayer();
        UpdateGameStatusDisplay();

        if (gameStateManager.CurrentPlayerIndex > 0)
        {
            uiManager.SetButtonStates(false, false, false); // 電腦玩家回合，禁用所有按鈕
            if (!isComputerTurnRunning)
            {
                ExecuteComputerPlayerTurn();
            }
        }
        else
        {
            uiManager.SetButtonStates(true, false, ShouldEnableUno()); // 人類玩家回合，UNO 僅在剩一張時啟用
        }
    }

    private async void ExecuteComputerPlayerTurn()
    {
        if (isComputerTurnRunning) return;
        isComputerTurnRunning = true;
        int computerPlayerIndex = gameStateManager.CurrentPlayerIndex - 1;
        
        if (computerPlayerIndex < gameStateManager.ComputerPlayers.Count)
        {
            var computerPlayer = gameStateManager.ComputerPlayers[computerPlayerIndex];
            uiManager.AddMessage($"{computerPlayer.PlayerName} 的回合開始");

            // 停頓一下，讓動作更容易看清
            await WaitSeconds(0.6f);

            Card cardToPlay = computerPlayer.ChooseCardToPlay(gameStateManager.CurrentTopCard);

            if (cardToPlay != null)
            {
                // 出牌訊息改由 OnCardPlayed 事件統一產生，避免重複
                if (cardAnimationManager != null && uiManager?.DiscardPileUI != null)
                {
                    gameStateManager.IsAnimating = true;
                    uiManager.SetButtonStates(false, false, false);
                    // 電腦出牌動畫：從棄牌堆上方飛入
                    Vector2 startPos = uiManager.DiscardPileUI.GlobalPosition + new Vector2(0, -140);
                    Vector2 targetPos = uiManager.DiscardPileUI.GlobalPosition;
                    bool computerWon = false;
                    cardAnimationManager.CreatePlayCardAnimation(cardToPlay, startPos, targetPos, () =>
                    {
                        gameStateManager.PlayCard(cardToPlay, gameStateManager.CurrentPlayerIndex);
                        UpdateDiscardPileDisplay();
                        UpdateGameStatusDisplay();
                        gameStateManager.IsAnimating = false;
                        // 若出完變成 0 張，直接結束
                        if (computerPlayer.Hand.Count == 0)
                        {
                            gameStateManager.IsGameOver();
                            computerWon = true;
                        }
                    });
                    await WaitSeconds(0.6f);
                    if (computerWon)
                    {
                        isComputerTurnRunning = false;
                        return;
                    }
                }
                else
                {
                    gameStateManager.PlayCard(cardToPlay, gameStateManager.CurrentPlayerIndex);
                    UpdateDiscardPileDisplay();
                    UpdateGameStatusDisplay();
                    if (computerPlayer.Hand.Count == 0)
                    {
                        gameStateManager.IsGameOver();
                        isComputerTurnRunning = false;
                        return;
                    }
                }

                if (cardToPlay.Type == CardType.Wild || cardToPlay.Type == CardType.WildDrawFour)
                {
                    CardColor chosenColor = computerPlayer.ChooseColor();
                    gameStateManager.CurrentTopCard.Color = chosenColor;
                    uiManager.AddMessage($"{computerPlayer.PlayerName} 選擇顏色: {gameStateManager.GetColorText(chosenColor)}");
                    UpdateDiscardPileDisplay();
                    UpdateGameStatusDisplay();
                    HandleSpecialCardEffect(cardToPlay);
                    // 萬能牌需要手動更新UI狀態，因為GameStateManager已經處理了回合輪換
                    UpdateGameStatusDisplay();
                    // 電腦喊 UNO：若只剩一張
                    if (computerPlayer.Hand.Count == 1)
                    {
                        uiManager.ShowUnoCallDialog(computerPlayer.PlayerName);
                        if (gameStateManager.PlayerHasCalledUno.Count > gameStateManager.CurrentPlayerIndex)
                        {
                            gameStateManager.PlayerHasCalledUno[gameStateManager.CurrentPlayerIndex] = true;
                        }
                    }
                    // Wild 自己不會在 GSM 內換手，這裡主動換到下一位
                    if (cardToPlay.Type == CardType.Wild)
                    {
                        NextPlayer();
                    }
                    // 後續由 NextPlayer 觸發
                }
                else
                {
                    // 處理其他類型的牌（Skip、Reverse、DrawTwo、Number）
                    HandleSpecialCardEffect(cardToPlay);
                    // 更新UI狀態，因為GameStateManager已經處理了回合輪換
                    UpdateGameStatusDisplay();
                    // 對於數字/反轉，需在這裡手動切到下一位（反轉僅切換方向，不換手）
                    if (cardToPlay.Type == CardType.Number || cardToPlay.Type == CardType.Reverse)
                    {
                        NextPlayer();
                    }
                    // 電腦喊 UNO：若只剩一張
                    if (computerPlayer.Hand.Count == 1)
                    {
                        uiManager.ShowUnoCallDialog(computerPlayer.PlayerName);
                        if (gameStateManager.PlayerHasCalledUno.Count > gameStateManager.CurrentPlayerIndex)
                        {
                            gameStateManager.PlayerHasCalledUno[gameStateManager.CurrentPlayerIndex] = true;
                        }
                    }
                }
            }
            else
            {
                uiManager.AddMessage($"{computerPlayer.PlayerName} 沒有可以打出的牌，抽一張牌");

                if (gameStateManager.DrawPile.Count == 0)
                {
                    gameStateManager.ReshuffleDiscardPile();
                    uiManager.AddMessage("抽牌堆已空，重新洗牌");
                }

                if (gameStateManager.DrawPile.Count > 0)
                {
                    var drawnCard = gameStateManager.DrawCard(gameStateManager.CurrentPlayerIndex);
                    if (drawnCard != null)
                    {
                        // 抽牌訊息改由 OnCardDrawn 事件統一產生，避免重複
                        gameStateManager.UpdatePlayerCardCounts();
                        if (cardAnimationManager != null && uiManager?.DrawPileUI != null)
                        {
                            Vector2 startPos = uiManager.DrawPileUI.GlobalPosition + uiManager.DrawPileUI.Size / 2;
                            Vector2 targetPos = (uiManager?.DiscardPileUI != null)
                                ? uiManager.DiscardPileUI.GlobalPosition + new Vector2(0, -140)
                                : startPos + new Vector2(0, -140);
                            cardAnimationManager.CreateDrawCardAnimation(drawnCard, startPos, targetPos, () => { });
                            await WaitSeconds(0.5f);
                        }
                        UpdateGameStatusDisplay();
                        // 電腦喊 UNO：抽牌後若只剩一張
                        if (computerPlayer.Hand.Count == 1)
                        {
                            uiManager.ShowUnoCallDialog(computerPlayer.PlayerName);
                            if (gameStateManager.PlayerHasCalledUno.Count > gameStateManager.CurrentPlayerIndex)
                            {
                                gameStateManager.PlayerHasCalledUno[gameStateManager.CurrentPlayerIndex] = true;
                            }
                        }
                    }
                }
                NextPlayer();
            }
        }
        else
        {
            gameStateManager.CurrentPlayerIndex = 0;
            NextPlayer();
        }
        isComputerTurnRunning = false;
        // 若下一位仍是電腦，並且這次回合結束後仍需繼續，這裡啟動下一位電腦回合
        if (gameStateManager.CurrentPlayerIndex > 0)
        {
            ExecuteComputerPlayerTurn();
        }
    }

    private void HandleSpecialCardEffect(Card card)
    {
        gameStateManager.HandleSpecialCardEffect(card);
        
        switch (card.Type)
        {
            case CardType.Number: uiManager.AddMessage("數字牌，沒有特殊效果"); break;
            case CardType.Skip: uiManager.AddMessage("跳過下一個玩家的回合"); break;
            case CardType.Reverse:
                uiManager.AddMessage("遊戲方向改變");
                // 方向已在 GameStateManager 中切換，這裡僅刷新順序列與高亮
                var names = new List<string> { "玩家1 (你)" };
                foreach (var cpu in gameStateManager.ComputerPlayers) names.Add(cpu.PlayerName);
                uiManager.InitializeTurnOrder(names);
                var counts = new List<int> { gameStateManager.PlayerHand.Count };
                foreach (var cpu in gameStateManager.ComputerPlayers) counts.Add(cpu.Hand.Count);
                uiManager.UpdateTurnOrderCounts(counts);
                uiManager.UpdateCurrentTurnHighlight(gameStateManager.CurrentPlayerIndex);
                break;
            case CardType.DrawTwo: uiManager.AddMessage("下一個玩家抽兩張牌"); break;
            case CardType.Wild: uiManager.AddMessage("萬能牌，輪換到下一個玩家"); break;
            case CardType.WildDrawFour: uiManager.AddMessage("下一個玩家抽四張牌"); break;
        }
    }

    // 遊戲初始化
    private void InitializeGame()
    {
        GD.Print($"初始化UNO遊戲... 遊玩人數: {gameStateManager.PlayerCount}人");
        
        gameStateManager.SetGamePhase(GamePhase.Initializing);
        gameStateManager.CreateComputerPlayers();
        gameStateManager.CreateDeck();
        gameStateManager.ShuffleDeck();
        gameStateManager.SetFirstTopCard();
        
        uiManager.UpdateDrawPileDisplay(gameStateManager.DrawPile.Count);
        uiManager.UpdateDiscardPileDisplay(gameStateManager.CurrentTopCard);
        uiManager.SetButtonStates(false, false, false); // 初始化時禁用所有按鈕
        uiManager.HideColorSelectionPanel();

        // 總是從人類玩家開始
        gameStateManager.CurrentPlayerIndex = 0;
        
        UpdateGameStatusDisplay();
        // 初始化玩家順序UI
        var playerNames = new List<string>();
        playerNames.Add("玩家1 (你)");
        foreach (var cpu in gameStateManager.ComputerPlayers)
        {
            playerNames.Add(cpu.PlayerName);
        }
        uiManager.InitializeTurnOrder(playerNames);
        uiManager.UpdateCurrentTurnHighlight(gameStateManager.CurrentPlayerIndex);
        
        // 啟用人類玩家的按鈕
        uiManager.SetButtonStates(true, false, ShouldEnableUno());

        StartInitialDealAnimation();
    }

    private void StartInitialDealAnimation()
    {
        gameStateManager.PlayerHand.Clear();
        foreach (var computerPlayer in gameStateManager.ComputerPlayers)
        {
            computerPlayer.Hand.Clear();
        }
        DealInitialCardWithAnimation(0, true);
    }

    private void DealInitialCardWithAnimation(int cardIndex, bool isHumanPlayer = true)
    {
        if (cardIndex >= gameStateManager.InitialCardsPerPlayer || gameStateManager.DrawPile.Count == 0)
        {
            GD.Print($"發牌完成，人類玩家手牌: {gameStateManager.PlayerHand.Count} 張");
            if (isHumanPlayer)
            {
                DealComputerPlayersCards();
            }
            return;
        }

        // 不要提前移除牌，等動畫完成後再移除
        var cardToDraw = gameStateManager.DrawPile[0];
        GD.Print($"開始發第 {cardIndex + 1} 張牌: {cardToDraw.Color} {cardToDraw.CardValue}, 抽牌堆剩餘: {gameStateManager.DrawPile.Count} 張");

        if (cardAnimationManager != null)
        {
            // 計算動畫位置
            Vector2 startPos = Vector2.Zero;
            Vector2 targetPos = Vector2.Zero;
            
            if (uiManager?.DrawPileUI != null)
            {
                startPos = uiManager.DrawPileUI.GlobalPosition + uiManager.DrawPileUI.Size / 2;
            }
            
            if (uiManager?.PlayerHandUI != null)
            {
                float cardWidth = 80.0f;
                float cardSpacing = 10.0f;
                float startX = uiManager.PlayerHandUI.GlobalPosition.X + cardWidth / 2;
                float targetX = startX + (cardIndex * (cardWidth + cardSpacing));
                targetPos = new Vector2(targetX, uiManager.PlayerHandUI.GlobalPosition.Y + uiManager.PlayerHandUI.Size.Y / 2);
            }

            cardAnimationManager.CreateDealCardAnimation(cardToDraw, startPos, targetPos, cardIndex, () =>
            {
                // 動畫完成後才移除牌並添加到手牌
                GD.Print($"動畫完成，添加牌到手牌: {cardToDraw.Color} {cardToDraw.CardValue}");
                if (gameStateManager.DrawPile.Count > 0)
                {
                    gameStateManager.DrawPile.RemoveAt(0);
                    gameStateManager.PlayerHand.Add(cardToDraw);
                    GD.Print($"牌已添加，人類玩家手牌: {gameStateManager.PlayerHand.Count} 張, 抽牌堆剩餘: {gameStateManager.DrawPile.Count} 張");
                    
                    // 立即更新顯示
                    UpdatePlayerHandDisplay();
                    UpdateDrawPileDisplay();
                    
                    // 驗證第一張牌是否正確添加
                    if (gameStateManager.PlayerHand.Count > 0)
                    {
                        var firstCard = gameStateManager.PlayerHand[0];
                        GD.Print($"驗證第一張牌: {firstCard.Color} {firstCard.CardValue}");
                    }
                }
                else
                {
                    GD.PrintErr("抽牌堆已空，無法移除牌");
                }

                var delayTween = CreateTween();
                delayTween.TweenCallback(Callable.From(() =>
                {
                    DealInitialCardWithAnimation(cardIndex + 1);
                })).SetDelay(0.1f); // 從 0.2f 減少到 0.1f
            });
        }
        else
        {
            // 如果沒有動畫管理器，直接處理
            GD.Print($"直接添加牌到手牌: {cardToDraw.Color} {cardToDraw.CardValue}");
            if (gameStateManager.DrawPile.Count > 0)
            {
                gameStateManager.DrawPile.RemoveAt(0);
                gameStateManager.PlayerHand.Add(cardToDraw);
                GD.Print($"牌已添加，人類玩家手牌: {gameStateManager.PlayerHand.Count} 張, 抽牌堆剩餘: {gameStateManager.DrawPile.Count} 張");
                
                // 立即更新顯示
                UpdatePlayerHandDisplay();
                UpdateDrawPileDisplay();
                
                // 驗證第一張牌是否正確添加
                if (gameStateManager.PlayerHand.Count > 0)
                {
                    var firstCard = gameStateManager.PlayerHand[0];
                    GD.Print($"驗證第一張牌: {firstCard.Color} {firstCard.CardValue}");
                }
            }
            DealInitialCardWithAnimation(cardIndex + 1);
        }
    }

    private void DealComputerPlayersCards()
    {
        for (int playerIndex = 0; playerIndex < gameStateManager.ComputerPlayers.Count; playerIndex++)
        {
            var computerPlayer = gameStateManager.ComputerPlayers[playerIndex];
            
            for (int cardIndex = 0; cardIndex < gameStateManager.InitialCardsPerPlayer; cardIndex++)
            {
                if (gameStateManager.DrawPile.Count > 0)
                {
                    var cardToDraw = gameStateManager.DrawPile[0];
                    gameStateManager.DrawPile.RemoveAt(0);
                    computerPlayer.Hand.Add(cardToDraw);
                }
                else
                {
                    break;
                }
            }
        }

        UpdateGameStatusDisplay();
        gameStateManager.SetGamePhase(GamePhase.Playing);

        // 移除自動開始電腦玩家回合的邏輯
        // 遊戲現在從人類玩家開始，等待人類玩家出第一張牌
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            GetTree().ChangeSceneToFile("res://scenes/main_screen.tscn");
        }
    }
}