using Godot;
using System;
using System.Collections.Generic;

public partial class MainGame : Node2D
{
    private Button drawCardButton;
    private Button unoButton;
    private Button backToMenuButton;
    private Panel colorSelectionPanel;
    private Button redButton, blueButton, greenButton, yellowButton;
    
    // 遊戲狀態
    private List<Card> drawPile = new List<Card>(); // 抽牌堆
    private List<Card> discardPile = new List<Card>(); // 棄牌堆
    private List<Card> playerHand = new List<Card>(); // 玩家手牌
    private Card currentTopCard; // 當前頂牌

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
        
        // 獲取顏色選擇面板和按鈕
        colorSelectionPanel = GetNode<Panel>("UILayer/UI/ColorSelectionPanel");
        redButton = GetNode<Button>("UILayer/UI/ColorSelectionPanel/ColorButtons/RedButton");
        blueButton = GetNode<Button>("UILayer/UI/ColorSelectionPanel/ColorButtons/BlueButton");
        greenButton = GetNode<Button>("UILayer/UI/ColorSelectionPanel/ColorButtons/GreenButton");
        yellowButton = GetNode<Button>("UILayer/UI/ColorSelectionPanel/ColorButtons/YellowButton");
    }

    private void ConnectButtonSignals()
    {
        // 連接動作按鈕
        if (drawCardButton != null)
            drawCardButton.Pressed += OnDrawCardPressed;
        
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
    }

    private void OnDrawCardPressed()
    {
        GD.Print("抽牌按鈕被按下");
        // 這裡添加抽牌邏輯
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
        
        // 發初始手牌
        DealInitialCards();
        
        // 設置第一張頂牌
        SetFirstTopCard();
        
        GD.Print($"初始化完成 - 抽牌堆: {drawPile.Count}張, 玩家手牌: {playerHand.Count}張, 頂牌: 1張");
        GD.Print($"總計: {drawPile.Count + playerHand.Count + 1}張 (108張牌 - 7張手牌 - 1張頂牌 = 100張抽牌堆)");
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
    
    private void DealInitialCards()
    {
        GD.Print("發初始手牌...");
        
        // 為玩家發7張牌
        for (int i = 0; i < 7; i++)
        {
            if (drawPile.Count > 0)
            {
                var card = drawPile[0];
                drawPile.RemoveAt(0);
                playerHand.Add(card);
            }
        }
        
        GD.Print($"玩家手牌: {playerHand.Count} 張");
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