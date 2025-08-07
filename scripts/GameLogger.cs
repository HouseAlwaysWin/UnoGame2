using Godot;
using System;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public static class GameLogger
{
    private static LogLevel currentLogLevel = LogLevel.Info;
    private static bool enableConsoleOutput = true;
    private static bool enableFileOutput = false;
    
    public static void SetLogLevel(LogLevel level)
    {
        currentLogLevel = level;
    }
    
    public static void SetConsoleOutput(bool enabled)
    {
        enableConsoleOutput = enabled;
    }
    
    public static void SetFileOutput(bool enabled)
    {
        enableFileOutput = enabled;
    }
    
    public static void Debug(string message)
    {
        Log(LogLevel.Debug, message);
    }
    
    public static void Info(string message)
    {
        Log(LogLevel.Info, message);
    }
    
    public static void Warning(string message)
    {
        Log(LogLevel.Warning, message);
    }
    
    public static void Error(string message)
    {
        Log(LogLevel.Error, message);
    }
    
    public static void Log(LogLevel level, string message)
    {
        if (level < currentLogLevel) return;
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string levelText = level.ToString().ToUpper();
        string formattedMessage = $"[{timestamp}] [{levelText}] {message}";
        
        if (enableConsoleOutput)
        {
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    GD.Print(formattedMessage);
                    break;
                case LogLevel.Warning:
                    GD.PrintRaw(formattedMessage);
                    break;
                case LogLevel.Error:
                    GD.PrintErr(formattedMessage);
                    break;
            }
        }
        
        if (enableFileOutput)
        {
            // TODO: 實現文件日誌輸出
            // 可以將日誌寫入到文件中
        }
    }
    
    // 遊戲特定日誌方法
    public static void GameState(string message)
    {
        Log(LogLevel.Info, $"[GameState] {message}");
    }
    
    public static void PlayerAction(string playerName, string action)
    {
        Log(LogLevel.Info, $"[Player] {playerName}: {action}");
    }
    
    public static void CardAction(string cardInfo, string action)
    {
        Log(LogLevel.Debug, $"[Card] {cardInfo}: {action}");
    }
    
    public static void Animation(string message)
    {
        Log(LogLevel.Debug, $"[Animation] {message}");
    }
    
    public static void UI(string message)
    {
        Log(LogLevel.Debug, $"[UI] {message}");
    }
}
