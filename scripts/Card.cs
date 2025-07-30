using Godot;
using System;

public enum CardType
{
    Number,
    Skip,
    Reverse,
    DrawTwo,
    Wild,
    WildDrawFour
}

public enum CardColor
{
    Red,
    Blue,
    Green,
    Yellow
}

public partial class Card : TextureRect
{
    [Signal]
    public delegate void CardClickedEventHandler(Card card);
    
    [Export]
    public CardColor Color { get; set; } = CardColor.Red;
    
    [Export]
    public string CardValue { get; set; } = "0";
    
    [Export]
    public CardType Type { get; set; } = CardType.Number;
    
    public override void _Ready()
    {
        // 初始化時不自動顯示正面，等待外部設置
        // UpdateCardDisplay();
        
        // 添加邊框效果
        AddBorder();
    }
    
    private void AddBorder()
    {
        // 為 TextureRect 添加邊框效果
        // 使用 Modulate 來創建邊框效果
        // 這裡我們用簡單的顏色調整來模擬邊框
        Modulate = new Color(1, 1, 1, 1); // 確保不透明度為1
    }
    
    public void SetCard(CardColor color, string value, CardType type = CardType.Number)
    {
        Color = color;
        CardValue = value;
        Type = type;
        
        UpdateCardDisplay();
    }
    
    private void UpdateCardDisplay()
    {
        // 根據卡片屬性設置紋理
        string texturePath = GetCardTexturePath();
        if (ResourceLoader.Exists(texturePath))
        {
            Texture2D texture = ResourceLoader.Load<Texture2D>(texturePath);
            if (texture != null)
            {
                Texture = texture;
            }
        }
    }
    
    private string GetCardTexturePath()
    {
        string basePath = "res://assets/";
        string colorString = Color.ToString().ToLower();
        
        switch (Type)
        {
            case CardType.Skip:
                return basePath + colorString + "Skip.png";
            case CardType.Reverse:
                return basePath + colorString + "Reverse.png";
            case CardType.DrawTwo:
                return basePath + colorString + "DrawTwo.png";
            case CardType.Wild:
                return basePath + "wild.png";
            case CardType.WildDrawFour:
                return basePath + "wildDrawFour.png";
            default: // CardType.Number
                return basePath + colorString + CardValue + ".png";
        }
    }
    
    public void SetCardBack()
    {
        // 設置卡片背面
        string backPath = "res://assets/back.png";
        if (ResourceLoader.Exists(backPath))
        {
            Texture2D backTexture = ResourceLoader.Load<Texture2D>(backPath);
            if (backTexture != null)
            {
                Texture = backTexture;
                GD.Print("卡片背面已設置");
            }
            else
            {
                GD.PrintErr("無法載入背面圖片: " + backPath);
            }
        }
        else
        {
            GD.PrintErr("背面圖片不存在: " + backPath);
        }
    }
    
    public void SetCardFront()
    {
        // 顯示卡片正面
        UpdateCardDisplay();
    }
    
    public bool CanPlayOn(Card topCard)
    {
        // 檢查是否可以打出這張牌
        if (Type == CardType.Wild || Type == CardType.WildDrawFour)
        {
            return true; // 萬能牌可以隨時打出
        }
        
        // 檢查顏色或數字是否匹配
        return Color == topCard.Color || CardValue == topCard.CardValue;
    }
    
    public override void _GuiInput(InputEvent @event)
    {
        // 處理卡片點擊事件
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                GD.Print($"卡片被點擊: {Color} {CardValue}");
                // 這裡可以發出自定義信號
                EmitSignal(SignalName.CardClicked, this);
            }
        }
    }
} 