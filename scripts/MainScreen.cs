using Godot;
using System;

public partial class MainScreen : Control
{
    public override void _Ready()
    {
        GD.Print("主選單已加載");
        
        // 獲取按鈕節點
        var singlePlayerButton = GetNode<Button>("VBoxContainer/StartButton");
        var multiplayerButton = GetNode<Button>("VBoxContainer/MultiplayerButton");
        var settingsButton = GetNode<Button>("VBoxContainer/SettingsButton");
        var quitButton = GetNode<Button>("VBoxContainer/QuitButton");
        
        // 連接按鈕信號
        if (singlePlayerButton != null)
            singlePlayerButton.Pressed += OnSinglePlayerButtonPressed;
        if (multiplayerButton != null)
            multiplayerButton.Pressed += OnMultiplayerButtonPressed;
        if (settingsButton != null)
            settingsButton.Pressed += OnSettingsButtonPressed;
        if (quitButton != null)
            quitButton.Pressed += OnQuitButtonPressed;
    }

    private void OnSinglePlayerButtonPressed()
    {
        GD.Print("開始單人遊戲 - 切換到主遊戲場景");
        GetTree().ChangeSceneToFile("res://scenes/main_game.tscn");
    }

    private void OnMultiplayerButtonPressed()
    {
        GD.Print("開始多人連線遊戲");
        // 這裡可以切換到多人遊戲場景
        // GetTree().ChangeSceneToFile("res://scenes/multiplayer_game.tscn");
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
