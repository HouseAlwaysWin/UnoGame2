using Godot;
using System;

public partial class MainScreen : Control
{
    public override void _Ready()
    {
        GD.Print("主選單已加載");
        
        // 獲取按鈕節點
        var startButton = GetNode<Button>("VBoxContainer/StartButton");
        var settingsButton = GetNode<Button>("VBoxContainer/SettingsButton");
        var quitButton = GetNode<Button>("VBoxContainer/QuitButton");
        
        // 連接按鈕信號
        if (startButton != null)
            startButton.Pressed += OnStartButtonPressed;
        if (settingsButton != null)
            settingsButton.Pressed += OnSettingsButtonPressed;
        if (quitButton != null)
            quitButton.Pressed += OnQuitButtonPressed;
    }

    private void OnStartButtonPressed()
    {
        GD.Print("開始遊戲 - 切換到主遊戲場景");
        GetTree().ChangeSceneToFile("res://scenes/main_game.tscn");
    }

    private void OnSettingsButtonPressed()
    {
        GD.Print("打開設定");
        // 這裡可以打開設定菜單
        // GetTree().ChangeSceneToFile("res://scenes/settings.tscn");
    }

    private void OnQuitButtonPressed()
    {
        GD.Print("離開遊戲");
        GetTree().Quit();
    }
}
