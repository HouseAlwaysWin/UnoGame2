using Godot;
using System;
using System.Collections.Generic;

public partial class UIManager : Node
{
    // UI 元素引用
    private Button drawCardButton;
    private Button unoButton;
    private Button backToMenuButton;
    private Button playCardButton;
    private Panel colorSelectionPanel;
    private Button redButton, blueButton, greenButton, yellowButton;
    
    // 卡牌顯示區域
    private TextureRect drawPileUI;
    private TextureRect discardPileUI;
    private HBoxContainer playerHandUI;
    private ScrollContainer playerHandScrollContainer;
    
    // 遊戲狀態標籤
    private Label currentPlayerLabel;
    private Label currentColorLabel;
    private Label cardsLeftLabel;
    
    // 訊息框相關
    private VBoxContainer messageContainer;
    private ScrollContainer messageScrollContainer;
    
    // 顏色選擇面板標題
    private Label colorTitle;
    
    // 事件信號
    public event Action OnDrawCardPressed;
    public event Action OnPlayCardPressed;
    public event Action OnUnoPressed;
    public event Action OnBackToMenuPressed;
    public event Action<string> OnColorSelected;
    
    // 按鈕狀態常量
    private const string BUTTON_DISABLED_STYLE = "disabled";
    private const string BUTTON_ENABLED_STYLE = "normal";
    
    public override void _Ready()
    {
        GD.Print("UIManager 初始化開始");
        InitializeUIReferences();
        ConnectButtonSignals();
        GD.Print("UIManager 初始化完成");
    }
    
    private void InitializeUIReferences()
    {
        try
        {
            // 獲取按鈕引用
            drawCardButton = GetNode<Button>("../UILayer/UI/ActionButtons/DrawCardButton");
            unoButton = GetNode<Button>("../UILayer/UI/ActionButtons/UnoButton");
            backToMenuButton = GetNode<Button>("../UILayer/UI/TopPanel/GameInfo/BackToMenuButton");
            playCardButton = GetNode<Button>("../UILayer/UI/ActionButtons/PlayCardButton");
            
            // 獲取顏色選擇面板和按鈕
            colorSelectionPanel = GetNode<Panel>("../UILayer/UI/ColorSelectionPanel");
            redButton = GetNode<Button>("../UILayer/UI/ColorSelectionPanel/ColorButtons/RedButton");
            blueButton = GetNode<Button>("../UILayer/UI/ColorSelectionPanel/ColorButtons/BlueButton");
            greenButton = GetNode<Button>("../UILayer/UI/ColorSelectionPanel/ColorButtons/GreenButton");
            yellowButton = GetNode<Button>("../UILayer/UI/ColorSelectionPanel/ColorButtons/YellowButton");
            
            // 獲取卡牌顯示區域引用
            drawPileUI = GetNode<TextureRect>("../UILayer/UI/DrawPile");
            discardPileUI = GetNode<TextureRect>("../UILayer/UI/CenterArea/DiscardPile");
            playerHandUI = GetNode<HBoxContainer>("../UILayer/UI/PlayerHand/ScrollContainer/CardsContainer");
            playerHandScrollContainer = GetNode<ScrollContainer>("../UILayer/UI/PlayerHand/ScrollContainer");
            
            // 獲取遊戲狀態標籤
            currentPlayerLabel = GetNode<Label>("../UILayer/UI/TopPanel/GameInfo/CurrentPlayer");
            currentColorLabel = GetNode<Label>("../UILayer/UI/TopPanel/GameInfo/CurrentColor");
            cardsLeftLabel = GetNode<Label>("../UILayer/UI/TopPanel/GameInfo/CardsLeft");
            
            // 獲取訊息框
            messageContainer = GetNode<VBoxContainer>("../UILayer/UI/MessagePanel/MessageScrollContainer/MessageContainer");
            messageScrollContainer = GetNode<ScrollContainer>("../UILayer/UI/MessagePanel/MessageScrollContainer");
            
            // 獲取顏色選擇面板標題
            colorTitle = GetNode<Label>("../UILayer/UI/ColorSelectionPanel/ColorTitle");
            
            GD.Print("UI 元素引用初始化完成");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"UI 元素引用初始化失敗: {ex.Message}");
        }
    }
    
    private void ConnectButtonSignals()
    {
        try
        {
            // 連接動作按鈕
            if (drawCardButton != null)
                drawCardButton.Pressed += () => OnDrawCardPressed?.Invoke();
            
            if (playCardButton != null)
                playCardButton.Pressed += () => OnPlayCardPressed?.Invoke();
            
            if (unoButton != null)
                unoButton.Pressed += () => OnUnoPressed?.Invoke();
            
            if (backToMenuButton != null)
                backToMenuButton.Pressed += () => OnBackToMenuPressed?.Invoke();
            
            // 連接顏色選擇按鈕
            if (redButton != null)
                redButton.Pressed += () => OnColorSelected?.Invoke("紅色");
            
            if (blueButton != null)
                blueButton.Pressed += () => OnColorSelected?.Invoke("藍色");
            
            if (greenButton != null)
                greenButton.Pressed += () => OnColorSelected?.Invoke("綠色");
            
            if (yellowButton != null)
                yellowButton.Pressed += () => OnColorSelected?.Invoke("黃色");
            
            GD.Print("按鈕信號連接完成");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"按鈕信號連接失敗: {ex.Message}");
        }
    }
    
    // 按鈕狀態管理
    public void SetButtonStates(bool drawEnabled, bool playEnabled, bool unoEnabled)
    {
        SafeSetButtonState(drawCardButton, drawEnabled, "抽牌");
        SafeSetButtonState(playCardButton, playEnabled, "出牌");
        SafeSetButtonState(unoButton, unoEnabled, "UNO");
        
        GD.Print($"按鈕狀態設置: 抽牌={drawEnabled}, 出牌={playEnabled}, UNO={unoEnabled}");
    }
    
    private void SafeSetButtonState(Button button, bool enabled, string buttonName)
    {
        if (button != null)
        {
            button.Disabled = !enabled;
            GD.Print($"{buttonName}按鈕狀態設置為: {(enabled ? "啟用" : "禁用")}");
        }
        else
        {
            GD.PrintErr($"{buttonName}按鈕引用為空");
        }
    }
    
    // 顏色選擇面板管理
    public void ShowColorSelectionPanel(string title = "請選擇下一個顏色:")
    {
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.Visible = true;
            SetColorTitle(title);
            SetButtonStates(false, false, false); // 禁用其他按鈕
            GD.Print("顏色選擇面板已顯示");
        }
        else
        {
            GD.PrintErr("顏色選擇面板引用為空");
        }
    }
    
    public void HideColorSelectionPanel()
    {
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.Visible = false;
            GD.Print("顏色選擇面板已隱藏");
        }
    }
    
    public void SetColorTitle(string title)
    {
        if (colorTitle != null)
        {
            colorTitle.Text = title;
            colorTitle.Modulate = new Color(1, 1, 1, 1);
        }
    }
    
    public void SetColorSelectionConfirmed(string color)
    {
        if (colorTitle != null)
        {
            colorTitle.Text = $"已選擇: {color}";
            colorTitle.Modulate = new Color(0, 1, 0, 1); // 綠色表示確認
        }
    }
    
    // 遊戲狀態顯示更新
    public void UpdateGameStatusDisplay(string currentPlayer, string currentColor, string cardsInfo)
    {
        SafeSetLabelText(currentPlayerLabel, $"當前玩家: {currentPlayer}");
        SafeSetLabelText(currentColorLabel, $"當前顏色: {currentColor}");
        SafeSetLabelText(cardsLeftLabel, cardsInfo);
        
        GD.Print($"遊戲狀態顯示已更新: 玩家={currentPlayer}, 顏色={currentColor}");
    }
    
    private void SafeSetLabelText(Label label, string text)
    {
        if (label != null)
        {
            label.Text = text;
        }
    }
    
    // 訊息管理
    public void AddMessage(string message)
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
            
            // 滾動到底部
            CallDeferred(nameof(ForceScrollToBottom));
            
            GD.Print($"添加訊息: {message}");
        }
        else
        {
            GD.PrintErr("訊息容器引用為空");
        }
    }
    
    private async void ForceScrollToBottom()
    {
        if (messageScrollContainer != null && messageContainer != null)
        {
            await ToSignal(GetTree(), "process_frame");
            var vScroll = messageScrollContainer.GetVScrollBar();
            messageScrollContainer.ScrollVertical = (int)vScroll.MaxValue;
        }
    }
    
    // 手牌顯示管理
    public void UpdatePlayerHandDisplay(List<Card> playerHand, int selectedCardIndex, Card.CardClickedEventHandler onCardClicked)
    {
        if (playerHandUI == null)
        {
            GD.PrintErr("玩家手牌UI容器引用為空");
            return;
        }
        
        GD.Print($"更新玩家手牌顯示，當前手牌數量: {playerHand.Count}");
        
        // 清除現有的手牌顯示
        foreach (Node child in playerHandUI.GetChildren())
        {
            if (child is Card || child is MarginContainer)
            {
                child.QueueFree();
            }
        }
        
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
                cardInstance.CardClicked += onCardClicked;
                
                // 檢查是否是選中的牌
                if (selectedCardIndex == i)
                {
                    GD.Print($"設置選中狀態，索引: {i}, 牌: {card.Color} {card.CardValue}");
                    
                    // 為選中的牌創建 MarginContainer 來實現向上移動
                    var marginContainer = new MarginContainer();
                    marginContainer.AddThemeConstantOverride("margin_top", -20);
                    marginContainer.AddChild(cardInstance);
                    playerHandUI.AddChild(marginContainer);
                }
                else
                {
                    playerHandUI.AddChild(cardInstance);
                }
                
                GD.Print($"添加了手牌: {card.Color} {card.CardValue}");
            }
        }
        
        GD.Print($"玩家手牌顯示更新完成，總共 {playerHand.Count} 張牌");
    }
    
    // 抽牌堆顯示管理
    public void UpdateDrawPileDisplay(int drawPileCount)
    {
        if (drawPileUI == null)
        {
            GD.PrintErr("抽牌堆UI引用為空");
            return;
        }
        
        if (drawPileCount > 0)
        {
            CreateDrawPileStack(drawPileCount);
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
    
    private void CreateDrawPileStack(int drawPileCount)
    {
        // 清除現有的子節點
        foreach (Node child in drawPileUI.GetChildren())
        {
            child.QueueFree();
        }
        
        // 創建多張卡牌來形成堆疊效果
        int stackCount = Math.Min(5, drawPileCount); // 最多顯示5張
        
        for (int i = 0; i < stackCount; i++)
        {
            var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
            if (cardScene != null)
            {
                var cardInstance = cardScene.Instantiate<Card>();
                drawPileUI.AddChild(cardInstance);
                
                // 設置位置，形成堆疊效果
                cardInstance.Position = new Vector2(i * 2, i * 2);
                cardInstance.ZIndex = i;
                
                // 所有卡牌都設置為背面
                cardInstance.SetCardBack();
            }
        }
    }
    
    // 棄牌堆顯示管理
    public void UpdateDiscardPileDisplay(Card topCard)
    {
        if (discardPileUI == null)
        {
            GD.PrintErr("棄牌堆UI引用為空");
            return;
        }
        
        if (topCard == null)
        {
            GD.PrintErr("頂牌為空");
            return;
        }
        
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
            topCardInstance.SetCard(topCard.Color, topCard.CardValue, topCard.Type);
            topCardInstance.SetCardFront();
            
            // 將頂牌添加到UI
            discardPileUI.AddChild(topCardInstance);
            
            GD.Print($"更新頂牌顯示: {topCard.Color} {topCard.CardValue}");
        }
    }
    
    // 獲取UI元素引用（供外部使用）
    public Button DrawCardButton => drawCardButton;
    public Button PlayCardButton => playCardButton;
    public Button UnoButton => unoButton;
    public Button BackToMenuButton => backToMenuButton;
    public TextureRect DrawPileUI => drawPileUI;
    public TextureRect DiscardPileUI => discardPileUI;
    public HBoxContainer PlayerHandUI => playerHandUI;
    public ScrollContainer PlayerHandScrollContainer => playerHandScrollContainer;
    public Panel ColorSelectionPanel => colorSelectionPanel;
}
