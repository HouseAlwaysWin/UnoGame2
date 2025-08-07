using Godot;
using System;

public partial class UIUpdateManager : Node
{
    private UIManager uiManager;
    private GameStateManager gameStateManager;
    
    // UI更新緩存
    private bool playerHandNeedsUpdate = false;
    private bool drawPileNeedsUpdate = false;
    private bool discardPileNeedsUpdate = false;
    private bool gameStatusNeedsUpdate = false;
    
    // 批量更新標誌
    private bool isBatchUpdate = false;
    
    public override void _Ready()
    {
        GameLogger.UI("UIUpdateManager 初始化");
    }
    
    public void Initialize(UIManager ui, GameStateManager gsm)
    {
        uiManager = ui;
        gameStateManager = gsm;
        GameLogger.UI("UIUpdateManager 初始化完成");
    }
    
    // 開始批量更新
    public void BeginBatchUpdate()
    {
        isBatchUpdate = true;
        ResetUpdateFlags();
    }
    
    // 結束批量更新
    public void EndBatchUpdate()
    {
        isBatchUpdate = false;
        PerformPendingUpdates();
    }
    
    // 重置更新標誌
    private void ResetUpdateFlags()
    {
        playerHandNeedsUpdate = false;
        drawPileNeedsUpdate = false;
        discardPileNeedsUpdate = false;
        gameStatusNeedsUpdate = false;
    }
    
    // 執行待處理的更新
    private void PerformPendingUpdates()
    {
        if (playerHandNeedsUpdate)
        {
            UpdatePlayerHandDisplay();
        }
        
        if (drawPileNeedsUpdate)
        {
            UpdateDrawPileDisplay();
        }
        
        if (discardPileNeedsUpdate)
        {
            UpdateDiscardPileDisplay();
        }
        
        if (gameStatusNeedsUpdate)
        {
            UpdateGameStatusDisplay();
        }
        
        ResetUpdateFlags();
    }
    
    // 標記需要更新玩家手牌
    public void MarkPlayerHandForUpdate()
    {
        if (isBatchUpdate)
        {
            playerHandNeedsUpdate = true;
        }
        else
        {
            UpdatePlayerHandDisplay();
        }
    }
    
    // 標記需要更新抽牌堆
    public void MarkDrawPileForUpdate()
    {
        if (isBatchUpdate)
        {
            drawPileNeedsUpdate = true;
        }
        else
        {
            UpdateDrawPileDisplay();
        }
    }
    
    // 標記需要更新棄牌堆
    public void MarkDiscardPileForUpdate()
    {
        if (isBatchUpdate)
        {
            discardPileNeedsUpdate = true;
        }
        else
        {
            UpdateDiscardPileDisplay();
        }
    }
    
    // 標記需要更新遊戲狀態
    public void MarkGameStatusForUpdate()
    {
        if (isBatchUpdate)
        {
            gameStatusNeedsUpdate = true;
        }
        else
        {
            UpdateGameStatusDisplay();
        }
    }
    
    // 更新所有UI
    public void UpdateAllUI()
    {
        BeginBatchUpdate();
        MarkPlayerHandForUpdate();
        MarkDrawPileForUpdate();
        MarkDiscardPileForUpdate();
        MarkGameStatusForUpdate();
        EndBatchUpdate();
    }
    
    // 更新玩家手牌顯示
    private void UpdatePlayerHandDisplay()
    {
        if (uiManager != null && gameStateManager != null)
        {
            uiManager.UpdatePlayerHandDisplay(
                gameStateManager.PlayerHand, 
                gameStateManager.SelectedCardIndex, 
                OnPlayerCardClicked
            );
            GameLogger.UI("玩家手牌顯示已更新");
        }
        else
        {
            GameLogger.Error("無法更新玩家手牌顯示：UIManager 或 GameStateManager 為空");
        }
    }
    
    // 更新抽牌堆顯示
    private void UpdateDrawPileDisplay()
    {
        if (uiManager != null && gameStateManager != null)
        {
            uiManager.UpdateDrawPileDisplay(gameStateManager.DrawPile.Count);
            GameLogger.UI("抽牌堆顯示已更新");
        }
        else
        {
            GameLogger.Error("無法更新抽牌堆顯示：UIManager 或 GameStateManager 為空");
        }
    }
    
    // 更新棄牌堆顯示
    private void UpdateDiscardPileDisplay()
    {
        if (uiManager != null && gameStateManager != null)
        {
            uiManager.UpdateDiscardPileDisplay(gameStateManager.CurrentTopCard);
            GameLogger.UI("棄牌堆顯示已更新");
        }
        else
        {
            GameLogger.Error("無法更新棄牌堆顯示：UIManager 或 GameStateManager 為空");
        }
    }
    
    // 更新遊戲狀態顯示
    private void UpdateGameStatusDisplay()
    {
        if (uiManager != null && gameStateManager != null)
        {
            string currentPlayer = gameStateManager.GetCurrentPlayerName();
            string currentColor = gameStateManager.GetColorText(gameStateManager.CurrentTopCard?.Color ?? CardColor.Red);
            string cardsInfo = gameStateManager.GetAllPlayersCardsInfo();
            
            uiManager.UpdateGameStatusDisplay(currentPlayer, currentColor, cardsInfo);
            GameLogger.UI("遊戲狀態顯示已更新");
        }
        else
        {
            GameLogger.Error("無法更新遊戲狀態顯示：UIManager 或 GameStateManager 為空");
        }
    }
    
    // 玩家卡牌點擊事件處理
    private void OnPlayerCardClicked(Card clickedCard)
    {
        if (gameStateManager == null) return;
        
        int cardIndex = clickedCard.HandIndex;
        GameLogger.PlayerAction("玩家", $"點擊了手牌: {clickedCard.Color} {clickedCard.CardValue}, 索引: {cardIndex}");

        // 如果點擊的是同一張牌，則取消選中
        if (gameStateManager.SelectedCardIndex == cardIndex)
        {
            GameLogger.PlayerAction("玩家", $"取消選中這張牌，索引: {cardIndex}");
            gameStateManager.SelectedCard = null;
            gameStateManager.SelectedCardIndex = -1;
            
            if (uiManager != null)
            {
                uiManager.SetButtonStates(true, false, true);
            }
            
            MarkPlayerHandForUpdate();
            return;
        }

        // 設置選中的手牌
        gameStateManager.SelectedCard = clickedCard;
        gameStateManager.SelectedCardIndex = cardIndex;
        GameLogger.PlayerAction("玩家", $"設置選中的牌: {gameStateManager.SelectedCard.Color} {gameStateManager.SelectedCard.CardValue}, 索引: {gameStateManager.SelectedCardIndex}");

        // 檢查是否可以打出這張牌
        if (gameStateManager.CanPlayCard(clickedCard))
        {
            GameLogger.PlayerAction("玩家", "可以打出這張牌！");
            if (uiManager != null)
            {
                uiManager.SetButtonStates(true, true, true);
            }
        }
        else
        {
            GameLogger.PlayerAction("玩家", "這張牌不能打出！");
            if (uiManager != null)
            {
                uiManager.SetButtonStates(true, false, true);
            }
        }
        
        MarkPlayerHandForUpdate();
    }
}
