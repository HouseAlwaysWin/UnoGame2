using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    // 玩家管理
    public int PlayerCount { get; set; } = 4; // 遊玩人數，預設4人
    private List<ComputerPlayer> computerPlayers = new List<ComputerPlayer>(); // 電腦玩家列表
    private int currentPlayerIndex = 0; // 當前玩家索引（0為人類玩家）

    // UI 引用
    private TextureRect drawPileUI;
    private TextureRect discardPileUI;
    private HBoxContainer playerHandUI; // 改回 HBoxContainer 類型

    // 遊戲狀態標籤
    private Label currentPlayerLabel;
    private Label currentColorLabel;
    private Label cardsLeftLabel;

    // 訊息框相關
    private VBoxContainer messageContainer;
    private ScrollContainer messageScrollContainer;
    private bool isScrolling = false; // 滾動狀態標記

    // 手牌滾動相關
    private ScrollContainer playerHandScrollContainer;
    private int currentHandScrollIndex = 0;
    private int maxVisibleCards = 8; // 最多顯示8張牌

    // 出牌相關
    private Card selectedCard = null; // 當前選中的手牌
    private int selectedCardIndex = -1; // 當前選中手牌的索引

    // 動畫相關
    private bool isAnimating = false;
    private Tween currentTween;
    private CardAnimationManager cardAnimationManager;

    public override void _Ready()
    {
        GD.Print("主遊戲場景已加載");

        // 獲取UI元素引用
        GetUIReferences();

        // 連接按鈕信號
        ConnectButtonSignals();

        // 創建卡牌動畫管理器
        cardAnimationManager = new CardAnimationManager();
        AddChild(cardAnimationManager);

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
        playerHandScrollContainer = GetNode<ScrollContainer>("UILayer/UI/PlayerHand/ScrollContainer");

        // 獲取遊戲狀態標籤
        currentPlayerLabel = GetNode<Label>("UILayer/UI/TopPanel/GameInfo/CurrentPlayer");
        currentColorLabel = GetNode<Label>("UILayer/UI/TopPanel/GameInfo/CurrentColor");
        cardsLeftLabel = GetNode<Label>("UILayer/UI/TopPanel/GameInfo/CardsLeft");

        // 獲取訊息框
        messageContainer = GetNode<VBoxContainer>("UILayer/UI/MessagePanel/MessageScrollContainer/MessageContainer");
        messageScrollContainer = GetNode<ScrollContainer>("UILayer/UI/MessagePanel/MessageScrollContainer");
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
        // 移除對左右滾動按鈕的引用和相關代碼
    }

    private void OnDrawCardPressed()
    {
        GD.Print("抽牌按鈕被按下");

        if (isAnimating)
        {
            GD.Print("動畫進行中，忽略抽牌請求");
            return;
        }

        // 檢查抽牌堆是否為空，如果為空則重新洗牌
        if (drawPile.Count == 0)
        {
            GD.Print("抽牌堆已空，重新洗牌");
            AddMessage("抽牌堆已空，重新洗牌");
            
            // 將棄牌堆的牌（除了頂牌）重新洗牌
            if (discardPile.Count > 1)
            {
                var cardsToShuffle = new List<Card>(discardPile);
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
                drawPile.AddRange(cardsToShuffle);
                
                // 清空棄牌堆（除了頂牌）
                discardPile.Clear();
                discardPile.Add(currentTopCard);
                
                GD.Print($"重新洗牌完成，抽牌堆: {drawPile.Count}張");
                AddMessage($"重新洗牌完成，抽牌堆: {drawPile.Count}張");
            }
        }

        if (drawPile.Count > 0)
        {
            // 開始發牌動畫
            StartDrawCardAnimation();
        }
        else
        {
            GD.Print("抽牌堆仍然為空，無法抽牌");
            AddMessage("抽牌堆仍然為空，無法抽牌");
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

        // 將牌添加到玩家手牌
        playerHand.Add(originalCard);

        // 更新玩家手牌顯示
        UpdatePlayerHandDisplay();

        // 更新抽牌堆顯示
        UpdateDrawPileDisplay();

        // 重置狀態
        isAnimating = false;
        
        // 注意：按鈕狀態會在 NextPlayer() 方法中正確設置
        // 這裡不需要重新啟用按鈕，因為 NextPlayer() 會根據當前玩家設置正確的按鈕狀態

        // 更新遊戲狀態顯示
        UpdateGameStatusDisplay();

        GD.Print($"抽牌完成，玩家手牌: {playerHand.Count} 張，抽牌堆剩餘: {drawPile.Count} 張");

        // 抽牌後輪換到下一個玩家（標準 UNO 規則）
        NextPlayer();
    }

    private void UpdatePlayerHandDisplay()
    {
        GD.Print($"更新玩家手牌顯示，當前手牌數量: {playerHand.Count}");

        // 清除現有的手牌顯示
        foreach (Node child in playerHandUI.GetChildren())
        {
            if (child is Card || child is MarginContainer)
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

                // 設置牌的索引並添加點擊事件
                cardInstance.HandIndex = i;
                cardInstance.CardClicked += OnPlayerCardClicked;

                // 檢查是否是選中的牌（使用索引來區分相同的牌）
                if (selectedCardIndex == i)
                {
                    GD.Print($"=== 設置選中狀態 ===");
                    GD.Print($"選中的牌索引: {i}, 牌: {card.Color} {card.CardValue}");

                    // 為選中的牌創建 MarginContainer 來實現向上移動
                    var marginContainer = new MarginContainer();
                    marginContainer.AddThemeConstantOverride("margin_top", -20); // 向上移動20像素
                    marginContainer.AddChild(cardInstance);
                    playerHandUI.AddChild(marginContainer);

                    GD.Print($"已為選中牌創建 MarginContainer，向上移動20像素");
                }
                else
                {
                    GD.Print($"非選中的牌: {card.Color} {card.CardValue}");
                    playerHandUI.AddChild(cardInstance);
                }

                GD.Print($"添加了手牌: {card.Color} {card.CardValue}");
            }
        }

        // 更新滾動按鈕狀態
        UpdateScrollButtons();

        GD.Print($"玩家手牌顯示更新完成，總共 {playerHand.Count} 張牌");
    }

    private void UpdateScrollButtons()
    {
        if (playerHandScrollContainer != null)
        {
            // 計算滾動範圍
            int totalCards = playerHand.Count;
            int maxScrollIndex = Math.Max(0, totalCards - maxVisibleCards);

            GD.Print($"滾動狀態: 當前索引={currentHandScrollIndex}, 最大索引={maxScrollIndex}, 總牌數={totalCards}");
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
        int cardIndex = clickedCard.HandIndex;
        GD.Print($"玩家點擊了手牌: {clickedCard.Color} {clickedCard.CardValue}, 索引: {cardIndex}");

        // 如果點擊的是同一張牌，則取消選中
        if (selectedCardIndex == cardIndex)
        {
            GD.Print($"取消選中這張牌，索引: {cardIndex}");
            selectedCard = null;
            selectedCardIndex = -1; // 重置索引
            // 禁用出牌按鈕
            if (playCardButton != null)
            {
                playCardButton.Disabled = true;
            }
            // 更新手牌顯示以重置所有牌的位置
            UpdatePlayerHandDisplay();
            return;
        }

        // 設置選中的手牌
        selectedCard = clickedCard;
        selectedCardIndex = cardIndex; // 使用牌的索引屬性
        GD.Print($"設置選中的牌: {selectedCard.Color} {selectedCard.CardValue}, 索引: {selectedCardIndex}");

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
            if (playerHandUI != null && playerHandUI.GetChildCount() > 0 && selectedCardIndex >= 0)
            {
                // 找到選中卡牌在UI中的位置（使用索引來精確定位）
                int uiIndex = 0;
                for (int i = 0; i < playerHandUI.GetChildCount(); i++)
                {
                    var child = playerHandUI.GetChild(i);
                    if (child is MarginContainer marginContainer && marginContainer.GetChild(0) is Card uiCard)
                    {
                        // 這是選中的卡牌（包裝在MarginContainer中）
                        if (uiIndex == selectedCardIndex)
                        {
                            startPos = uiCard.GlobalPosition;
                            GD.Print($"找到選中卡牌位置（MarginContainer）: {startPos}, 索引: {selectedCardIndex}");
                            break;
                        }
                        uiIndex++;
                    }
                    else if (child is Card cardNode)
                    {
                        // 檢查是否是選中的卡牌（使用索引）
                        if (uiIndex == selectedCardIndex)
                        {
                            startPos = cardNode.GlobalPosition;
                            GD.Print($"找到選中卡牌位置（Card）: {startPos}, 索引: {selectedCardIndex}");
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
            if (discardPileUI != null)
            {
                targetPos = discardPileUI.GlobalPosition;
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
            float animationDuration = 0.8f;
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

        // 移除動畫卡牌
        animatedCard.QueueFree();

        // 執行實際的出牌邏輯
        ExecutePlayCard(originalCard);
    }

    private void ExecutePlayCard(Card cardToPlay)
    {
        GD.Print($"執行出牌邏輯: {cardToPlay.Color} {cardToPlay.CardValue}");

        // 從玩家手牌中移除這張牌
        playerHand.Remove(cardToPlay);

        // 將牌添加到棄牌堆
        discardPile.Add(cardToPlay);
        currentTopCard = cardToPlay;

        // 清空選中的手牌
        selectedCard = null;
        selectedCardIndex = -1; // 重置索引

        // 更新顯示
        UpdatePlayerHandDisplay();
        UpdateDiscardPileDisplay();

        // 禁用出牌按鈕
        if (playCardButton != null)
        {
            playCardButton.Disabled = true;
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

        GD.Print($"出牌完成，剩餘手牌: {playerHand.Count} 張");

        // 更新遊戲狀態顯示
        UpdateGameStatusDisplay();
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

    private void ShowColorSelectionPanel()
    {
        GD.Print("顯示顏色選擇面板");
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.Visible = true;
            // 禁用其他按鈕，強制玩家選擇顏色
            if (drawCardButton != null) drawCardButton.Disabled = true;
            if (playCardButton != null) playCardButton.Disabled = true;
            if (unoButton != null) unoButton.Disabled = true;

            // 添加提示信息
            var colorTitle = GetNode<Label>("UILayer/UI/ColorSelectionPanel/ColorTitle");
            if (colorTitle != null)
            {
                colorTitle.Text = "請選擇下一個顏色:";
                colorTitle.Modulate = new Color(1, 1, 1, 1); // 確保可見
            }
        }
    }

    private void OnColorSelected(string color)
    {
        GD.Print($"選擇顏色: {color}");

        // 顯示選擇確認
        var colorTitle = GetNode<Label>("UILayer/UI/ColorSelectionPanel/ColorTitle");
        if (colorTitle != null)
        {
            colorTitle.Text = $"已選擇: {color}";
            colorTitle.Modulate = new Color(0, 1, 0, 1); // 綠色表示確認
        }

        // 延遲一下再隱藏面板，讓玩家看到選擇確認
        var delayTween = CreateTween();
        delayTween.TweenCallback(Callable.From(() =>
        {
            // 隱藏顏色選擇面板
            if (colorSelectionPanel != null)
            {
                colorSelectionPanel.Visible = false;
            }

            // 注意：按鈕狀態會在 NextPlayer() 方法中正確設置
            // 這裡不需要重新啟用按鈕，因為 NextPlayer() 會根據當前玩家設置正確的按鈕狀態

            // 更新當前頂牌的顏色（萬能牌會改變顏色）
            if (currentTopCard != null && (currentTopCard.Type == CardType.Wild || currentTopCard.Type == CardType.WildDrawFour))
            {
                // 創建一個新的頂牌實例，使用選擇的顏色
                var newTopCard = new Card();
                newTopCard.SetCard(GetCardColorFromString(color), currentTopCard.CardValue, currentTopCard.Type);

                // 更新棄牌堆中的頂牌
                if (discardPile.Count > 0)
                {
                    discardPile[discardPile.Count - 1] = newTopCard;
                }
                currentTopCard = newTopCard;

                // 更新顯示
                UpdateDiscardPileDisplay();

                GD.Print($"萬能牌顏色已更改為: {color}");

                // 更新遊戲狀態顯示
                UpdateGameStatusDisplay();

                // 處理特殊牌效果並輪換到下一個玩家
                HandleSpecialCardEffect(currentTopCard);
                NextPlayer();
            }
        })).SetDelay(1.0f); // 延遲1秒
    }

    private void InitializeGame()
    {
        GD.Print($"初始化UNO遊戲... 遊玩人數: {PlayerCount}人");

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

        // 通過動畫發放初始手牌
        StartInitialDealAnimation();

        // 初始化按鈕狀態
        if (playCardButton != null)
        {
            playCardButton.Disabled = true; // 初始時禁用出牌按鈕
        }

        // 隱藏顏色選擇面板
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.Visible = false;
        }

        // 隨機選擇第一個玩家（0-3，其中0是人類玩家）
        var random = new Random();
        currentPlayerIndex = random.Next(0, PlayerCount);
        GD.Print($"隨機選擇第一個玩家: {GetCurrentPlayerName()}");

        GD.Print($"初始化完成 - 抽牌堆: {drawPile.Count}張, 頂牌: 1張, 電腦玩家: {computerPlayers.Count}個");

        // 更新遊戲狀態顯示
        UpdateGameStatusDisplay();

        // 根據第一個玩家設置按鈕狀態
        if (currentPlayerIndex == 0)
        {
            // 如果是人類玩家，啟用按鈕
            if (drawCardButton != null) 
            {
                drawCardButton.Disabled = false;
                GD.Print($"初始化：抽牌按鈕狀態設置為 {drawCardButton.Disabled}");
            }
            if (playCardButton != null) 
            {
                playCardButton.Disabled = true; // 需要選擇牌
                GD.Print($"初始化：出牌按鈕狀態設置為 {playCardButton.Disabled}");
            }
            if (unoButton != null) 
            {
                unoButton.Disabled = false;
                GD.Print($"初始化：UNO按鈕狀態設置為 {unoButton.Disabled}");
            }
            GD.Print("初始化：啟用人類玩家按鈕");
        }
        else
        {
            // 如果是電腦玩家，禁用按鈕
            if (drawCardButton != null) 
            {
                drawCardButton.Disabled = true;
                GD.Print($"初始化：抽牌按鈕狀態設置為 {drawCardButton.Disabled}");
            }
            if (playCardButton != null) 
            {
                playCardButton.Disabled = true;
                GD.Print($"初始化：出牌按鈕狀態設置為 {playCardButton.Disabled}");
            }
            if (unoButton != null) 
            {
                unoButton.Disabled = true;
                GD.Print($"初始化：UNO按鈕狀態設置為 {unoButton.Disabled}");
            }
            GD.Print("初始化：禁用按鈕（電腦玩家回合）");
        }
    }

    private void CreateComputerPlayers()
    {
        GD.Print($"創建電腦玩家... 總人數: {PlayerCount}人");

        // 清空現有電腦玩家
        computerPlayers.Clear();

        // 創建電腦玩家（總人數減去1個人類玩家）
        for (int i = 1; i < PlayerCount; i++)
        {
            var computerPlayer = new ComputerPlayer(i);
            computerPlayers.Add(computerPlayer);
            GD.Print($"創建電腦玩家: {computerPlayer.PlayerName}");
        }

        GD.Print($"電腦玩家創建完成，共 {computerPlayers.Count} 個電腦玩家");
    }

    private void UpdateGameStatusDisplay()
    {
        // 更新當前玩家顯示
        if (currentPlayerLabel != null)
        {
            string currentPlayerName = GetCurrentPlayerName();
            currentPlayerLabel.Text = $"當前玩家: {currentPlayerName}";
            GD.Print($"更新當前玩家顯示: {currentPlayerName}");
        }

        // 更新當前顏色顯示
        if (currentColorLabel != null && currentTopCard != null)
        {
            string colorText = GetColorText(currentTopCard.Color);
            currentColorLabel.Text = $"當前顏色: {colorText}";
            GD.Print($"更新當前顏色顯示: {colorText}");
        }

        // 更新所有玩家手牌數量顯示
        if (cardsLeftLabel != null)
        {
            string allPlayersCardsInfo = GetAllPlayersCardsInfo();
            cardsLeftLabel.Text = allPlayersCardsInfo;
            GD.Print($"更新所有玩家手牌信息: {allPlayersCardsInfo}");
        }
    }

    private string GetCurrentPlayerName()
    {
        if (currentPlayerIndex == 0)
        {
            return "玩家1 (你)";
        }
        else
        {
            int computerPlayerIndex = currentPlayerIndex - 1;
            if (computerPlayerIndex < computerPlayers.Count)
            {
                return computerPlayers[computerPlayerIndex].PlayerName;
            }
            else
            {
                return $"玩家{currentPlayerIndex + 1}";
            }
        }
    }

    private string GetAllPlayersCardsInfo()
    {
        var info = new List<string>();

        // 人類玩家手牌數量
        info.Add($"你: {playerHand.Count}張");

        // 電腦玩家手牌數量
        for (int i = 0; i < computerPlayers.Count; i++)
        {
            var computerPlayer = computerPlayers[i];
            info.Add($"{computerPlayer.PlayerName}: {computerPlayer.Hand.Count}張");
        }

        return string.Join(" | ", info);
    }

    private void NextPlayer()
    {
        // 輪換到下一個玩家
        currentPlayerIndex = (currentPlayerIndex + 1) % PlayerCount;
        GD.Print($"輪換到下一個玩家: {GetCurrentPlayerName()}");
        GD.Print($"當前玩家索引: {currentPlayerIndex}");

        // 更新UI顯示
        UpdateGameStatusDisplay();

        // 如果是電腦玩家的回合，執行電腦玩家的行動
        if (currentPlayerIndex > 0)
        {
            GD.Print($"開始執行電腦玩家回合，currentPlayerIndex: {currentPlayerIndex}");
            GD.Print("禁用人類玩家按鈕");
            // 禁用人類玩家的按鈕
            if (drawCardButton != null)
            {
                drawCardButton.Disabled = true;
                GD.Print("抽牌按鈕已禁用");
            }
            if (playCardButton != null)
            {
                playCardButton.Disabled = true;
                GD.Print("出牌按鈕已禁用");
            }
            if (unoButton != null)
            {
                unoButton.Disabled = true;
                GD.Print("UNO按鈕已禁用");
            }

            ExecuteComputerPlayerTurn();
        }
        else
        {
            GD.Print("輪換到人類玩家回合");
            GD.Print("啟用人類玩家按鈕");
            // 啟用人類玩家的按鈕
            if (drawCardButton != null)
            {
                drawCardButton.Disabled = false;
                GD.Print($"NextPlayer：抽牌按鈕狀態設置為 {drawCardButton.Disabled}");
            }
            if (playCardButton != null)
            {
                playCardButton.Disabled = true; // 出牌按鈕需要選擇牌後才啟用
                GD.Print($"NextPlayer：出牌按鈕狀態設置為 {playCardButton.Disabled}");
            }
            if (unoButton != null)
            {
                unoButton.Disabled = false;
                GD.Print($"NextPlayer：UNO按鈕狀態設置為 {unoButton.Disabled}");
            }
        }
    }

    private void NextPlayerWithoutComputerTurn()
    {
        // 輪換到下一個玩家，但不執行電腦玩家回合
        currentPlayerIndex = (currentPlayerIndex + 1) % PlayerCount;
        GD.Print($"輪換到下一個玩家: {GetCurrentPlayerName()}");

        // 更新UI顯示
        UpdateGameStatusDisplay();
    }

    private void ExecuteComputerPlayerTurn()
    {
        GD.Print($"ExecuteComputerPlayerTurn 被調用，currentPlayerIndex: {currentPlayerIndex}");
        int computerPlayerIndex = currentPlayerIndex - 1;
        GD.Print($"計算的電腦玩家索引: {computerPlayerIndex}, 電腦玩家數量: {computerPlayers.Count}");
        
        if (computerPlayerIndex < computerPlayers.Count)
        {
            var computerPlayer = computerPlayers[computerPlayerIndex];
            GD.Print($"電腦玩家 {computerPlayer.PlayerName} 的回合開始");
            AddMessage($"{computerPlayer.PlayerName} 的回合開始");
            GD.Print($"電腦玩家手牌數量: {computerPlayer.Hand.Count}");
            GD.Print($"當前頂牌: {currentTopCard?.Color} {currentTopCard?.CardValue}");

            // 檢查電腦玩家是否有可以打出的牌
            Card cardToPlay = computerPlayer.ChooseCardToPlay(currentTopCard);

            if (cardToPlay != null)
            {
                GD.Print($"電腦玩家 {computerPlayer.PlayerName} 打出: {cardToPlay.Color} {cardToPlay.CardValue}");
                AddMessage($"{computerPlayer.PlayerName} 打出: {GetColorText(cardToPlay.Color)} {cardToPlay.CardValue}");

                // 從電腦玩家手牌中移除這張牌
                computerPlayer.Hand.Remove(cardToPlay);
                // 添加到棄牌堆
                discardPile.Add(cardToPlay);
                currentTopCard = cardToPlay;

                // 更新顯示
                UpdateDiscardPileDisplay();
                UpdateGameStatusDisplay();

                // 檢查是否是萬能牌，如果是則自動選擇顏色
                if (cardToPlay.Type == CardType.Wild || cardToPlay.Type == CardType.WildDrawFour)
                {
                    GD.Print($"電腦玩家 {computerPlayer.PlayerName} 打出萬能牌，自動選擇顏色");
                    AddMessage($"{computerPlayer.PlayerName} 打出萬能牌，自動選擇顏色");

                    // 電腦玩家自動選擇顏色
                    CardColor chosenColor = computerPlayer.ChooseColor();
                    GD.Print($"電腦玩家選擇顏色: {chosenColor}");
                    AddMessage($"{computerPlayer.PlayerName} 選擇顏色: {GetColorText(chosenColor)}");

                    // 更新當前頂牌的顏色
                    currentTopCard.Color = chosenColor;
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
                        AddMessage($"{computerPlayer.PlayerName} 出普通牌，輪換到下一個玩家");
                        NextPlayer();
                    }
                }
            }
            else
            {
                GD.Print($"電腦玩家 {computerPlayer.PlayerName} 沒有可以打出的牌，抽一張牌");
                AddMessage($"{computerPlayer.PlayerName} 沒有可以打出的牌，抽一張牌");

                // 檢查抽牌堆是否為空，如果為空則重新洗牌
                if (drawPile.Count == 0)
                {
                    GD.Print("抽牌堆已空，重新洗牌");
                    AddMessage("抽牌堆已空，重新洗牌");
                    
                    // 將棄牌堆的牌（除了頂牌）重新洗牌
                    if (discardPile.Count > 1)
                    {
                        var cardsToShuffle = new List<Card>(discardPile);
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
                        drawPile.AddRange(cardsToShuffle);
                        
                        // 清空棄牌堆（除了頂牌）
                        discardPile.Clear();
                        discardPile.Add(currentTopCard);
                        
                        GD.Print($"重新洗牌完成，抽牌堆: {drawPile.Count}張");
                        AddMessage($"重新洗牌完成，抽牌堆: {drawPile.Count}張");
                    }
                }

                // 電腦玩家抽一張牌
                if (drawPile.Count > 0)
                {
                    var drawnCard = drawPile[0];
                    drawPile.RemoveAt(0);
                    computerPlayer.Hand.Add(drawnCard);
                    GD.Print($"電腦玩家 {computerPlayer.PlayerName} 抽到: {drawnCard.Color} {drawnCard.CardValue}");
                    AddMessage($"{computerPlayer.PlayerName} 抽到: {GetColorText(drawnCard.Color)} {drawnCard.CardValue}");
                }
                else
                {
                    GD.Print("抽牌堆仍然為空，無法抽牌");
                    AddMessage("抽牌堆仍然為空，無法抽牌");
                }

                // 輪換到下一個玩家
                NextPlayer();
            }
        }
        else
        {
            GD.PrintErr($"電腦玩家索引超出範圍: {computerPlayerIndex}, 電腦玩家數量: {computerPlayers.Count}");
            AddMessage($"錯誤：電腦玩家索引超出範圍");
            // 如果索引超出範圍，輪換到人類玩家
            currentPlayerIndex = 0;
            NextPlayer();
        }
    }

    private void HandleSpecialCardEffect(Card card)
    {
        switch (card.Type)
        {
            case CardType.Skip:
                GD.Print("跳過下一個玩家的回合");
                AddMessage("跳過下一個玩家的回合");
                NextPlayer(); // 跳過一個玩家
                break;
            case CardType.Reverse:
                GD.Print("遊戲方向改變");
                AddMessage("遊戲方向改變");
                // 反轉牌需要輪換到下一個玩家
                NextPlayer();
                break;
            case CardType.DrawTwo:
                GD.Print("下一個玩家抽兩張牌");
                AddMessage("下一個玩家抽兩張牌");
                // 讓下一個玩家抽兩張牌
                NextPlayerWithoutComputerTurn();
                DrawTwoCardsForCurrentPlayer();
                // 抽牌後再輪換到下一個玩家
                NextPlayer();
                break;
            case CardType.Wild:
                GD.Print("萬能牌，輪換到下一個玩家");
                AddMessage("萬能牌，輪換到下一個玩家");
                NextPlayer();
                break;
            case CardType.WildDrawFour:
                GD.Print("下一個玩家抽四張牌");
                AddMessage("下一個玩家抽四張牌");
                // 讓下一個玩家抽四張牌
                NextPlayerWithoutComputerTurn();
                DrawFourCardsForCurrentPlayer();
                // 抽牌後再輪換到下一個玩家
                NextPlayer();
                break;
        }
    }

    private void DrawTwoCardsForCurrentPlayer()
    {
        if (currentPlayerIndex == 0)
        {
            // 人類玩家抽兩張牌
            AddMessage("你抽了兩張牌");
            for (int i = 0; i < 2; i++)
            {
                if (drawPile.Count > 0)
                {
                    var card = drawPile[0];
                    drawPile.RemoveAt(0);
                    playerHand.Add(card);
                    GD.Print($"你抽到: {card.Color} {card.CardValue}");
                }
            }
            UpdatePlayerHandDisplay();
        }
        else
        {
            // 電腦玩家抽兩張牌
            int computerPlayerIndex = currentPlayerIndex - 1;
            if (computerPlayerIndex < computerPlayers.Count)
            {
                var computerPlayer = computerPlayers[computerPlayerIndex];
                AddMessage($"{computerPlayer.PlayerName} 抽了兩張牌");
                for (int i = 0; i < 2; i++)
                {
                    if (drawPile.Count > 0)
                    {
                        var card = drawPile[0];
                        drawPile.RemoveAt(0);
                        computerPlayer.Hand.Add(card);
                        GD.Print($"電腦玩家 {computerPlayer.PlayerName} 抽到: {card.Color} {card.CardValue}");
                    }
                }
            }
        }
        UpdateGameStatusDisplay();
    }

    private void DrawFourCardsForCurrentPlayer()
    {
        if (currentPlayerIndex == 0)
        {
            // 人類玩家抽四張牌
            AddMessage("你抽了四張牌");
            for (int i = 0; i < 4; i++)
            {
                if (drawPile.Count > 0)
                {
                    var card = drawPile[0];
                    drawPile.RemoveAt(0);
                    playerHand.Add(card);
                    GD.Print($"你抽到: {card.Color} {card.CardValue}");
                }
            }
            UpdatePlayerHandDisplay();
        }
        else
        {
            // 電腦玩家抽四張牌
            int computerPlayerIndex = currentPlayerIndex - 1;
            if (computerPlayerIndex < computerPlayers.Count)
            {
                var computerPlayer = computerPlayers[computerPlayerIndex];
                AddMessage($"{computerPlayer.PlayerName} 抽了四張牌");
                for (int i = 0; i < 4; i++)
                {
                    if (drawPile.Count > 0)
                    {
                        var card = drawPile[0];
                        drawPile.RemoveAt(0);
                        computerPlayer.Hand.Add(card);
                        GD.Print($"電腦玩家 {computerPlayer.PlayerName} 抽到: {card.Color} {card.CardValue}");
                    }
                }
            }
        }
        UpdateGameStatusDisplay();
    }

    private string GetColorText(CardColor color)
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

    // 添加訊息到訊息框
    private void AddMessage(string message)
    {
        if (messageContainer != null)
        {
            var messageLabel = new Label();
            messageLabel.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            messageLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
            messageLabel.AddThemeConstantOverride("font_size", 14);
            messageLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;

            messageContainer.AddChild(messageLabel);

            // 限制訊息數量，最多保留20條
            if (messageContainer.GetChildCount() > 20)
            {
                var firstChild = messageContainer.GetChild(0);
                if (firstChild != null)
                {
                    firstChild.QueueFree();
                }
            }

            // 確保容器有正確的最小高度
            messageContainer.CustomMinimumSize = new Vector2(0, 0);

            // 使用 FollowFocus 配合溫和的滾動
            CallDeferred(nameof(ForceScrollToBottom));

            GD.Print($"添加訊息: {message}");
        }
    }



    // 簡單的滾動到底部方法
    private async void ForceScrollToBottom()
    {
        if (messageScrollContainer != null && messageContainer != null)
        {
            await ToSignal(GetTree(), "process_frame");
            var vScroll = messageScrollContainer.GetVScrollBar();
            messageScrollContainer.ScrollVertical = (int)vScroll.MaxValue;
        }
    }


    private void StartInitialDealAnimation()
    {
        GD.Print("開始發放初始手牌動畫...");

        // 清空玩家手牌（如果有的話）
        playerHand.Clear();

        // 清空電腦玩家手牌
        foreach (var computerPlayer in computerPlayers)
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
        for (int playerIndex = 0; playerIndex < computerPlayers.Count; playerIndex++)
        {
            var computerPlayer = computerPlayers[playerIndex];
            GD.Print($"為電腦玩家 {computerPlayer.PlayerName} 發放初始手牌...");

            // 發放7張牌給這個電腦玩家
            for (int cardIndex = 0; cardIndex < 7; cardIndex++)
            {
                if (drawPile.Count > 0)
                {
                    var cardToDraw = drawPile[0];
                    drawPile.RemoveAt(0);
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
        GD.Print("遊戲初始化完成，可以開始遊戲");

        // 如果第一個玩家是電腦玩家，開始電腦玩家回合
        if (currentPlayerIndex > 0)
        {
            GD.Print("第一個玩家是電腦玩家，開始電腦玩家回合");
            NextPlayer();
        }
    }

    private void DealInitialCardWithAnimation(int cardIndex, bool isHumanPlayer = true)
    {
        if (cardIndex >= 7 || drawPile.Count == 0)
        {
            if (isHumanPlayer)
            {
                GD.Print($"人類玩家初始手牌發放完成，手牌: {playerHand.Count} 張");
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
        var cardToDraw = drawPile[0];
        drawPile.RemoveAt(0);

        // 計算起始位置（從抽牌堆位置開始）
        Vector2 startPos = drawPileUI.GlobalPosition + drawPileUI.Size / 2;

        // 計算目標位置（玩家手牌區域的特定位置）
        Vector2 targetPos;
        float cardWidth = 80.0f;
        float cardSpacing = 10.0f;
        float startX = playerHandUI.GlobalPosition.X + cardWidth / 2;
        float targetX = startX + (cardIndex * (cardWidth + cardSpacing));
        targetPos = new Vector2(targetX, playerHandUI.GlobalPosition.Y + playerHandUI.Size.Y / 2);

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
                playerHand.Add(cardToDraw);

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
            playerHand.Add(cardToDraw);
            UpdatePlayerHandDisplay();
            UpdateDrawPileDisplay();
            
            // 繼續發下一張牌
            DealInitialCardWithAnimation(cardIndex + 1);
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
        return colorString.ToLower() switch
        {
            "red" or "紅色" => CardColor.Red,
            "blue" or "藍色" => CardColor.Blue,
            "green" or "綠色" => CardColor.Green,
            "yellow" or "黃色" => CardColor.Yellow,
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