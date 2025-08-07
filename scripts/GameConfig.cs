using Godot;
using System;

public static class GameConfig
{
    // 遊戲基本配置
    public static int DefaultPlayerCount { get; set; } = 4;
    public static int InitialCardsPerPlayer { get; set; } = 7;
    public static int MaxVisibleCards { get; set; } = 8;
    
    // 動畫配置
    public static bool EnableAnimations { get; set; } = true;
    public static float AnimationSpeed { get; set; } = 1.0f;
    public static float CardAnimationDuration { get; set; } = 0.5f;
    
    // UI配置
    public static float CardWidth { get; set; } = 80.0f;
    public static float CardHeight { get; set; } = 120.0f;
    public static float CardSpacing { get; set; } = 10.0f;
    
    // 遊戲邏輯配置
    public static bool EnableUNOChallenge { get; set; } = true;
    public static bool EnableDrawUntilPlay { get; set; } = false;
    public static bool EnableStrictRules { get; set; } = true;
    
    // 從設定文件載入配置
    public static void LoadConfig()
    {
        try
        {
            var config = new ConfigFile();
            var error = config.Load("user://game_config.cfg");
            
            if (error == Error.Ok)
            {
                DefaultPlayerCount = (int)config.GetValue("Game", "PlayerCount", DefaultPlayerCount);
                InitialCardsPerPlayer = (int)config.GetValue("Game", "InitialCardsPerPlayer", InitialCardsPerPlayer);
                EnableAnimations = (bool)config.GetValue("Animation", "EnableAnimations", EnableAnimations);
                AnimationSpeed = (float)config.GetValue("Animation", "AnimationSpeed", AnimationSpeed);
                GD.Print("遊戲配置載入成功");
            }
            else
            {
                GD.Print("使用預設配置");
                SaveConfig(); // 創建預設配置文件
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"載入配置失敗: {ex.Message}");
        }
    }
    
    // 儲存配置到文件
    public static void SaveConfig()
    {
        try
        {
            var config = new ConfigFile();
            
            config.SetValue("Game", "PlayerCount", DefaultPlayerCount);
            config.SetValue("Game", "InitialCardsPerPlayer", InitialCardsPerPlayer);
            config.SetValue("Animation", "EnableAnimations", EnableAnimations);
            config.SetValue("Animation", "AnimationSpeed", AnimationSpeed);
            
            var error = config.Save("user://game_config.cfg");
            if (error == Error.Ok)
            {
                GD.Print("遊戲配置儲存成功");
            }
            else
            {
                GD.PrintErr($"儲存配置失敗: {error}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"儲存配置失敗: {ex.Message}");
        }
    }
}
