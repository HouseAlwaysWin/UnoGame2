using Godot;
using System;
using System.Collections.Generic;

public partial class MainGame : Node2D
{
    private Button drawCardButton;
    private Button unoButton;
    private Button backToMenuButton;
    private Button playCardButton; // 新增出牌按鈕
    private Panel colorSelectionPanel;
    private Button redButton, blueButton, greenButton, yellowButton;
    
    // 遊戲狀態
    private List<Card> drawPile = new List<Card>(); // 抽牌堆
    private List<Card> discardPile = new List<Card>(); // 棄牌堆
    private List<Card> playerHand = new List<Card>(); // 玩家手牌
    private Card currentTopCard; // 當前頂牌
    
    // UI 引用
    private TextureRect drawPileUI;
    private TextureRect discardPileUI;
    private HBoxContainer playerHandUI;
    
    // 手牌滾動相關
    private Button leftScrollButton;
    private Button rightScrollButton;
    private ScrollContainer playerHandScrollContainer;
    private int currentHandScrollIndex = 0;
    private int maxVisibleCards = 8; // 最多顯示8張牌
    
    // 出牌相關
    private Card selectedCard = null; // 當前選中的手牌
    
    // 動畫相關
    private bool isAnimating = false;
    private Tween currentTween;
    
    public override void _Ready()
    {
        GD.Print("主遊戲場景已加載");
        
        // 獲取UI元素引用
        GetUIReferences();
        
        // 連接按鈕信號
        ConnectButtonSignals();
        
        // 初始化遊戲邏輯
        InitializeGame();
    }

    private void GetUIReferences()
    {
        // 獲取按鈕引用
        drawCardButton = GetNode<Button>("UILayer/UI/ActionButtons/DrawCardButton");
        unoButton = GetNode<Button>("UILayer/UI/ActionButtons/UnoButton");
        backToMenuButton = GetNode<Button>("UILayer/UI/TopPanel/GameInfo/BackToMenuButton");
        playCardButton = GetNode<Button>("UILayer/UI/ActionButtons/PlayCardButton"); // 獲取出牌按鈕
        
        // 獲取顏色選擇面板和按鈕
        colorSelectionPanel = GetNode<Panel>("UILayer/UI/ColorSelectionPanel");
        redButton = GetNode<Button>("UILayer/UI/ColorSelectionPanel/ColorButtons/RedButton");
        blueButton = GetNode<Button>("UILayer/UI/ColorSelectionPanel/ColorButtons/BlueButton");
        greenButton = GetNode<Button>("UILayer/UI/ColorSelectionPanel/ColorButtons/GreenButton");
        yellowButton = GetNode<Button>("UILayer/UI/ColorSelectionPanel/ColorButtons/YellowButton");
        
        // 獲取卡牌顯示區域引用
        drawPileUI = GetNode<TextureRect>("UILayer/UI/DrawPile");
        discardPileUI = GetNode<TextureRect>("UILayer/UI/CenterArea/DiscardPile");
        playerHandUI = GetNode<HBoxContainer>("UILayer/UI/PlayerHand/ScrollContainer/CardsContainer");
        
        // 獲取手牌滾動按鈕和容器
        leftScrollButton = GetNode<Button>("UILayer/UI/PlayerHand/LeftScrollButton");
        rightScrollButton = GetNode<Button>("UILayer/UI/PlayerHand/RightScrollButton");
        playerHandScrollContainer = GetNode<ScrollContainer>("UILayer/UI/PlayerHand/ScrollContainer");
    }
    


    private void ConnectButtonSignals()
    {
        // 連接動作按鈕
        if (drawCardButton != null)
            drawCardButton.Pressed += OnDrawCardPressed;
        
        if (playCardButton != null)
            playCardButton.Pressed += OnPlayCardPressed;
        
        if (unoButton != null)
            unoButton.Pressed += OnUnoPressed;
        
        if (backToMenuButton != null)
            backToMenuButton.Pressed += OnBackToMenuPressed;
        
        // 連接顏色選擇按鈕
        if (redButton != null)
            redButton.Pressed += () => OnColorSelected("紅色");
        
        if (blueButton != null)
            blueButton.Pressed += () => OnColorSelected("藍色");
        
        if (greenButton != null)
            greenButton.Pressed += () => OnColorSelected("綠色");
        
        if (yellowButton != null)
            yellowButton.Pressed += () => OnColorSelected("黃色");
        
        // 連接滾動按鈕
        if (leftScrollButton != null)
            leftScrollButton.Pressed += OnLeftScrollPressed;
        
        if (rightScrollButton != null)
            rightScrollButton.Pressed += OnRightScrollPressed;
    }

    private void OnDrawCardPressed()
    {
        GD.Print("抽牌按鈕被按下");
        
        if (isAnimating)
        {
            GD.Print("動畫進行中，忽略抽牌請求");
            return;
        }
        
        if (drawPile.Count > 0)
        {
            // 開始發牌動畫
            StartDrawCardAnimation();
        }
        else
        {
            GD.Print("抽牌堆已空！");
        }
    }
    
    private void OnPlayCardPressed()
    {
        GD.Print("出牌按鈕被按下");
        
        // 檢查是否有選中的手牌
        if (selectedCard != null)
        {
            // 檢查是否可以打出這張牌
            if (currentTopCard != null && selectedCard.CanPlayOn(currentTopCard))
            {
                GD.Print($"打出卡牌: {selectedCard.Color} {selectedCard.CardValue}");
                PlayCard(selectedCard);
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
        if (drawPile.Count == 0) return;
        
        GD.Print("開始發牌動畫...");
        isAnimating = true;
        drawCardButton.Disabled = true; // 禁用按鈕防止重複點擊
        
        // 從抽牌堆取出一張牌
        var cardToDraw = drawPile[0];
        drawPile.RemoveAt(0);
        
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
            Vector2 startPos = drawPileUI.GlobalPosition + drawPileUI.Size / 2;
            animatedCard.GlobalPosition = startPos;
            animatedCard.ZIndex = 1000; // 確保在最上層
            
            GD.Print($"動畫卡牌初始位置: {startPos}");
            
            // 計算目標位置（玩家手牌區域的特定位置）
            Vector2 targetPos;
            if (playerHand.Count < maxVisibleCards)
            {
                // 如果手牌少於最大顯示數量，移動到對應的位置
                float cardWidth = 80.0f;
                float cardSpacing = 10.0f;
                float startX = playerHandUI.GlobalPosition.X + cardWidth / 2;
                float targetX = startX + (playerHand.Count * (cardWidth + cardSpacing));
                targetPos = new Vector2(targetX, playerHandUI.GlobalPosition.Y + playerHandUI.Size.Y / 2);
            }
            else
            {
                // 如果手牌已經很多，移動到滾動區域的末尾
                targetPos = playerHandUI.GlobalPosition + new Vector2(playerHandUI.Size.X - 40, playerHandUI.Size.Y / 2);
            }
            
            GD.Print($"動畫卡牌目標位置: {targetPos}, 當前手牌數量: {playerHand.Count}");
            
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
            float animationDuration = 1.0f;
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
            currentTween.TweenCallback(Callable.From(() => {
                animatedCard.SetCardFront();
                GD.Print("卡牌翻轉到正面");
            })).SetDelay(0.5f);
            
            // 動畫完成後的回調
            currentTween.TweenCallback(Callable.From(() => {
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
        trailTween.TweenCallback(Callable.From(() => {
            trailLine.QueueFree();
        })).SetDelay(duration + 0.5f);
    }
    
    private void OnDrawCardAnimationComplete(Card animatedCard, Card originalCard)
    {
        // 移除動畫卡牌
        animatedCard.QueueFree();
        
        // 將牌添加到玩家手牌
        playerHand.Add(originalCard);
        
        // 更新玩家手牌顯示
        UpdatePlayerHandDisplay();
        
        // 更新抽牌堆顯示
        UpdateDrawPileDisplay();
        
        // 重置狀態
        isAnimating = false;
        drawCardButton.Disabled = false;
        
        GD.Print($"抽牌完成，玩家手牌: {playerHand.Count} 張，抽牌堆剩餘: {drawPile.Count} 張");
    }
    
    private void UpdatePlayerHandDisplay()
    {
        GD.Print($"更新玩家手牌顯示，當前手牌數量: {playerHand.Count}");
        
        // 清除現有的手牌顯示
        foreach (Node child in playerHandUI.GetChildren())
        {
            if (child is Card)
            {
                child.QueueFree();
            }
        }
        
        GD.Print($"清除了現有的手牌元素");
        
        // 顯示玩家手牌
        for (int i = 0; i < playerHand.Count; i++)
        {
            var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
            if (cardScene != null)
            {
                var cardInstance = cardScene.Instantiate<Card>();
                var card = playerHand[i];
                cardInstance.SetCard(card.Color, card.CardValue, card.Type);
                cardInstance.SetCardFront();
                
                // 添加點擊事件
                cardInstance.CardClicked += OnPlayerCardClicked;
                
                playerHandUI.AddChild(cardInstance);
                GD.Print($"添加了手牌: {card.Color} {card.CardValue}");
            }
        }
        
        // 更新滾動按鈕狀態
        UpdateScrollButtons();
        
        GD.Print($"玩家手牌顯示更新完成，總共 {playerHand.Count} 張牌");
    }
    
    private void UpdateScrollButtons()
    {
        if (leftScrollButton != null && rightScrollButton != null)
        {
            // 計算滾動範圍
            int totalCards = playerHand.Count;
            int maxScrollIndex = Math.Max(0, totalCards - maxVisibleCards);
            
            // 更新按鈕可見性
            leftScrollButton.Visible = currentHandScrollIndex > 0;
            rightScrollButton.Visible = currentHandScrollIndex < maxScrollIndex;
            
            // 更新按鈕可用性
            leftScrollButton.Disabled = currentHandScrollIndex <= 0;
            rightScrollButton.Disabled = currentHandScrollIndex >= maxScrollIndex;
            
            GD.Print($"滾動狀態: 當前索引={currentHandScrollIndex}, 最大索引={maxScrollIndex}, 總牌數={totalCards}");
        }
    }
    
    private void OnLeftScrollPressed()
    {
        if (currentHandScrollIndex > 0)
        {
            currentHandScrollIndex--;
            UpdateHandScrollPosition();
            GD.Print($"向左滾動，當前索引: {currentHandScrollIndex}");
        }
    }
    
    private void OnRightScrollPressed()
    {
        int maxScrollIndex = Math.Max(0, playerHand.Count - maxVisibleCards);
        if (currentHandScrollIndex < maxScrollIndex)
        {
            currentHandScrollIndex++;
            UpdateHandScrollPosition();
            GD.Print($"向右滾動，當前索引: {currentHandScrollIndex}");
        }
    }
    
    private void UpdateHandScrollPosition()
    {
        if (playerHandScrollContainer != null)
        {
            // 計算滾動位置
            float cardWidth = 80.0f; // 卡牌寬度
            float scrollOffset = currentHandScrollIndex * cardWidth;
            
            // 設置滾動位置
            playerHandScrollContainer.ScrollHorizontal = (int)scrollOffset;
            
            // 更新按鈕狀態
            UpdateScrollButtons();
        }
    }
    
    private void UpdateDrawPileDisplay()
    {
        if (drawPile.Count > 0)
        {
            CreateDrawPileStack();
        }
        else
        {
            // 清空抽牌堆顯示
            foreach (Node child in drawPileUI.GetChildren())
            {
                child.QueueFree();
            }
        }
    }
    
    private void OnPlayerCardClicked(Card clickedCard)
    {
        GD.Print($"玩家點擊了手牌: {clickedCard.Color} {clickedCard.CardValue}");
        
        // 設置選中的手牌
        selectedCard = clickedCard;
        
        // 檢查是否可以打出這張牌
        if (currentTopCard != null && clickedCard.CanPlayOn(currentTopCard))
        {
            GD.Print("可以打出這張牌！");
            // 啟用出牌按鈕
            if (playCardButton != null)
            {
                playCardButton.Disabled = false;
            }
        }
        else
        {
            GD.Print("這張牌不能打出");
            // 禁用出牌按鈕
            if (playCardButton != null)
            {
                playCardButton.Disabled = true;
            }
        }
    }
    
    private void PlayCard(Card cardToPlay)
    {
        GD.Print($"開始出牌: {cardToPlay.Color} {cardToPlay.CardValue}");
        
        // 從玩家手牌中移除這張牌
        playerHand.Remove(cardToPlay);
        
        // 將牌添加到棄牌堆
        discardPile.Add(cardToPlay);
        currentTopCard = cardToPlay;
        
        // 更新顯示
        UpdatePlayerHandDisplay();
        UpdateDiscardPileDisplay();
        
        // 清空選中的手牌
        selectedCard = null;
        
        // 禁用出牌按鈕
        if (playCardButton != null)
        {
            playCardButton.Disabled = true;
        }
        
        GD.Print($"出牌完成，剩餘手牌: {playerHand.Count} 張");
    }
    
    private void UpdateDiscardPileDisplay()
    {
        if (discardPileUI != null && currentTopCard != null)
        {
            // 清除現有的子節點
            foreach (Node child in discardPileUI.GetChildren())
            {
                child.QueueFree();
            }
            
            // 創建頂牌實例
            var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
            if (cardScene != null)
            {
                var topCardInstance = cardScene.Instantiate<Card>();
                topCardInstance.SetCard(currentTopCard.Color, currentTopCard.CardValue, currentTopCard.Type);
                topCardInstance.SetCardFront();
                
                // 將頂牌添加到UI
                discardPileUI.AddChild(topCardInstance);
                
                GD.Print($"更新頂牌顯示: {currentTopCard.Color} {currentTopCard.CardValue}");
            }
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

    private void OnColorSelected(string color)
    {
        GD.Print($"選擇顏色: {color}");
        // 隱藏顏色選擇面板
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.Visible = false;
        }
        // 這裡添加顏色選擇後的遊戲邏輯
    }

    private void InitializeGame()
    {
        GD.Print("初始化UNO遊戲...");
        
        // 創建完整的UNO牌組
        CreateDeck();
        
        // 洗牌
        ShuffleDeck();
        
        // 設置第一張頂牌
        SetFirstTopCard();
        
        // 顯示抽牌堆背面
        ShowDrawPileBack();
        
        // 通過動畫發放初始手牌
        StartInitialDealAnimation();
        
        // 初始化按鈕狀態
        if (playCardButton != null)
        {
            playCardButton.Disabled = true; // 初始時禁用出牌按鈕
        }
        
        GD.Print($"初始化完成 - 抽牌堆: {drawPile.Count}張, 頂牌: 1張");
    }
    
    private void StartInitialDealAnimation()
    {
        GD.Print("開始發放初始手牌動畫...");
        
        // 清空玩家手牌（如果有的話）
        playerHand.Clear();
        
        // 開始發放7張初始手牌
        DealInitialCardWithAnimation(0);
    }
    
    private void DealInitialCardWithAnimation(int cardIndex)
    {
        if (cardIndex >= 7 || drawPile.Count == 0)
        {
            GD.Print($"初始手牌發放完成，玩家手牌: {playerHand.Count} 張");
            return;
        }
        
        GD.Print($"開始發放第 {cardIndex + 1} 張初始手牌...");
        
        // 從抽牌堆取出一張牌
        var cardToDraw = drawPile[0];
        drawPile.RemoveAt(0);
        
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
            Vector2 startPos = drawPileUI.GlobalPosition + drawPileUI.Size / 2;
            animatedCard.GlobalPosition = startPos;
            animatedCard.ZIndex = 1000; // 確保在最上層
            
            GD.Print($"初始發牌動畫卡牌初始位置: {startPos}");
            
            // 計算目標位置（玩家手牌區域的特定位置）
            Vector2 targetPos;
            float cardWidth = 80.0f;
            float cardSpacing = 10.0f;
            float startX = playerHandUI.GlobalPosition.X + cardWidth / 2;
            float targetX = startX + (cardIndex * (cardWidth + cardSpacing));
            targetPos = new Vector2(targetX, playerHandUI.GlobalPosition.Y + playerHandUI.Size.Y / 2);
            
            GD.Print($"初始發牌動畫卡牌目標位置: {targetPos}, 第 {cardIndex + 1} 張牌");
            
            // 如果目標位置計算有問題，使用固定位置
            if (targetPos.X == 0 || targetPos.Y == 0)
            {
                // 使用屏幕中心作為備用目標位置
                targetPos = new Vector2(GetViewport().GetVisibleRect().Size.X / 2, GetViewport().GetVisibleRect().Size.Y - 100);
                GD.Print($"使用備用目標位置: {targetPos}");
            }
            
            // 創建動畫軌跡（拋物線效果）
            Vector2 controlPoint = new Vector2(
                (startPos.X + targetPos.X) / 2,
                Math.Min(startPos.Y, targetPos.Y) - 100 // 向上拋物線
            );
            
            GD.Print($"初始發牌動畫控制點位置: {controlPoint}");
            
            // 創建動畫
            var dealTween = CreateTween();
            dealTween.SetParallel(true);
            
            // 使用簡化的位置動畫
            float animationDuration = 0.8f;
            dealTween.TweenProperty(animatedCard, "global_position", targetPos, animationDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
            
            GD.Print($"開始初始發牌動畫: 從 {startPos} 到 {targetPos}, 持續時間: {animationDuration}秒");
            
            // 旋轉動畫（翻牌效果）
            dealTween.TweenProperty(animatedCard, "rotation", Mathf.Pi, 0.4f)
                .SetDelay(0.2f)
                .SetEase(Tween.EaseType.InOut);
            
            // 縮放動畫（彈跳效果）
            dealTween.TweenProperty(animatedCard, "scale", Vector2.One * 1.2f, 0.3f)
                .SetEase(Tween.EaseType.Out);
            dealTween.TweenProperty(animatedCard, "scale", Vector2.One, 0.3f)
                .SetDelay(0.3f)
                .SetEase(Tween.EaseType.In);
            
            // 在翻牌時顯示正面
            dealTween.TweenCallback(Callable.From(() => {
                animatedCard.SetCardFront();
                GD.Print($"第 {cardIndex + 1} 張卡牌翻轉到正面");
            })).SetDelay(0.4f);
            
            // 動畫完成後的回調
            dealTween.TweenCallback(Callable.From(() => {
                GD.Print($"第 {cardIndex + 1} 張初始發牌動畫完成");
                // 移除動畫卡牌
                animatedCard.QueueFree();
                
                // 將牌添加到玩家手牌
                playerHand.Add(cardToDraw);
                
                // 更新玩家手牌顯示
                UpdatePlayerHandDisplay();
                
                // 更新抽牌堆顯示
                UpdateDrawPileDisplay();
                
                // 延遲一下再發放下一張牌，讓動畫更自然
                var delayTween = CreateTween();
                delayTween.TweenCallback(Callable.From(() => {
                    DealInitialCardWithAnimation(cardIndex + 1);
                })).SetDelay(0.2f);
            })).SetDelay(animationDuration);
        }
        else
        {
            GD.PrintErr("無法載入卡片場景");
        }
    }
    
    private void ShowDrawPileBack()
    {
        if (drawPileUI != null && drawPile.Count > 0)
        {
            // 創建堆疊卡牌效果
            CreateDrawPileStack();
            GD.Print("抽牌堆堆疊效果已創建");
        }
    }
    
    private void CreateDrawPileStack()
    {
        // 清除現有的子節點
        foreach (Node child in drawPileUI.GetChildren())
        {
            child.QueueFree();
        }
        
        // 創建多張卡牌來形成堆疊效果
        int stackCount = Math.Min(5, drawPile.Count); // 最多顯示5張
        
        for (int i = 0; i < stackCount; i++)
        {
            // 載入卡片場景並實例化
            var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
            if (cardScene != null)
            {
                var cardInstance = cardScene.Instantiate<Card>();
                
                // 將卡牌添加到UI（在設置背面之前）
                drawPileUI.AddChild(cardInstance);
                
                // 設置位置，形成堆疊效果
                cardInstance.Position = new Vector2(i * 2, i * 2); // 每張卡牌稍微偏移
                cardInstance.ZIndex = i; // 設置層級，後面的卡牌在上層
                
                // 所有卡牌都設置為背面（在添加到UI之後）
                cardInstance.SetCardBack();
            }
        }
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
            
            GD.Print($"{color} 顏色牌創建完成，當前牌組: {drawPile.Count} 張");
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
        
        GD.Print($"牌組創建完成，共 {drawPile.Count} 張牌");
    }
    
    private void CreateCard(CardColor color, string value, CardType type)
    {
        // 載入卡片場景
        var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
        if (cardScene != null)
        {
            var cardInstance = cardScene.Instantiate<Card>();
            cardInstance.SetCard(color, value, type);
            drawPile.Add(cardInstance);
        }
        else
        {
            GD.PrintErr("無法載入卡片場景: res://scenes/card.tscn");
        }
    }
    
    private CardColor GetCardColorFromString(string colorString)
    {
        return colorString switch
        {
            "red" => CardColor.Red,
            "blue" => CardColor.Blue,
            "green" => CardColor.Green,
            "yellow" => CardColor.Yellow,
            _ => CardColor.Red
        };
    }
    
    private void ShuffleDeck()
    {
        GD.Print("洗牌中...");
        var random = new Random();
        
        // Fisher-Yates 洗牌算法
        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
        }
        
        GD.Print("洗牌完成");
    }
    

    
    private void SetFirstTopCard()
    {
        GD.Print("設置第一張頂牌...");
        
        // 找到第一張非萬能牌的牌作為頂牌
        Card firstCard = null;
        for (int i = 0; i < drawPile.Count; i++)
        {
            if (drawPile[i].Type != CardType.Wild && drawPile[i].Type != CardType.WildDrawFour)
            {
                firstCard = drawPile[i];
                drawPile.RemoveAt(i);
                break;
            }
        }
        
        if (firstCard != null)
        {
            currentTopCard = firstCard;
            discardPile.Add(firstCard);
            
            // 顯示頂牌
            if (discardPileUI != null)
            {
                // 清除現有的子節點
                foreach (Node child in discardPileUI.GetChildren())
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
                    discardPileUI.AddChild(topCardInstance);
                    
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
                    
                    discardPileUI.AddChild(topCardBorder);
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