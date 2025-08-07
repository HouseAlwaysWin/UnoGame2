using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MainGame : Node2D
{
    private GameStateManager gameStateManager;
    private UIManager uiManager;
    private CardAnimationManager cardAnimationManager;

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
    private void OnPlayerTurnChanged(int playerIndex) => GD.Print($"玩家回合改變: 玩家 {playerIndex}");
    private void OnCardPlayed(Card card, int playerIndex)
    {
        string playerName = playerIndex == 0 ? "你" : gameStateManager.GetCurrentPlayerName();
        string message = $"{playerName} 打出: {gameStateManager.GetColorText(card.Color)} {card.CardValue}";
        GD.Print(message);
        uiManager.AddMessage(message);
    }
    private void OnCardDrawn(Card card, int playerIndex)
    {
        string playerName = playerIndex == 0 ? "你" : gameStateManager.GetCurrentPlayerName();
        string message = $"{playerName} 抽到: {gameStateManager.GetColorText(card.Color)} {card.CardValue}";
        GD.Print(message);
        uiManager.AddMessage(message);
    }
    private void OnGamePhaseChanged(int newPhase) => GD.Print($"遊戲階段改變: {(GamePhase)newPhase}");
    private void OnSpecialCardEffect(Card card, int effectType) => GD.Print($"特殊牌效果: {(CardType)effectType}");
    private void OnGameOver(int winnerPlayerIndex)
    {
        string winnerName = winnerPlayerIndex == 0 ? "你" : $"玩家{winnerPlayerIndex + 1}";
        string message = $"遊戲結束！獲勝者: {winnerName}";
        GD.Print(message);
        uiManager.AddMessage(message);
    }

    // UI事件處理
    private void OnDrawCardPressed()
    {
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
        if (gameStateManager.SelectedCard != null && gameStateManager.CanPlayCard(gameStateManager.SelectedCard))
        {
            PlayCard(gameStateManager.SelectedCard);
        }
        else
        {
            GD.Print("請先選擇要打出的手牌或這張牌不能打出！");
        }
    }

    private void OnUnoPressed() => GD.Print("喊UNO!按鈕被按下");
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
        uiManager.SetButtonStates(false, false, true);

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
        uiManager.SetButtonStates(true, false, true);

        if (cardToPlay.Type == CardType.Wild || cardToPlay.Type == CardType.WildDrawFour)
        {
            ShowColorSelectionPanel();
        }
        else
        {
            HandleSpecialCardEffect(cardToPlay);
            NextPlayer();
        }

        if (gameStateManager.IsGameOver())
        {
            uiManager.AddMessage("遊戲結束！");
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
        string allPlayersCardsInfo = gameStateManager.GetAllPlayersCardsInfo();
        
        uiManager.UpdateGameStatusDisplay(currentPlayerName, colorText, allPlayersCardsInfo);
    }

    private void OnPlayerCardClicked(Card clickedCard)
    {
        int cardIndex = clickedCard.HandIndex;
        GD.Print($"玩家點擊了手牌: {clickedCard.Color} {clickedCard.CardValue}, 索引: {cardIndex}");

        if (gameStateManager.SelectedCardIndex == cardIndex)
        {
            gameStateManager.SelectedCard = null;
            gameStateManager.SelectedCardIndex = -1;
            uiManager.SetButtonStates(true, false, true);
            UpdatePlayerHandDisplay();
            return;
        }

        gameStateManager.SelectedCard = clickedCard;
        gameStateManager.SelectedCardIndex = cardIndex;

        bool canPlay = gameStateManager.CanPlayCard(clickedCard);
        uiManager.SetButtonStates(true, canPlay, true);
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
        gameStateManager.NextPlayer();
        UpdateGameStatusDisplay();

        if (gameStateManager.CurrentPlayerIndex > 0)
        {
            uiManager.SetButtonStates(false, false, false);
            ExecuteComputerPlayerTurn();
        }
        else
        {
            uiManager.SetButtonStates(true, false, true);
        }
    }

    private void ExecuteComputerPlayerTurn()
    {
        int computerPlayerIndex = gameStateManager.CurrentPlayerIndex - 1;
        
        if (computerPlayerIndex < gameStateManager.ComputerPlayers.Count)
        {
            var computerPlayer = gameStateManager.ComputerPlayers[computerPlayerIndex];
            uiManager.AddMessage($"{computerPlayer.PlayerName} 的回合開始");

            Card cardToPlay = computerPlayer.ChooseCardToPlay(gameStateManager.CurrentTopCard);

            if (cardToPlay != null)
            {
                uiManager.AddMessage($"{computerPlayer.PlayerName} 打出: {gameStateManager.GetColorText(cardToPlay.Color)} {cardToPlay.CardValue}");
                gameStateManager.PlayCard(cardToPlay, gameStateManager.CurrentPlayerIndex);
                UpdateDiscardPileDisplay();
                UpdateGameStatusDisplay();

                if (cardToPlay.Type == CardType.Wild || cardToPlay.Type == CardType.WildDrawFour)
                {
                    CardColor chosenColor = computerPlayer.ChooseColor();
                    gameStateManager.CurrentTopCard.Color = chosenColor;
                    uiManager.AddMessage($"{computerPlayer.PlayerName} 選擇顏色: {gameStateManager.GetColorText(chosenColor)}");
                    UpdateDiscardPileDisplay();
                    UpdateGameStatusDisplay();
                    HandleSpecialCardEffect(cardToPlay);
                    // HandleSpecialCardEffect 已經處理了回合輪換，不需要再次調用 NextPlayer()
                }
                else
                {
                    // 處理其他類型的牌（Skip、Reverse、DrawTwo、Number）
                    HandleSpecialCardEffect(cardToPlay);
                    // HandleSpecialCardEffect 已經處理了回合輪換，不需要再次調用 NextPlayer()
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
                        uiManager.AddMessage($"{computerPlayer.PlayerName} 抽到: {gameStateManager.GetColorText(drawnCard.Color)} {drawnCard.CardValue}");
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
    }

    private void HandleSpecialCardEffect(Card card)
    {
        gameStateManager.HandleSpecialCardEffect(card);
        
        switch (card.Type)
        {
            case CardType.Skip: uiManager.AddMessage("跳過下一個玩家的回合"); break;
            case CardType.Reverse: uiManager.AddMessage("遊戲方向改變"); break;
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
        uiManager.SetButtonStates(false, false, false);
        uiManager.HideColorSelectionPanel();

        // 總是從人類玩家開始
        gameStateManager.CurrentPlayerIndex = 0;
        
        UpdateGameStatusDisplay();
        
        // 啟用人類玩家的按鈕
        uiManager.SetButtonStates(true, false, true);

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
                })).SetDelay(0.2f);
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