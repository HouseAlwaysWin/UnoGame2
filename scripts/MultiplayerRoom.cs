using Godot;
using System;

public partial class MultiplayerRoom : Control
{
    public override void _Ready()
    {
        var backButton = GetNode<Button>("BackButton");
        if (backButton != null)
            backButton.Pressed += OnBackButtonPressed;
    }

    private void OnBackButtonPressed()
    {
        GD.Print("返回主選單");
        GetTree().ChangeSceneToFile("res://scenes/main_screen.tscn");
    }
}