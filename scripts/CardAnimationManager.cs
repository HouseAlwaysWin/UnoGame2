using Godot;
using System;

public partial class CardAnimationManager : Node
{
    private bool isAnimating = false;
    
    public bool IsAnimating => isAnimating;
    
    // 創建發牌動畫
    public void CreateDealCardAnimation(
        Card cardToDraw,
        Vector2 startPos,
        Vector2 targetPos,
        int cardIndex,
        Action onAnimationComplete)
    {
        if (isAnimating)
        {
            GD.Print("動畫正在進行中，跳過此次動畫");
            onAnimationComplete?.Invoke();
            return;
        }
        
        GD.Print($"開始發牌動畫: 第 {cardIndex + 1} 張牌");
        isAnimating = true;
        
        // 創建動畫卡牌實例
        var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
        if (cardScene == null)
        {
            GD.PrintErr("無法載入卡片場景");
            isAnimating = false;
            onAnimationComplete?.Invoke();
            return;
        }
        
        var animatedCard = cardScene.Instantiate<Card>();
        animatedCard.SetCard(cardToDraw.Color, cardToDraw.CardValue, cardToDraw.Type);
        animatedCard.SetCardBack(); // 開始時顯示背面
        
        // 將動畫卡牌添加到UI層級中
        var uiLayer = GetNode<CanvasLayer>("../UILayer");
        uiLayer.AddChild(animatedCard);
        
        // 設置動畫卡牌的大小和屬性
        animatedCard.Size = new Vector2(80, 120);
        animatedCard.Visible = true;
        animatedCard.Modulate = new Color(1, 1, 1, 1);
        animatedCard.GlobalPosition = startPos;
        animatedCard.ZIndex = 1000;
        
        GD.Print($"發牌動畫卡牌初始位置: {startPos}");
        GD.Print($"發牌動畫卡牌目標位置: {targetPos}");
        
        // 創建動畫
        var dealTween = CreateTween();
        dealTween.SetParallel(true);
        
        // 位置動畫
        float animationDuration = 0.8f;
        dealTween.TweenProperty(animatedCard, "global_position", targetPos, animationDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        
        GD.Print($"開始發牌動畫: 從 {startPos} 到 {targetPos}, 持續時間: {animationDuration}秒");
        
        // 旋轉動畫（翻牌效果）
        dealTween.TweenProperty(animatedCard, "rotation", Mathf.Pi, 0.4f)
            .SetDelay(0.2f)
            .SetEase(Tween.EaseType.InOut);
        
        // 縮放動畫（彈跳效果）
        dealTween.TweenProperty(animatedCard, "scale", Vector2.One * 1.2f, 0.3f)
            .SetEase(Tween.EaseType.Out);
        dealTween.TweenProperty(animatedCard, "scale", Vector2.One, 0.3f)
            .SetDelay(0.3f)
            .SetEase(Tween.EaseType.In);
        
        // 在翻牌時顯示正面
        dealTween.TweenCallback(Callable.From(() =>
        {
            animatedCard.SetCardFront();
            GD.Print($"第 {cardIndex + 1} 張卡牌翻轉到正面");
        })).SetDelay(0.4f);
        
        // 動畫完成後的回調
        dealTween.TweenCallback(Callable.From(() =>
        {
            GD.Print($"第 {cardIndex + 1} 張發牌動畫完成");
            
            // 移除動畫卡牌
            if (animatedCard != null && IsInstanceValid(animatedCard))
            {
                animatedCard.QueueFree();
            }
            
            // 重置動畫狀態
            isAnimating = false;
            
            // 調用完成回調
            onAnimationComplete?.Invoke();
        })).SetDelay(animationDuration);
    }
    
    // 創建抽牌動畫
    public void CreateDrawCardAnimation(
        Card cardToDraw,
        Vector2 startPos,
        Vector2 targetPos,
        Action onAnimationComplete)
    {
        GD.Print("開始抽牌動畫...");
        
        // 創建動畫卡牌實例
        var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
        if (cardScene == null)
        {
            GD.PrintErr("無法載入卡片場景");
            onAnimationComplete?.Invoke();
            return;
        }
        
        var animatedCard = cardScene.Instantiate<Card>();
        animatedCard.SetCard(cardToDraw.Color, cardToDraw.CardValue, cardToDraw.Type);
        animatedCard.SetCardBack(); // 開始時顯示背面
        
        // 將動畫卡牌添加到UI層級中
        var uiLayer = GetNode<CanvasLayer>("../UILayer");
        uiLayer.AddChild(animatedCard);
        
        // 設置動畫卡牌的大小和屬性
        animatedCard.Size = new Vector2(80, 120);
        animatedCard.Visible = true;
        animatedCard.Modulate = new Color(1, 1, 1, 1);
        animatedCard.GlobalPosition = startPos;
        animatedCard.ZIndex = 1000;
        
        // 創建動畫
        var drawTween = CreateTween();
        drawTween.SetParallel(true);
        
        // 位置動畫
        float animationDuration = 1.0f;
        drawTween.TweenProperty(animatedCard, "global_position", targetPos, animationDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        
        // 旋轉動畫（翻牌效果）
        drawTween.TweenProperty(animatedCard, "rotation", Mathf.Pi, 0.5f)
            .SetDelay(0.3f)
            .SetEase(Tween.EaseType.InOut);
        
        // 縮放動畫（彈跳效果）
        drawTween.TweenProperty(animatedCard, "scale", Vector2.One * 1.3f, 0.3f)
            .SetEase(Tween.EaseType.Out);
        drawTween.TweenProperty(animatedCard, "scale", Vector2.One * 0.9f, 0.2f)
            .SetDelay(0.3f)
            .SetEase(Tween.EaseType.In);
        drawTween.TweenProperty(animatedCard, "scale", Vector2.One, 0.2f)
            .SetDelay(0.5f)
            .SetEase(Tween.EaseType.Out);
        
        // 在翻牌時顯示正面
        drawTween.TweenCallback(Callable.From(() =>
        {
            animatedCard.SetCardFront();
            GD.Print("抽牌卡牌翻轉到正面");
        })).SetDelay(0.5f);
        
        // 動畫完成後的回調
        drawTween.TweenCallback(Callable.From(() =>
        {
            GD.Print("抽牌動畫完成");
            
            // 移除動畫卡牌
            if (animatedCard != null && IsInstanceValid(animatedCard))
            {
                animatedCard.QueueFree();
            }
            
            // 調用完成回調
            onAnimationComplete?.Invoke();
        })).SetDelay(animationDuration);
    }
    
    // 創建出牌動畫
    public void CreatePlayCardAnimation(
        Card cardToPlay,
        Vector2 startPos,
        Vector2 targetPos,
        Action onAnimationComplete)
    {
        GD.Print("開始出牌動畫...");
        
        // 創建動畫卡牌實例
        var cardScene = ResourceLoader.Load<PackedScene>("res://scenes/card.tscn");
        if (cardScene == null)
        {
            GD.PrintErr("無法載入卡片場景");
            onAnimationComplete?.Invoke();
            return;
        }
        
        var animatedCard = cardScene.Instantiate<Card>();
        animatedCard.SetCard(cardToPlay.Color, cardToPlay.CardValue, cardToPlay.Type);
        animatedCard.SetCardFront(); // 出牌時顯示正面
        
        // 將動畫卡牌添加到UI層級中
        var uiLayer = GetNode<CanvasLayer>("../UILayer");
        uiLayer.AddChild(animatedCard);
        
        // 設置動畫卡牌的大小和屬性
        animatedCard.Size = new Vector2(80, 120);
        animatedCard.Visible = true;
        animatedCard.Modulate = new Color(1, 1, 1, 1);
        animatedCard.GlobalPosition = startPos;
        animatedCard.ZIndex = 1000;
        
        // 創建動畫
        var playTween = CreateTween();
        playTween.SetParallel(true);
        
        // 位置動畫
        float animationDuration = 0.8f;
        playTween.TweenProperty(animatedCard, "global_position", targetPos, animationDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        
        // 縮放動畫
        playTween.TweenProperty(animatedCard, "scale", Vector2.One * 1.2f, 0.4f)
            .SetEase(Tween.EaseType.Out);
        playTween.TweenProperty(animatedCard, "scale", Vector2.One, 0.4f)
            .SetDelay(0.4f)
            .SetEase(Tween.EaseType.In);
        
        // 動畫完成後的回調
        playTween.TweenCallback(Callable.From(() =>
        {
            GD.Print("出牌動畫完成");
            
            // 移除動畫卡牌
            if (animatedCard != null && IsInstanceValid(animatedCard))
            {
                animatedCard.QueueFree();
            }
            
            // 調用完成回調
            onAnimationComplete?.Invoke();
        })).SetDelay(animationDuration);
    }
}