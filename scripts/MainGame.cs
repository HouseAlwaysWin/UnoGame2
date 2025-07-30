using Godot;
using System;

public partial class MainGame : Node2D
{
    private Button drawCardButton;
    private Button unoButton;
    private Button backToMenuButton;
    private Panel colorSelectionPanel;
    private Button redButton, blueButton, greenButton, yellowButton;

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
        // 這裡可以添加遊戲初始化邏輯
        // 例如：創建牌組、分配玩家、設置遊戲規則等
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