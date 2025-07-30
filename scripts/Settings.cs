using Godot;
using System;

public partial class Settings : Control
{
    private OptionButton languageOptions;
    private Button saveButton;
    private Button backButton;
    
    // 支援的語言
    private string[] supportedLanguages = { "繁體中文", "English", "日本語" };
    private string[] languageCodes = { "zh-TW", "en", "ja" };

    public override void _Ready()
    {
        GD.Print("設定場景已載入");
        
        // 獲取UI元素
        languageOptions = GetNode<OptionButton>("SettingsPanel/LanguageSection/LanguageOptions");
        saveButton = GetNode<Button>("SettingsPanel/ButtonsContainer/SaveButton");
        backButton = GetNode<Button>("SettingsPanel/ButtonsContainer/BackButton");
        
        // 初始化語言選項
        InitializeLanguageOptions();
        
        // 連接按鈕信號
        if (saveButton != null)
            saveButton.Pressed += OnSaveButtonPressed;
        if (backButton != null)
            backButton.Pressed += OnBackButtonPressed;
        if (languageOptions != null)
            languageOptions.ItemSelected += OnLanguageSelected;
    }

    private void InitializeLanguageOptions()
    {
        if (languageOptions == null) return;
        
        // 清空現有選項
        languageOptions.Clear();
        
        // 添加語言選項
        for (int i = 0; i < supportedLanguages.Length; i++)
        {
            languageOptions.AddItem(supportedLanguages[i], i);
        }
        
        // 載入當前語言設定
        string currentLanguage = GetCurrentLanguage();
        int currentIndex = Array.IndexOf(languageCodes, currentLanguage);
        if (currentIndex >= 0)
        {
            languageOptions.Selected = currentIndex;
        }
        else
        {
            languageOptions.Selected = 0; // 預設繁體中文
        }
    }

    private void OnLanguageSelected(long index)
    {
        if (index >= 0 && index < languageCodes.Length)
        {
            string selectedLanguage = languageCodes[index];
            GD.Print($"選擇語言: {supportedLanguages[index]} ({selectedLanguage})");
            
            // 這裡可以即時預覽語言變更
            // 實際的語言切換會在儲存時執行
        }
    }

    private void OnSaveButtonPressed()
    {
        GD.Print("儲存設定");
        
        // 儲存語言設定
        if (languageOptions != null)
        {
            int selectedIndex = languageOptions.Selected;
            if (selectedIndex >= 0 && selectedIndex < languageCodes.Length)
            {
                string selectedLanguage = languageCodes[selectedIndex];
                SaveLanguageSetting(selectedLanguage);
                GD.Print($"已儲存語言設定: {supportedLanguages[selectedIndex]}");
            }
        }
        
        // 顯示儲存成功訊息
        ShowSaveMessage();
    }

    private void OnBackButtonPressed()
    {
        GD.Print("返回主選單");
        GetTree().ChangeSceneToFile("res://scenes/main_screen.tscn");
    }

    private string GetCurrentLanguage()
    {
        // 從設定檔或預設值讀取當前語言
        // 這裡可以擴展為讀取實際的設定檔
        return "zh-TW"; // 預設繁體中文
    }

    private void SaveLanguageSetting(string languageCode)
    {
        // 儲存語言設定到設定檔
        // 這裡可以擴展為實際的設定檔儲存
        GD.Print($"儲存語言設定: {languageCode}");
        
        // 範例：可以儲存到 ConfigFile 或 UserDefaults
        // var config = new ConfigFile();
        // config.SetValue("Settings", "Language", languageCode);
        // config.Save("user://settings.cfg");
    }

    private void ShowSaveMessage()
    {
        // 顯示儲存成功訊息
        GD.Print("設定已儲存！");
        
        // 這裡可以添加UI提示，例如彈出訊息框
        // 或者延遲返回主選單
        CallDeferred(nameof(ReturnToMainMenu));
    }

    private void ReturnToMainMenu()
    {
        // 延遲返回主選單，讓用戶看到儲存訊息
        GetTree().ChangeSceneToFile("res://scenes/main_screen.tscn");
    }
} 