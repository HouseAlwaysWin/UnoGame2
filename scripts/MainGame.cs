using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MainGame : Node2D
{
    private GameStateManager gameStateManager;
    private UIManager uiManager;
    
    // 動畫相關
    private Tween currentTween;
    private CardAnimationManager cardAnimationManager;

    public override void _Ready()
    {
        GD.Print("主遊戲場景已加載");
        gameStateManager = GetNode<GameStateManager>("GameStateManager");

        // 創建並初始化UI管理器
        uiManager = new UIManager();
        AddChild(uiManager);

        // 連接UI管理器的事件信號
        ConnectUIManagerSignals();

        // 創建卡牌動畫管理器
        cardAnimationManager = new CardAnimationManager();
        AddChild(cardAnimationManager);

        // 連接GameStateManager的事件信號
        ConnectGameStateManagerSignals();

        // 初始化遊戲邏輯
        InitializeGame();
    }





    private void ConnectUIManagerSignals()
    {
        // 連接UI管理器的事件信號
        if (uiManager != null)
        {
            uiManager.OnDrawCardPressed += OnDrawCardPressed;
            uiManager.OnPlayCardPressed += OnPlayCardPressed;
            uiManager.OnUnoPressed += OnUnoPressed;
            uiManager.OnBackToMenuPressed += OnBackToMenuPressed;
            uiManager.OnColorSelected += OnColorSelected;
        }
    }

    private void ConnectGameStateManagerSignals()
    {
        // 連接GameStateManager的事件信號
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

    // GameStateManager事件處理方法
    private void OnGameStateChanged()
    {
        // 遊戲狀態改變時的處理邏輯
        GD.Print("遊戲狀態已改變");
    }

    private void OnPlayerTurnChanged(int playerIndex)
    {
        GD.Print($"玩家回合改變: 玩家 {playerIndex}");
        // 這裡可以添加回合改變時的UI更新邏輯
    }

    private void OnCardPlayed(Card card, int playerIndex)
    {
        string playerName = playerIndex == 0 ? "你" : gameStateManager.GetCurrentPlayerName();
        GD.Print($"{playerName} 打出: {gameStateManager.GetColorText(card.Color)} {card.CardValue}");
        uiManager.AddMessage($"{playerName} 打出: {gameStateManager.GetColorText(card.Color)} {card.CardValue}");
    }

    private void OnCardDrawn(Card card, int playerIndex)
    {
        string playerName = playerIndex == 0 ? "你" : gameStateManager.GetCurrentPlayerName();
        GD.Print($"{playerName} 抽到: {gameStateManager.GetColorText(card.Color)} {card.CardValue}");
        uiManager.AddMessage($"{playerName} 抽到: {gameStateManager.GetColorText(card.Color)} {card.CardValue}");
    }

    private void OnGamePhaseChanged(int newPhase)
    {
        GamePhase phase = (GamePhase)newPhase;
        GD.Print($"遊戲階段改變: {phase}");
        // 這裡可以添加階段改變時的UI更新邏輯
    }

    private void OnSpecialCardEffect(Card card, int effectType)
    {
        CardType cardType = (CardType)effectType;
        GD.Print($"特殊牌效果: {cardType}");
        // 這裡可以添加特殊牌效果時的UI更新邏輯
    }

    private void OnGameOver(int winnerPlayerIndex)
    {
        string winnerName = winnerPlayerIndex == 0 ? "你" : $"玩家{winnerPlayerIndex + 1}";
        GD.Print($"遊戲結束！獲勝者: {winnerName}");
        uiManager.AddMessage($"遊戲結束！獲勝者: {winnerName}");
        // 這裡可以添加遊戲結束時的UI更新邏輯
    }



    private void OnDrawCardPressed()
    {
        GD.Print("抽牌按鈕被按下");

        if (gameStateManager.IsAnimating)
        {
            GD.Print("動畫進行中，忽略抽牌請求");
            return;
        }

        // 檢查抽牌堆是否為空，如果為空則重新洗牌
        if (gameStateManager.DrawPile.Count == 0)
        {
            GD.Print("抽牌堆已空，重新洗牌");
            uiManager.AddMessage("抽牌堆已空，重新洗牌");
            
            // 將棄牌堆的牌（除了頂牌）重新洗牌
            if (gameStateManager.DiscardPile.Count > 1)
            {
                var cardsToShuffle = new List<Card>(gameStateManager.DiscardPile);
                cardsToShuffle.RemoveAt(cardsToShuffle.Count - 1); // 移除頂牌
                
                // 重新洗牌
                var random = new Random();
                for (int i = cardsToShuffle.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = cardsToShuffle[i];
                    cardsToShuffle[i] = cardsToShuffle[j];
                    cardsToShuffle[j] = temp;
                }
                
                // 將洗好的牌放回抽牌堆
                gameStateManager.DrawPile.AddRange(cardsToShuffle);
                
                // 清空棄牌堆（除了頂牌）
                gameStateManager.DiscardPile.Clear();
                gameStateManager.DiscardPile.Add(gameStateManager.CurrentTopCard);
                
                GD.Print($"重新洗牌完成，抽牌堆: {gameStateManager.DrawPile.Count}張");
                uiManager.AddMessage($"重新洗牌完成，抽牌堆: {gameStateManager.DrawPile.Count}張");
            }
        }

        if (gameStateManager.DrawPile.Count > 0)
        {
            // 開始發牌動畫
            StartDrawCardAnimation();
        }
        else
        {
            GD.Print("抽牌堆仍然為空，無法抽牌");
            uiManager.AddMessage("抽牌堆仍然為空，無法抽牌");
        }
    }

    private void OnPlayCardPressed()
    {
        GD.Print("出牌按鈕被按下");

        // 檢查是否有選中的手牌
        if (gameStateManager.SelectedCard != null)
        {
            // 檢查是否可以打出這張牌
            if (gameStateManager.CanPlayCard(gameStateManager.SelectedCard))
            {
                GD.Print($"打出卡牌: {gameStateManager.SelectedCard.Color} {gameStateManager.SelectedCard.CardValue}");
                PlayCard(gameStateManager.SelectedCard);
            }
            else
            {
                GD.Print("這張牌不能打出！");
            }
        }
        else
        {
            GD.Print("請先選擇要打出的手牌");
        }
    }

    private void StartDrawCardAnimation()
    {
        if (gameStateManager.DrawPile.Count == 0) return;

        // 檢查是否啟用動畫
        if (!gameStateManager.EnableAnimations)
        {
            // 如果禁用動畫，直接執行抽牌邏輯
            gameStateManager.DrawCard(0); // 0表示人類玩家
            
            UpdatePlayerHandDisplay();
            UpdateDrawPileDisplay();
            UpdateGameStatusDisplay();
            
            GD.Print("抽牌完成（禁用動畫），輪到下一個玩家");
            NextPlayer();
            return;
        }

        GD.Print("開始發牌動畫...");
        gameStateManager.IsAnimating = true;
        uiManager.SetButtonStates(false, false, true); // 禁用抽牌和出牌按鈕，保持UNO按鈕啟用

        // 從抽牌堆取出一張牌
        var cardToDraw = gameStateManager.DrawPile[0];
        gameStateManager.DrawPile.RemoveAt(0);

        // 創建動畫卡牌實例
        var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
        if (cardScene != null)
        {
            var animatedCard = cardScene.Instantiate<Card>();
            animatedCard.SetCard(cardToDraw.Color, cardToDraw.CardValue, cardToDraw.Type);
            animatedCard.SetCardBack(); // 開始時顯示背面

            // 將動畫卡牌添加到UI層級中
            var uiLayer = GetNode<CanvasLayer>("UILayer");
            uiLayer.AddChild(animatedCard);

            // 設置動畫卡牌的大小和屬性
            animatedCard.Size = new Vector2(80, 120); // 設置固定大小
            animatedCard.Visible = true; // 確保可見
            animatedCard.Modulate = new Color(1, 1, 1, 1); // 確保不透明度為1

            // 設置初始位置（從抽牌堆位置開始）
            Vector2 startPos = Vector2.Zero;
            if (uiManager?.DrawPileUI != null)
            {
                startPos = uiManager.DrawPileUI.GlobalPosition + uiManager.DrawPileUI.Size / 2;
            }
            else
            {
                GD.PrintErr("DrawPileUI 引用為空");
                return;
            }
            animatedCard.GlobalPosition = startPos;
            animatedCard.ZIndex = 1000; // 確保在最上層

            GD.Print($"動畫卡牌初始位置: {startPos}");

            // 計算目標位置（玩家手牌區域的特定位置）
            Vector2 targetPos = Vector2.Zero;
            if (uiManager?.PlayerHandUI != null)
            {
                if (gameStateManager.PlayerHand.Count < gameStateManager.MaxVisibleCards)
                {
                    // 如果手牌少於最大顯示數量，移動到對應的位置
                    float cardWidth = 80.0f;
                    float cardSpacing = 10.0f;
                    float startX = uiManager.PlayerHandUI.GlobalPosition.X + cardWidth / 2;
                    float targetX = startX + (gameStateManager.PlayerHand.Count * (cardWidth + cardSpacing));
                    targetPos = new Vector2(targetX, uiManager.PlayerHandUI.GlobalPosition.Y + uiManager.PlayerHandUI.Size.Y / 2);
                }
                else
                {
                    // 如果手牌已經很多，移動到滾動區域的末尾
                    targetPos = uiManager.PlayerHandUI.GlobalPosition + new Vector2(uiManager.PlayerHandUI.Size.X - 40, uiManager.PlayerHandUI.Size.Y / 2);
                }
            }
            else
            {
                GD.PrintErr("PlayerHandUI 引用為空");
                return;
            }

            GD.Print($"動畫卡牌目標位置: {targetPos}, 當前手牌數量: {gameStateManager.PlayerHand.Count}");

            // 如果目標位置計算有問題，使用固定位置
            if (targetPos.X == 0 || targetPos.Y == 0)
            {
                // 使用屏幕中心作為備用目標位置
                targetPos = new Vector2(GetViewport().GetVisibleRect().Size.X / 2, GetViewport().GetVisibleRect().Size.Y - 100);
                GD.Print($"使用備用目標位置: {targetPos}");
            }

            // 創建動畫
            currentTween = CreateTween();
            currentTween.SetParallel(true);

            // 使用簡化的位置動畫
            float animationDuration = 1.0f * gameStateManager.AnimationSpeed;
            currentTween.TweenProperty(animatedCard, "global_position", targetPos, animationDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);

            GD.Print($"開始動畫: 從 {startPos} 到 {targetPos}, 持續時間: {animationDuration}秒");

            // 旋轉動畫（翻牌效果）
            currentTween.TweenProperty(animatedCard, "rotation", Mathf.Pi, 0.5f)
                .SetDelay(0.3f)
                .SetEase(Tween.EaseType.InOut);

            // 縮放動畫（彈跳效果）
            currentTween.TweenProperty(animatedCard, "scale", Vector2.One * 1.3f, 0.3f)
                .SetEase(Tween.EaseType.Out);
            currentTween.TweenProperty(animatedCard, "scale", Vector2.One * 0.9f, 0.2f)
                .SetDelay(0.3f)
                .SetEase(Tween.EaseType.In);
            currentTween.TweenProperty(animatedCard, "scale", Vector2.One, 0.2f)
                .SetDelay(0.5f)
                .SetEase(Tween.EaseType.Out);

            // 在翻牌時顯示正面
            currentTween.TweenCallback(Callable.From(() =>
            {
                animatedCard.SetCardFront();
                GD.Print("卡牌翻轉到正面");
            })).SetDelay(0.5f);

            // 動畫完成後的回調
            currentTween.TweenCallback(Callable.From(() =>
            {
                GD.Print("發牌動畫完成");
                OnDrawCardAnimationComplete(animatedCard, cardToDraw);
            })).SetDelay(animationDuration);

            // 簡化的軌跡效果（可選）
            // CreateTrailEffect(startPos, targetPos, new Vector2((startPos.X + targetPos.X) / 2, Math.Min(startPos.Y, targetPos.Y) - 100), animationDuration);
        }
        else
        {
            GD.PrintErr("無法載入卡片場景");
        }
    }

    private void CreateTrailEffect(Vector2 startPos, Vector2 targetPos, Vector2 controlPoint, float duration)
    {
        // 創建軌跡線效果
        var trailLine = new Line2D();
        trailLine.Width = 3.0f;
        trailLine.DefaultColor = new Color(1, 1, 1, 0.6f);
        trailLine.ZIndex = 999;

        // 生成軌跡點
        var points = new Vector2[20];
        for (int i = 0; i < 20; i++)
        {
            float t = i / 19.0f;
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            points[i] = uu * startPos + 2 * u * t * controlPoint + tt * targetPos;
        }

        trailLine.Points = points;
        AddChild(trailLine);

        // 軌跡線淡出動畫
        var trailTween = CreateTween();
        trailTween.TweenProperty(trailLine, "modulate:a", 0.0f, 0.5f)
            .SetDelay(duration)
            .SetEase(Tween.EaseType.Out);

        // 動畫完成後移除軌跡線
        trailTween.TweenCallback(Callable.From(() =>
        {
            trailLine.QueueFree();
        })).SetDelay(duration + 0.5f);
    }

    private void OnDrawCardAnimationComplete(Card animatedCard, Card originalCard)
    {
        // 移除動畫卡牌
        animatedCard.QueueFree();

        // 使用GameStateManager的DrawCard方法
        gameStateManager.DrawCard(0); // 0表示人類玩家

        // 更新玩家手牌顯示
        UpdatePlayerHandDisplay();

        // 更新抽牌堆顯示
        UpdateDrawPileDisplay();

        // 重置狀態
        gameStateManager.IsAnimating = false;

        // 更新遊戲狀態顯示
        UpdateGameStatusDisplay();

        GD.Print($"抽牌完成，玩家手牌: {gameStateManager.PlayerHand.Count} 張，抽牌堆剩餘: {gameStateManager.DrawPile.Count} 張");

        // 抽牌後輪到下一個玩家（根據UNO規則）
        GD.Print("抽牌完成，輪到下一個玩家");
        NextPlayer();
    }

    private void UpdatePlayerHandDisplay()
    {
        if (uiManager != null)
        {
            uiManager.UpdatePlayerHandDisplay(gameStateManager.PlayerHand, gameStateManager.SelectedCardIndex, OnPlayerCardClicked);
        }
        else
        {
            GD.PrintErr("UIManager 引用為空，無法更新玩家手牌顯示");
        }
    }

    private void UpdateDrawPileDisplay()
    {
        if (uiManager != null)
        {
            uiManager.UpdateDrawPileDisplay(gameStateManager.DrawPile.Count);
        }
        else
        {
            GD.PrintErr("UIManager 引用為空，無法更新抽牌堆顯示");
        }
    }

    private void OnPlayerCardClicked(Card clickedCard)
    {
        int cardIndex = clickedCard.HandIndex;
        GD.Print($"玩家點擊了手牌: {clickedCard.Color} {clickedCard.CardValue}, 索引: {cardIndex}");

        // 如果點擊的是同一張牌，則取消選中
        if (gameStateManager.SelectedCardIndex == cardIndex)
        {
            GD.Print($"取消選中這張牌，索引: {cardIndex}");
            gameStateManager.SelectedCard = null;
            gameStateManager.SelectedCardIndex = -1; // 重置索引
            // 禁用出牌按鈕
            if (uiManager != null)
            {
                uiManager.SetButtonStates(true, false, true);
            }
            else
            {
                GD.PrintErr("UIManager 引用為空");
            }
            // 更新手牌顯示以重置所有牌的位置
            UpdatePlayerHandDisplay();
            return;
        }

        // 設置選中的手牌
        gameStateManager.SelectedCard = clickedCard;
        gameStateManager.SelectedCardIndex = cardIndex; // 使用牌的索引屬性
        GD.Print($"設置選中的牌: {gameStateManager.SelectedCard.Color} {gameStateManager.SelectedCard.CardValue}, 索引: {gameStateManager.SelectedCardIndex}");

        // 檢查是否可以打出這張牌
        if (gameStateManager.CanPlayCard(clickedCard))
        {
            GD.Print("可以打出這張牌！");
            // 啟用出牌按鈕
            if (uiManager != null)
            {
                uiManager.SetButtonStates(true, true, true);
            }
            else
            {
                GD.PrintErr("UIManager 引用為空");
            }
        }
        else
        {
            GD.Print("這張牌不能打出");
            // 禁用出牌按鈕
            if (uiManager != null)
            {
                uiManager.SetButtonStates(true, false, true);
            }
            else
            {
                GD.PrintErr("UIManager 引用為空");
            }
        }

        // 更新手牌顯示以顯示選中狀態
        UpdatePlayerHandDisplay();
    }

    private void PlayCard(Card cardToPlay)
    {
        GD.Print($"開始出牌: {cardToPlay.Color} {cardToPlay.CardValue}");

        // 開始出牌動畫
        StartPlayCardAnimation(cardToPlay);
    }

    private void StartPlayCardAnimation(Card cardToPlay)
    {
        GD.Print($"開始出牌動畫: {cardToPlay.Color} {cardToPlay.CardValue}");

        // 創建動畫卡牌實例
        var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
        if (cardScene != null)
        {
            var animatedCard = cardScene.Instantiate<Card>();
            animatedCard.SetCard(cardToPlay.Color, cardToPlay.CardValue, cardToPlay.Type);
            animatedCard.SetCardFront();

            // 設置初始位置（從手牌位置開始）
            Vector2 startPos = Vector2.Zero;
            if (uiManager.PlayerHandUI != null && uiManager.PlayerHandUI.GetChildCount() > 0 && gameStateManager.SelectedCardIndex >= 0)
            {
                // 找到選中卡牌在UI中的位置（使用索引來精確定位）
                int uiIndex = 0;
                for (int i = 0; i < uiManager.PlayerHandUI.GetChildCount(); i++)
                {
                    var child = uiManager.PlayerHandUI.GetChild(i);
                    if (child is MarginContainer marginContainer && marginContainer.GetChild(0) is Card uiCard)
                    {
                        // 這是選中的卡牌（包裝在MarginContainer中）
                        if (uiIndex == gameStateManager.SelectedCardIndex)
                        {
                            startPos = uiCard.GlobalPosition;
                            GD.Print($"找到選中卡牌位置（MarginContainer）: {startPos}, 索引: {gameStateManager.SelectedCardIndex}");
                            break;
                        }
                        uiIndex++;
                    }
                    else if (child is Card cardNode)
                    {
                        // 檢查是否是選中的卡牌（使用索引）
                        if (uiIndex == gameStateManager.SelectedCardIndex)
                        {
                            startPos = cardNode.GlobalPosition;
                            GD.Print($"找到選中卡牌位置（Card）: {startPos}, 索引: {gameStateManager.SelectedCardIndex}");
                            break;
                        }
                        uiIndex++;
                    }
                }
            }

            // 如果找不到位置，使用默認位置
            if (startPos == Vector2.Zero)
            {
                startPos = new Vector2(GetViewport().GetVisibleRect().Size.X / 2, GetViewport().GetVisibleRect().Size.Y - 100);
            }

            // 設置目標位置（棄牌堆位置）
            Vector2 targetPos = Vector2.Zero;
            if (uiManager?.DiscardPileUI != null)
            {
                targetPos = uiManager.DiscardPileUI.GlobalPosition;
            }
            else
            {
                // 使用屏幕中心作為備用目標位置
                targetPos = new Vector2(GetViewport().GetVisibleRect().Size.X / 2, GetViewport().GetVisibleRect().Size.Y / 2);
            }

            GD.Print($"出牌動畫: 從 {startPos} 到 {targetPos}");

            // 設置動畫卡牌的位置和屬性
            animatedCard.GlobalPosition = startPos;
            animatedCard.ZIndex = 1000; // 確保在最上層
            animatedCard.Size = new Vector2(80, 120);

            // 添加到場景中
            AddChild(animatedCard);

            // 創建動畫
            var playCardTween = CreateTween();
            playCardTween.SetParallel(true);

            // 位置動畫
            float animationDuration = 0.8f * gameStateManager.AnimationSpeed;
            playCardTween.TweenProperty(animatedCard, "global_position", targetPos, animationDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);

            // 旋轉動畫（翻牌效果）
            playCardTween.TweenProperty(animatedCard, "rotation", Mathf.Pi * 0.1f, animationDuration * 0.3f)
                .SetEase(Tween.EaseType.Out);
            playCardTween.TweenProperty(animatedCard, "rotation", 0, animationDuration * 0.7f)
                .SetDelay(animationDuration * 0.3f)
                .SetEase(Tween.EaseType.In);

            // 縮放動畫（彈跳效果）
            playCardTween.TweenProperty(animatedCard, "scale", Vector2.One * 1.2f, animationDuration * 0.3f)
                .SetEase(Tween.EaseType.Out);
            playCardTween.TweenProperty(animatedCard, "scale", Vector2.One, animationDuration * 0.7f)
                .SetDelay(animationDuration * 0.3f)
                .SetEase(Tween.EaseType.In);

            // 動畫完成後的回調
            playCardTween.TweenCallback(Callable.From(() =>
            {
                GD.Print("出牌動畫完成");
                OnPlayCardAnimationComplete(animatedCard, cardToPlay);
            })).SetDelay(animationDuration);
        }
        else
        {
            GD.PrintErr("無法載入卡片場景");
            // 如果動畫失敗，直接執行出牌邏輯
            ExecutePlayCard(cardToPlay);
        }
    }

    private void OnPlayCardAnimationComplete(Card animatedCard, Card originalCard)
    {
        GD.Print("出牌動畫完成，執行出牌邏輯");

        // 在移除動畫卡牌之前，先保存原始卡牌的信息
        var cardColor = originalCard.Color;
        var cardValue = originalCard.CardValue;
        var cardType = originalCard.Type;

        // 移除動畫卡牌
        animatedCard.QueueFree();

        // 創建一個新的Card對象來執行出牌邏輯
        var cardToPlay = new Card();
        cardToPlay.SetCard(cardColor, cardValue, cardType);

        // 執行實際的出牌邏輯
        ExecutePlayCard(cardToPlay);
    }

    private void ExecutePlayCard(Card cardToPlay)
    {
        GD.Print($"執行出牌邏輯: {cardToPlay.Color} {cardToPlay.CardValue}");

        // 使用GameStateManager的PlayCard方法
        gameStateManager.PlayCard(cardToPlay, 0); // 0表示人類玩家

        // 清空選中的手牌
        gameStateManager.SelectedCard = null;
        gameStateManager.SelectedCardIndex = -1; // 重置索引

        // 更新顯示
        UpdatePlayerHandDisplay();
        UpdateDiscardPileDisplay();

        // 禁用出牌按鈕
        if (uiManager != null)
        {
            uiManager.SetButtonStates(true, false, true);
        }
        else
        {
            GD.PrintErr("UIManager 引用為空");
        }

        // 檢查是否是萬能牌，如果是則顯示顏色選擇面板
        if (cardToPlay.Type == CardType.Wild || cardToPlay.Type == CardType.WildDrawFour)
        {
            ShowColorSelectionPanel();
        }
        else
        {
            // 如果不是萬能牌，直接處理特殊牌效果並輪換到下一個玩家
            HandleSpecialCardEffect(cardToPlay);
            NextPlayer();
        }

        GD.Print($"出牌完成，剩餘手牌: {gameStateManager.PlayerHand.Count} 張");

        // 檢查遊戲是否結束
        if (gameStateManager.IsGameOver())
        {
            GD.Print("遊戲結束！");
            if (uiManager != null)
            {
                uiManager.AddMessage("遊戲結束！");
            }
            else
            {
                GD.PrintErr("UIManager 引用為空");
            }
            // 這裡可以添加遊戲結束的處理邏輯
        }

        // 更新遊戲狀態顯示
        UpdateGameStatusDisplay();
    }

    private void UpdateDiscardPileDisplay()
    {
        if (uiManager != null)
        {
            uiManager.UpdateDiscardPileDisplay(gameStateManager.CurrentTopCard);
        }
        else
        {
            GD.PrintErr("UIManager 引用為空，無法更新棄牌堆顯示");
        }
    }

    private void OnUnoPressed()
    {
        GD.Print("喊UNO!按鈕被按下");
        // 這裡添加喊UNO邏輯
    }

    private void OnBackToMenuPressed()
    {
        GD.Print("返回主選單");
        GetTree().ChangeSceneToFile("res://scenes/main_screen.tscn");
    }

    private void ShowColorSelectionPanel()
    {
        GD.Print("顯示顏色選擇面板");
        
        // 設置遊戲階段為顏色選擇
        gameStateManager.SetGamePhase(GamePhase.ColorSelection);
        gameStateManager.IsWaitingForColorSelection = true;
        
        if (uiManager != null)
        {
            uiManager.ShowColorSelectionPanel();
        }
        else
        {
            GD.PrintErr("UIManager 引用為空");
        }
    }

    private void OnColorSelected(string color)
    {
        GD.Print($"選擇顏色: {color}");

        // 顯示選擇確認
        if (uiManager != null)
        {
            uiManager.SetColorSelectionConfirmed(color);
        }
        else
        {
            GD.PrintErr("UIManager 引用為空");
        }

        // 延遲一下再隱藏面板，讓玩家看到選擇確認
        var delayTween = CreateTween();
        delayTween.TweenCallback(Callable.From(() =>
        {
            // 重置顏色選擇狀態
            gameStateManager.IsWaitingForColorSelection = false;
            gameStateManager.SetGamePhase(GamePhase.Playing);
            
            // 隱藏顏色選擇面板
            uiManager.HideColorSelectionPanel();

            // 注意：按鈕狀態會在 NextPlayer() 方法中正確設置
            // 這裡不需要重新啟用按鈕，因為 NextPlayer() 會根據當前玩家設置正確的按鈕狀態

            // 更新當前頂牌的顏色（萬能牌會改變顏色）
            if (gameStateManager.CurrentTopCard != null && (gameStateManager.CurrentTopCard.Type == CardType.Wild || gameStateManager.CurrentTopCard.Type == CardType.WildDrawFour))
            {
                // 創建一個新的頂牌實例，使用選擇的顏色
                var newTopCard = new Card();
                newTopCard.SetCard(GetCardColorFromString(color), gameStateManager.CurrentTopCard.CardValue, gameStateManager.CurrentTopCard.Type);

                // 更新棄牌堆中的頂牌
                if (gameStateManager.DiscardPile.Count > 0)
                {
                    gameStateManager.DiscardPile[gameStateManager.DiscardPile.Count - 1] = newTopCard;
                }
                gameStateManager.CurrentTopCard = newTopCard;

                // 更新顯示
                UpdateDiscardPileDisplay();

                GD.Print($"萬能牌顏色已更改為: {color}");

                // 更新遊戲狀態顯示
                UpdateGameStatusDisplay();

                // 處理特殊牌效果並輪換到下一個玩家
                HandleSpecialCardEffect(gameStateManager.CurrentTopCard);
                NextPlayer();
            }
        })).SetDelay(1.0f); // 延遲1秒
    }

    private void InitializeGame()
    {
        GD.Print($"初始化UNO遊戲... 遊玩人數: {gameStateManager.PlayerCount}人");

        // 設置遊戲階段為初始化
        gameStateManager.SetGamePhase(GamePhase.Initializing);

        // 創建電腦玩家
        CreateComputerPlayers();

        // 創建完整的UNO牌組
        CreateDeck();

        // 洗牌
        ShuffleDeck();

        // 設置第一張頂牌
        SetFirstTopCard();

        // 顯示抽牌堆背面
        ShowDrawPileBack();

        // 設置遊戲階段為發牌
        gameStateManager.SetGamePhase(GamePhase.Dealing);

        // 通過動畫發放初始手牌
        StartInitialDealAnimation();

        // 初始化按鈕狀態
        uiManager.SetButtonStates(false, false, false); // 初始時禁用所有按鈕

        // 隱藏顏色選擇面板
        uiManager.HideColorSelectionPanel();

        // 隨機選擇第一個玩家（0-3，其中0是人類玩家）
        var random = new Random();
        gameStateManager.CurrentPlayerIndex = random.Next(0, gameStateManager.PlayerCount);
        GD.Print($"隨機選擇第一個玩家: {GetCurrentPlayerName()}");

        GD.Print($"初始化完成 - 抽牌堆: {gameStateManager.DrawPile.Count}張, 頂牌: 1張, 電腦玩家: {gameStateManager.ComputerPlayers.Count}個");

        // 更新遊戲狀態顯示
        UpdateGameStatusDisplay();

        // 根據第一個玩家設置按鈕狀態
        if (gameStateManager.CurrentPlayerIndex == 0)
        {
            // 如果是人類玩家，啟用按鈕
            uiManager.SetButtonStates(true, false, true); // 啟用抽牌和UNO按鈕，禁用出牌按鈕
            GD.Print("初始化：啟用人類玩家按鈕");
        }
        else
        {
            // 如果是電腦玩家，禁用按鈕
            uiManager.SetButtonStates(false, false, false);
            GD.Print("初始化：禁用按鈕（電腦玩家回合）");
        }
    }

    private void CreateComputerPlayers()
    {
        GD.Print($"創建電腦玩家... 總人數: {gameStateManager.PlayerCount}人");

        // 清空現有電腦玩家
        gameStateManager.ComputerPlayers.Clear();

        // 創建電腦玩家（總人數減去1個人類玩家）
        for (int i = 1; i < gameStateManager.PlayerCount; i++)
        {
            var computerPlayer = new ComputerPlayer(i);
            gameStateManager.ComputerPlayers.Add(computerPlayer);
            GD.Print($"創建電腦玩家: {computerPlayer.PlayerName}");
        }

        GD.Print($"電腦玩家創建完成，共 {gameStateManager.ComputerPlayers.Count} 個電腦玩家");
    }

    private void UpdateGameStatusDisplay()
    {
        string currentPlayerName = GetCurrentPlayerName();
        string colorText = gameStateManager.CurrentTopCard != null ? GetColorText(gameStateManager.CurrentTopCard.Color) : "無";
        string allPlayersCardsInfo = GetAllPlayersCardsInfo();
        
        uiManager.UpdateGameStatusDisplay(currentPlayerName, colorText, allPlayersCardsInfo);
    }

    private string GetCurrentPlayerName()
    {
        return gameStateManager.GetCurrentPlayerName();
    }

    private string GetAllPlayersCardsInfo()
    {
        return gameStateManager.GetAllPlayersCardsInfo();
    }

    private void NextPlayer()
    {
        // 使用GameStateManager的NextPlayer方法
        gameStateManager.NextPlayer();

        // 更新UI顯示
        UpdateGameStatusDisplay();

        // 如果是電腦玩家的回合，執行電腦玩家的行動
        if (gameStateManager.CurrentPlayerIndex > 0)
        {
            GD.Print($"開始執行電腦玩家回合，currentPlayerIndex: {gameStateManager.CurrentPlayerIndex}");
            GD.Print("禁用人類玩家按鈕");
            // 禁用人類玩家的按鈕
            uiManager.SetButtonStates(false, false, false);
            GD.Print("人類玩家按鈕已禁用");

            ExecuteComputerPlayerTurn();
        }
        else
        {
            GD.Print("輪換到人類玩家回合");
            GD.Print("啟用人類玩家按鈕");
            // 啟用人類玩家的按鈕
            uiManager.SetButtonStates(true, false, true); // 啟用抽牌和UNO按鈕，禁用出牌按鈕
            GD.Print("人類玩家按鈕已啟用");
        }
    }

    private void NextPlayerWithoutComputerTurn()
    {
        // 使用GameStateManager的NextPlayerWithoutComputerTurn方法
        gameStateManager.NextPlayerWithoutComputerTurn();

        // 更新UI顯示
        UpdateGameStatusDisplay();
    }

    private void ExecuteComputerPlayerTurn()
    {
        GD.Print($"ExecuteComputerPlayerTurn 被調用，currentPlayerIndex: {gameStateManager.CurrentPlayerIndex}");
        int computerPlayerIndex = gameStateManager.CurrentPlayerIndex - 1;
        GD.Print($"計算的電腦玩家索引: {computerPlayerIndex}, 電腦玩家數量: {gameStateManager.ComputerPlayers.Count}");
        
        if (computerPlayerIndex < gameStateManager.ComputerPlayers.Count)
        {
            var computerPlayer = gameStateManager.ComputerPlayers[computerPlayerIndex];
            GD.Print($"電腦玩家 {computerPlayer.PlayerName} 的回合開始");
            uiManager.AddMessage($"{computerPlayer.PlayerName} 的回合開始");
            GD.Print($"電腦玩家手牌數量: {computerPlayer.Hand.Count}");
            GD.Print($"當前頂牌: {gameStateManager.CurrentTopCard?.Color} {gameStateManager.CurrentTopCard?.CardValue}");

            // 檢查電腦玩家是否有可以打出的牌
            Card cardToPlay = computerPlayer.ChooseCardToPlay(gameStateManager.CurrentTopCard);

            if (cardToPlay != null)
            {
                GD.Print($"電腦玩家 {computerPlayer.PlayerName} 打出: {cardToPlay.Color} {cardToPlay.CardValue}");
                uiManager.AddMessage($"{computerPlayer.PlayerName} 打出: {GetColorText(cardToPlay.Color)} {cardToPlay.CardValue}");

                // 使用GameStateManager的PlayCard方法
                gameStateManager.PlayCard(cardToPlay, gameStateManager.CurrentPlayerIndex);

                // 更新顯示
                UpdateDiscardPileDisplay();
                UpdateGameStatusDisplay();

                // 檢查是否是萬能牌，如果是則自動選擇顏色
                if (cardToPlay.Type == CardType.Wild || cardToPlay.Type == CardType.WildDrawFour)
                {
                    GD.Print($"電腦玩家 {computerPlayer.PlayerName} 打出萬能牌，自動選擇顏色");
                    uiManager.AddMessage($"{computerPlayer.PlayerName} 打出萬能牌，自動選擇顏色");

                    // 電腦玩家自動選擇顏色
                    CardColor chosenColor = computerPlayer.ChooseColor();
                    GD.Print($"電腦玩家選擇顏色: {chosenColor}");
                    uiManager.AddMessage($"{computerPlayer.PlayerName} 選擇顏色: {GetColorText(chosenColor)}");

                    // 更新當前頂牌的顏色
                    gameStateManager.CurrentTopCard.Color = chosenColor;
                    UpdateDiscardPileDisplay();
                    UpdateGameStatusDisplay();

                    // 處理特殊牌效果
                    HandleSpecialCardEffect(cardToPlay);
                }
                else
                {
                    // 處理特殊牌效果
                    HandleSpecialCardEffect(cardToPlay);

                    // 如果不是特殊牌，輪換到下一個玩家
                    if (cardToPlay.Type == CardType.Number)
                    {
                        GD.Print("電腦玩家出普通牌，輪換到下一個玩家");
                        uiManager.AddMessage($"{computerPlayer.PlayerName} 出普通牌，輪換到下一個玩家");
                        NextPlayer();
                    }
                }
            }
            else
            {
                GD.Print($"電腦玩家 {computerPlayer.PlayerName} 沒有可以打出的牌，抽一張牌");
                uiManager.AddMessage($"{computerPlayer.PlayerName} 沒有可以打出的牌，抽一張牌");

                // 檢查抽牌堆是否為空，如果為空則重新洗牌
                if (gameStateManager.DrawPile.Count == 0)
                {
                    GD.Print("抽牌堆已空，重新洗牌");
                    uiManager.AddMessage("抽牌堆已空，重新洗牌");
                    
                    // 將棄牌堆的牌（除了頂牌）重新洗牌
                    if (gameStateManager.DiscardPile.Count > 1)
                    {
                        var cardsToShuffle = new List<Card>(gameStateManager.DiscardPile);
                        cardsToShuffle.RemoveAt(cardsToShuffle.Count - 1); // 移除頂牌
                        
                        // 重新洗牌
                        var random = new Random();
                        for (int i = cardsToShuffle.Count - 1; i > 0; i--)
                        {
                            int j = random.Next(i + 1);
                            var temp = cardsToShuffle[i];
                            cardsToShuffle[i] = cardsToShuffle[j];
                            cardsToShuffle[j] = temp;
                        }
                        
                        // 將洗好的牌放回抽牌堆
                        gameStateManager.DrawPile.AddRange(cardsToShuffle);
                        
                        // 清空棄牌堆（除了頂牌）
                        gameStateManager.DiscardPile.Clear();
                        gameStateManager.DiscardPile.Add(gameStateManager.CurrentTopCard);
                        
                        GD.Print($"重新洗牌完成，抽牌堆: {gameStateManager.DrawPile.Count}張");
                        uiManager.AddMessage($"重新洗牌完成，抽牌堆: {gameStateManager.DrawPile.Count}張");
                    }
                }

                // 電腦玩家抽一張牌
                if (gameStateManager.DrawPile.Count > 0)
                {
                    // 使用GameStateManager的DrawCard方法
                    var drawnCard = gameStateManager.DrawCard(gameStateManager.CurrentPlayerIndex);
                    
                    if (drawnCard != null)
                    {
                        GD.Print($"電腦玩家 {computerPlayer.PlayerName} 抽到: {drawnCard.Color} {drawnCard.CardValue}");
                        uiManager.AddMessage($"{computerPlayer.PlayerName} 抽到: {GetColorText(drawnCard.Color)} {drawnCard.CardValue}");
                    }
                }
                else
                {
                    GD.Print("抽牌堆仍然為空，無法抽牌");
                    uiManager.AddMessage("抽牌堆仍然為空，無法抽牌");
                }

                // 輪換到下一個玩家
                NextPlayer();
            }
        }
        else
        {
            GD.PrintErr($"電腦玩家索引超出範圍: {computerPlayerIndex}, 電腦玩家數量: {gameStateManager.ComputerPlayers.Count}");
            uiManager.AddMessage($"錯誤：電腦玩家索引超出範圍");
            // 如果索引超出範圍，輪換到人類玩家
            gameStateManager.CurrentPlayerIndex = 0;
            NextPlayer();
        }
    }

    private void HandleSpecialCardEffect(Card card)
    {
        // 使用GameStateManager的HandleSpecialCardEffect方法
        gameStateManager.HandleSpecialCardEffect(card);
        
        // 添加訊息到UI
        switch (card.Type)
        {
            case CardType.Skip:
                uiManager.AddMessage("跳過下一個玩家的回合");
                break;
            case CardType.Reverse:
                uiManager.AddMessage("遊戲方向改變");
                break;
            case CardType.DrawTwo:
                uiManager.AddMessage("下一個玩家抽兩張牌");
                break;
            case CardType.Wild:
                uiManager.AddMessage("萬能牌，輪換到下一個玩家");
                break;
            case CardType.WildDrawFour:
                uiManager.AddMessage("下一個玩家抽四張牌");
                break;
        }
    }

    private void DrawTwoCardsForCurrentPlayer()
    {
        // 使用GameStateManager的DrawTwoCardsForCurrentPlayer方法
        gameStateManager.DrawTwoCardsForCurrentPlayer();
        
        // 添加訊息到UI
        if (gameStateManager.CurrentPlayerIndex == 0)
        {
            uiManager.AddMessage("你抽了兩張牌");
        }
        else
        {
            int computerPlayerIndex = gameStateManager.CurrentPlayerIndex - 1;
            if (computerPlayerIndex < gameStateManager.ComputerPlayers.Count)
            {
                var computerPlayer = gameStateManager.ComputerPlayers[computerPlayerIndex];
                uiManager.AddMessage($"{computerPlayer.PlayerName} 抽了兩張牌");
            }
        }
        
        // 更新UI顯示
        UpdatePlayerHandDisplay();
        UpdateGameStatusDisplay();
    }

    private void DrawFourCardsForCurrentPlayer()
    {
        // 使用GameStateManager的DrawFourCardsForCurrentPlayer方法
        gameStateManager.DrawFourCardsForCurrentPlayer();
        
        // 添加訊息到UI
        if (gameStateManager.CurrentPlayerIndex == 0)
        {
            uiManager.AddMessage("你抽了四張牌");
        }
        else
        {
            int computerPlayerIndex = gameStateManager.CurrentPlayerIndex - 1;
            if (computerPlayerIndex < gameStateManager.ComputerPlayers.Count)
            {
                var computerPlayer = gameStateManager.ComputerPlayers[computerPlayerIndex];
                uiManager.AddMessage($"{computerPlayer.PlayerName} 抽了四張牌");
            }
        }
        
        // 更新UI顯示
        UpdatePlayerHandDisplay();
        UpdateGameStatusDisplay();
    }

    private void UpdatePlayerCardCounts()
    {
        gameStateManager.UpdatePlayerCardCounts();
    }

    private string GetColorText(CardColor color)
    {
        return gameStateManager.GetColorText(color);
    }

    // 安全地調用UIManager方法的輔助方法
    private void SafeUIManagerCall(Action<UIManager> action, string operationName)
    {
        if (uiManager != null)
        {
            try
            {
                action(uiManager);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"UIManager {operationName} 調用失敗: {ex.Message}");
            }
        }
        else
        {
            GD.PrintErr($"UIManager 引用為空，無法執行 {operationName}");
        }
    }








    private void StartInitialDealAnimation()
    {
        GD.Print("開始發放初始手牌動畫...");

        // 清空玩家手牌（如果有的話）
        gameStateManager.PlayerHand.Clear();

        // 清空電腦玩家手牌
        foreach (var computerPlayer in gameStateManager.ComputerPlayers)
        {
            computerPlayer.Hand.Clear();
        }

        // 開始發放初始手牌（人類玩家）
        DealInitialCardWithAnimation(0, true);
    }

    private void DealComputerPlayersCards()
    {
        GD.Print("開始為電腦玩家發放初始手牌...");

        // 為每個電腦玩家發放7張牌
        for (int playerIndex = 0; playerIndex < gameStateManager.ComputerPlayers.Count; playerIndex++)
        {
            var computerPlayer = gameStateManager.ComputerPlayers[playerIndex];
            GD.Print($"為電腦玩家 {computerPlayer.PlayerName} 發放初始手牌...");

            // 發放初始手牌給這個電腦玩家
            for (int cardIndex = 0; cardIndex < gameStateManager.InitialCardsPerPlayer; cardIndex++)
            {
                if (gameStateManager.DrawPile.Count > 0)
                {
                    var cardToDraw = gameStateManager.DrawPile[0];
                    gameStateManager.DrawPile.RemoveAt(0);
                    computerPlayer.Hand.Add(cardToDraw);
                    GD.Print($"電腦玩家 {computerPlayer.PlayerName} 獲得第 {cardIndex + 1} 張牌: {cardToDraw.Color} {cardToDraw.CardValue}");
                }
                else
                {
                    GD.PrintErr("抽牌堆已空，無法為電腦玩家發放更多牌");
                    break;
                }
            }

            GD.Print($"電腦玩家 {computerPlayer.PlayerName} 初始手牌發放完成，手牌: {computerPlayer.Hand.Count} 張");
        }

        GD.Print("所有電腦玩家初始手牌發放完成");

        // 開始遊戲
        UpdateGameStatusDisplay();
        // 設置遊戲階段為遊戲中
        gameStateManager.SetGamePhase(GamePhase.Playing);

        GD.Print("遊戲初始化完成，可以開始遊戲");

        // 如果第一個玩家是電腦玩家，開始電腦玩家回合
        if (gameStateManager.CurrentPlayerIndex > 0)
        {
            GD.Print("第一個玩家是電腦玩家，開始電腦玩家回合");
            NextPlayer();
        }
    }

    private void DealInitialCardWithAnimation(int cardIndex, bool isHumanPlayer = true)
    {
        if (cardIndex >= gameStateManager.InitialCardsPerPlayer || gameStateManager.DrawPile.Count == 0)
        {
            if (isHumanPlayer)
            {
                GD.Print($"人類玩家初始手牌發放完成，手牌: {gameStateManager.PlayerHand.Count} 張");
                // 開始為電腦玩家發牌
                DealComputerPlayersCards();
            }
            else
            {
                GD.Print($"電腦玩家初始手牌發放完成");
            }
            return;
        }

        GD.Print($"開始發放第 {cardIndex + 1} 張初始手牌...");

        // 從抽牌堆取出一張牌
        var cardToDraw = gameStateManager.DrawPile[0];
        gameStateManager.DrawPile.RemoveAt(0);

        // 計算起始位置（從抽牌堆位置開始）
        Vector2 startPos = Vector2.Zero;
        if (uiManager?.DrawPileUI != null)
        {
            startPos = uiManager.DrawPileUI.GlobalPosition + uiManager.DrawPileUI.Size / 2;
        }
        else
        {
            GD.PrintErr("DrawPileUI 引用為空");
            return;
        }

        // 計算目標位置（玩家手牌區域的特定位置）
        Vector2 targetPos = Vector2.Zero;
        if (uiManager?.PlayerHandUI != null)
        {
            float cardWidth = 80.0f;
            float cardSpacing = 10.0f;
            float startX = uiManager.PlayerHandUI.GlobalPosition.X + cardWidth / 2;
            float targetX = startX + (cardIndex * (cardWidth + cardSpacing));
            targetPos = new Vector2(targetX, uiManager.PlayerHandUI.GlobalPosition.Y + uiManager.PlayerHandUI.Size.Y / 2);
        }
        else
        {
            GD.PrintErr("PlayerHandUI 引用為空");
            return;
        }

        // 如果目標位置計算有問題，使用固定位置
        if (targetPos.X == 0 || targetPos.Y == 0)
        {
            targetPos = new Vector2(GetViewport().GetVisibleRect().Size.X / 2, GetViewport().GetVisibleRect().Size.Y - 100);
            GD.Print($"使用備用目標位置: {targetPos}");
        }

        // 使用CardAnimationManager創建動畫
        if (cardAnimationManager != null)
        {
            cardAnimationManager.CreateDealCardAnimation(cardToDraw, startPos, targetPos, cardIndex, () =>
            {
                // 動畫完成後的回調
                // 將牌添加到玩家手牌
                gameStateManager.PlayerHand.Add(cardToDraw);

                // 更新玩家手牌顯示
                UpdatePlayerHandDisplay();

                // 更新抽牌堆顯示
                UpdateDrawPileDisplay();

                // 延遲一下再發放下一張牌，讓動畫更自然
                var delayTween = CreateTween();
                delayTween.TweenCallback(Callable.From(() =>
                {
                    DealInitialCardWithAnimation(cardIndex + 1);
                })).SetDelay(0.2f);
            });
        }
        else
        {
            GD.PrintErr("CardAnimationManager 未初始化");
            // 備用方案：直接添加牌到手牌
            gameStateManager.PlayerHand.Add(cardToDraw);
            UpdatePlayerHandDisplay();
            UpdateDrawPileDisplay();
            
            // 繼續發下一張牌
            DealInitialCardWithAnimation(cardIndex + 1);
        }
    }

    private void ShowDrawPileBack()
    {
        uiManager.UpdateDrawPileDisplay(gameStateManager.DrawPile.Count);
        GD.Print("抽牌堆堆疊效果已創建");
    }

    private void CreateDeck()
    {
        GD.Print("創建UNO牌組...");

        // 創建數字牌 (0-9, 每種顏色各2張，除了0只有1張)
        string[] colors = { "red", "blue", "green", "yellow" };

        foreach (string color in colors)
        {
            GD.Print($"創建 {color} 顏色的牌...");

            // 數字0 (每種顏色1張)
            CreateCard(GetCardColorFromString(color), "0", CardType.Number);

            // 數字1-9 (每種顏色2張)
            for (int i = 1; i <= 9; i++)
            {
                CreateCard(GetCardColorFromString(color), i.ToString(), CardType.Number);
                CreateCard(GetCardColorFromString(color), i.ToString(), CardType.Number);
            }

            // 特殊牌 (每種顏色2張)
            CreateCard(GetCardColorFromString(color), "", CardType.Skip);
            CreateCard(GetCardColorFromString(color), "", CardType.Skip);
            CreateCard(GetCardColorFromString(color), "", CardType.Reverse);
            CreateCard(GetCardColorFromString(color), "", CardType.Reverse);
            CreateCard(GetCardColorFromString(color), "", CardType.DrawTwo);
            CreateCard(GetCardColorFromString(color), "", CardType.DrawTwo);

            GD.Print($"{color} 顏色牌創建完成，當前牌組: {gameStateManager.DrawPile.Count} 張");
        }

        // 萬能牌 (4張)
        GD.Print("創建萬能牌...");
        for (int i = 0; i < 4; i++)
        {
            CreateCard(CardColor.Red, "", CardType.Wild);
        }

        // 萬能牌+4 (4張)
        GD.Print("創建萬能牌+4...");
        for (int i = 0; i < 4; i++)
        {
            CreateCard(CardColor.Red, "", CardType.WildDrawFour);
        }

        GD.Print($"牌組創建完成，共 {gameStateManager.DrawPile.Count} 張牌");
    }

    private void CreateCard(CardColor color, string value, CardType type)
    {
        // 載入卡片場景
        var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
        if (cardScene != null)
        {
            var cardInstance = cardScene.Instantiate<Card>();
            cardInstance.SetCard(color, value, type);
            gameStateManager.DrawPile.Add(cardInstance);
        }
        else
        {
            GD.PrintErr("無法載入卡片場景: res://scenes/card.tscn");
        }
    }

    private CardColor GetCardColorFromString(string colorString)
    {
        return gameStateManager.GetCardColorFromString(colorString);
    }

    private void ShuffleDeck()
    {
        GD.Print("洗牌中...");
        var random = new Random();

        // Fisher-Yates 洗牌算法
        for (int i = gameStateManager.DrawPile.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (gameStateManager.DrawPile[i], gameStateManager.DrawPile[j]) = (gameStateManager.DrawPile[j], gameStateManager.DrawPile[i]);
        }

        GD.Print("洗牌完成");
    }



    private void SetFirstTopCard()
    {
        GD.Print("設置第一張頂牌...");

        // 找到第一張非萬能牌的牌作為頂牌
        Card firstCard = null;
        for (int i = 0; i < gameStateManager.DrawPile.Count; i++)
        {
            if (gameStateManager.DrawPile[i].Type != CardType.Wild && gameStateManager.DrawPile[i].Type != CardType.WildDrawFour)
            {
                firstCard = gameStateManager.DrawPile[i];
                gameStateManager.DrawPile.RemoveAt(i);
                break;
            }
        }

        if (firstCard != null)
        {
            gameStateManager.CurrentTopCard = firstCard;
            gameStateManager.DiscardPile.Add(firstCard);

            // 顯示頂牌
            if (uiManager.DiscardPileUI != null)
            {
                // 清除現有的子節點
                foreach (Node child in uiManager.DiscardPileUI.GetChildren())
                {
                    child.QueueFree();
                }

                // 創建頂牌實例
                var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
                if (cardScene != null)
                {
                    var topCardInstance = cardScene.Instantiate<Card>();
                    topCardInstance.SetCard(firstCard.Color, firstCard.CardValue, firstCard.Type);
                    topCardInstance.SetCardFront(); // 確保顯示正面

                    // 將頂牌添加到UI
                    uiManager.DiscardPileUI.AddChild(topCardInstance);

                    // 為頂牌添加邊框效果
                    var topCardBorder = new ColorRect();
                    topCardBorder.Color = new Color(1, 1, 1, 0.9f); // 白色邊框，更接近真實卡牌
                    topCardBorder.Size = topCardInstance.Size + new Vector2(4, 4);
                    topCardBorder.Position = topCardInstance.Position - new Vector2(2, 2);
                    topCardBorder.ZIndex = topCardInstance.ZIndex - 1;

                    // 添加圓角效果
                    var topCardBorderStyle = new StyleBoxFlat();
                    topCardBorderStyle.BgColor = topCardBorder.Color;
                    topCardBorderStyle.CornerRadiusTopLeft = 12;
                    topCardBorderStyle.CornerRadiusTopRight = 12;
                    topCardBorderStyle.CornerRadiusBottomLeft = 12;
                    topCardBorderStyle.CornerRadiusBottomRight = 12;
                    topCardBorder.AddThemeStyleboxOverride("panel", topCardBorderStyle);

                    uiManager.DiscardPileUI.AddChild(topCardBorder);
                }
            }

            GD.Print($"頂牌設置為: {firstCard.Color} {firstCard.CardValue}");
        }
        else
        {
            GD.Print("警告: 沒有找到合適的頂牌");
        }
    }

    public override void _Input(InputEvent @event)
    {
        // 處理輸入事件
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                GD.Print("返回主選單");
                GetTree().ChangeSceneToFile("res://scenes/main_screen.tscn");
            }
        }
    }
}