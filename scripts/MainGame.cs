using Godot;
using System;

public partial class MainGame : Node2D
{
    public override void _Ready()
    {
        GD.Print("主遊戲場景已加載");
        
        // 初始化遊戲邏輯
        InitializeGame();
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