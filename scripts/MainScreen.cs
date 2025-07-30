using Godot;
using System;

public partial class MainScreen : Control
{
    public override void _Ready()
    {
        GD.Print("主選單已加載");
        
        // 獲取按鈕節點
        var singlePlayerButton = GetNode<Button>("MenuButtonGroup/StartButton");
        var multiplayerButton = GetNode<Button>("MenuButtonGroup/MultiplayerButton");
        var settingsButton = GetNode<Button>("MenuButtonGroup/SettingsButton");
        var quitButton = GetNode<Button>("MenuButtonGroup/QuitButton");
        
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
        GD.Print("開始多人連線遊戲 - 切換到多人房間");
        GetTree().ChangeSceneToFile("res://scenes/multiplayer_room.tscn");
    }

    private void OnSettingsButtonPressed()
    {
        GD.Print("打開設定 - 切換到設定場景");
        GetTree().ChangeSceneToFile("res://scenes/settings.tscn");
    }

    private void OnQuitButtonPressed()
    {
        GD.Print("離開遊戲");
        GetTree().Quit();
    }
}
