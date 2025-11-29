using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 爬平台小游戏 - 完整的智能平台追踪系统
/// 玩家Y固定，X随手机倾斜移动
/// 根据上下方平台追踪动态生成新平台
/// 
/// 系统设计说明：
/// 1. 上方追踪平台（TopTracked）：记录最上方需要生成下一个平台的参考平台
/// 2. 下方追踪平台（BottomTracked）：记录最下方的平台，判断是否游戏失败
/// 3. 距离计算：由上方追踪平台的顶部到屏幕上边界的距离决定是否生成新平台
/// 4. 自动升级：当上方平台离开屏幕时，自动升级到下一个平台；下方平台超出范围时也自动升级
/// </summary>
public class ClimbingGameUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Canvas gameCanvas; // 游戏Canvas
    [SerializeField] private RectTransform gameAreaRect; // 游戏区域
    [SerializeField] private RectTransform characterRect; // 角色
    [SerializeField] private Transform platformContainer; // 平台容器
    [SerializeField] private Image platformPrefab; // 平台预制体

    [Header("角色设置")]
    [SerializeField] private float characterRadius = 2.5f; // 角色碰撞半径

    [Header("平台设置")]
    [SerializeField] private float platformWidth = 20f; // 平台宽度
    [SerializeField] private float platformHeight = 2f; // 平台高度
    [SerializeField] private Color platformColor = Color.white; // 平台颜色

    [Header("平台运动")]
    [SerializeField] private float platformUpSpeed = 50f; // 平台向上升的速度
    [SerializeField] private float platformDownDistance = 30f; // 接触时下降的距离
    [SerializeField] private float platformDownDuration = 0.2f; // 下降持续时间

    [Header("平台生成逻辑")]
    [SerializeField] private float initialPlatformYOffset = -150f; // 初始平台的Y偏移（负数表示在玩家下方）
    [SerializeField] private float platformHeightGap = 100f; // 平台之间的固定高度间隔
    [SerializeField] private float platformSpawnXRange = 80f; // 平台X位置的随机范围（±多少像素）
    [SerializeField] private float platformDisappearDistance = 100f; // 平台超出屏幕上方多远时删除
    [SerializeField] private float bottomDisappearDistance = 100f; // 平台低于屏幕下方多远时删除

    [Header("Mask效果设置")]
    [SerializeField] private bool enablePlatformMask = true; // 是否启用平台Mask效果
    [SerializeField] private float maskTopDistance = 100f; // 超出屏幕上方多远时隐藏（Mask）
    [SerializeField] private float maskBottomDistance = 100f; // 低于屏幕下方多远时隐藏（Mask）

    [Header("平台触发生成设置")]
    [SerializeField] private bool useTriggerSpawning = true; // 是否使用触发式生成（而不是每帧生成）
    [SerializeField] private float triggerSpawnLookaheadDistance = 300f; // 向上预生成平台的距离（基于消失限制）

    [Header("游戏设置")]
    [SerializeField] private PhoneShakeDetector phoneDetector;

    [Header("分数系统")]
    [SerializeField] private bool enableScoreSystem = true; // 是否启用分数系统
    [SerializeField] private TextMeshProUGUI scoreText; // 分数显示的TMP Text（在Inspector中指定）
    [SerializeField] private float scoreDisplayScale = 1f; // 分数显示倍数（可显示为整数）

    [Header("动态难度")]
    [SerializeField] private bool enableDynamicSpeed = true; // 是否启用动态速度调整
    [SerializeField] private float basePlatformUpSpeed = 50f; // 基础平台上升速度
    [SerializeField] private float speedIncreasePerScore = 0.05f; // 每100分增加的速度值
    [SerializeField] private float maxPlatformUpSpeed = 200f; // 最大平台上升速度限制

    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool isGameRunning = false;

    // 内部变量
    private Vector2 characterPosition; // 角色位置 (X: 可变, Y: 固定为0)
    private float gameAreaWidth;
    private float gameAreaHeight;

    // 平台管理 - 按照生成顺序存储
    private List<RectTransform> activePlatforms = new List<RectTransform>();
    private List<float> platformSpawnY = new List<float>(); // 每个平台的初始Y位置（相对于游戏区域）

    // 平台追踪系统
    private int topTrackedPlatformIndex = -1; // 上方追踪的平台索引（用于决定生成新平台）
    private int bottomTrackedPlatformIndex = -1; // 下方追踪的平台索引（失败判定用）
    private float distanceToTopBoundary = 0f; // 上方追踪平台距离屏幕上边界的距离

    // 平台运动
    private float platformsCurrentY = 0f; // 所有平台的统一Y偏移（相对于初始位置向上移动）
    private bool isPlatformDecelerating = false;
    private float platformDecelerateTimer = 0f;
    private float platformDecelerateStartY = 0f;

    // 游戏状态
    private int platformsTouched = 0;
    private float highestY = 0f;
    private float currentScore = 0f; // 当前分数（基于平台升过的距离）
    private float finalScore = 0f; // 最终分数（历史最高分，取绝对值）
    private float currentDynamicSpeed = 0f; // 当前的动态速度

    // 游戏结束
    private bool isGameOverAnimating = false;
    private float gameOverFlashTimer = 0f;
    private Image gameOverFlashImage;

    private void Start()
    {
        if (gameAreaRect == null || characterRect == null)
        {
            Debug.LogError("[ClimbingGameUI] 未指定游戏区域或角色RectTransform！");
            return;
        }

        if (phoneDetector == null)
        {
            phoneDetector = FindObjectOfType<PhoneShakeDetector>();
            if (phoneDetector == null)
            {
                Debug.LogError("[ClimbingGameUI] 找不到PhoneShakeDetector组件！");
                return;
            }
        }

        gameAreaWidth = gameAreaRect.rect.width;
        gameAreaHeight = gameAreaRect.rect.height;

        // 角色Y固定在0（屏幕中心），X可变
        characterPosition = Vector2.zero;
        characterRect.anchoredPosition = characterPosition;

        phoneDetector.Calibrate();

        if (gameCanvas == null)
        {
            gameCanvas = gameAreaRect.GetComponentInParent<Canvas>();
        }

        CreateGameOverFlashUI();

        Debug.Log($"[ClimbingGameUI] 初始化完成。游戏区域: {gameAreaWidth}x{gameAreaHeight}");
    }

    private void Update()
    {
        if (isGameOverAnimating)
        {
            HandleGameOverAnimation();
            return;
        }

        if (!isGameRunning || characterRect == null)
            return;

        // 1. 角色移动（只X轴）
        HandleCharacterMovement();

        // 2. 更新平台位置
        UpdatePlatformsPosition();

        // 3. 检测碰撞（触碰平台）
        DetectPlatformCollision();

        // 4. 基于生成模式生成新平台
        // 如果使用触发生成模式，则在碰撞时生成，否则每帧生成
        if (!useTriggerSpawning)
        {
            SpawnNewPlatformsBasedOnTracking();
        }

        // 5. 更新平台Mask效果（透明度）
        UpdatePlatformMask();

        // 6. 清理超出范围的平台
        CleanupPlatforms();

        // 7. 更新追踪平台索引
        UpdateTrackedPlatformIndices();

        // 8. 更新分数和动态难度
        UpdateScore();
        UpdateDynamicSpeed();

        // 9. 检查游戏失败条件
        CheckGameFailCondition();
    }

    /// <summary>
    /// 处理角色水平移动（根据手机倾斜角度）
    /// 
    /// 倾斜映射：
    /// - xRotation = -90°：人物静止不动（中立位置）
    /// - xRotation = -45°：向右最大速度
    /// - xRotation = -135°：向左最大速度
    /// </summary>
    private void HandleCharacterMovement()
    {
        // 获取倾斜输入（-1到1）
        float tiltInput = phoneDetector.GetTiltInput();

        // 计算移动方向和速度
        // tiltInput范围：-1（向左最快）到 1（向右最快）
        float targetX = tiltInput * (gameAreaWidth / 2f - characterRadius);

        // 限制角色在屏幕范围内
        targetX = Mathf.Clamp(targetX, -gameAreaWidth / 2f + characterRadius, gameAreaWidth / 2f - characterRadius);

        // 使用Lerp平滑移动（可选，根据需要调整）
        // 更大的时间系数会使移动更快反应倾斜变化
        float moveResponseTime = 0.1f; // 移动响应时间（秒）
        characterPosition.x = Mathf.Lerp(characterPosition.x, targetX, Time.deltaTime / moveResponseTime);

        // Y轴始终锁定在屏幕中心
        characterPosition.y = 0f;

        characterRect.anchoredPosition = characterPosition;

        if (showDebugInfo)
        {
            // 在Update中打印一次（避免过度输出）
            // Debug.Log($"[ClimbingGameUI] 倾斜输入: {tiltInput:F3}, 角色X: {characterPosition.x:F2}");
        }
    }

    /// <summary>
    /// 更新所有平台的位置
    /// 包括处理平台的上升和下降动画
    /// </summary>
    private void UpdatePlatformsPosition()
    {
        // 处理平台下降减速动画
        if (isPlatformDecelerating)
        {
            platformDecelerateTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(platformDecelerateTimer / platformDownDuration);
            float easeProgress = Mathf.SmoothStep(0f, 1f, progress);
            platformsCurrentY = platformDecelerateStartY - (platformDownDistance * easeProgress);

            if (progress >= 1f)
            {
                isPlatformDecelerating = false;
                platformsCurrentY = platformDecelerateStartY - platformDownDistance;
            }
        }
        else
        {
            // 平台持续向上升（使用动态速度或基础速度）
            float currentSpeed = enableDynamicSpeed ? currentDynamicSpeed : platformUpSpeed;
            platformsCurrentY += currentSpeed * Time.deltaTime;
        }

        highestY = Mathf.Max(highestY, platformsCurrentY);

        // 应用位置到所有平台（保留X位置，只更新Y位置）
        for (int i = 0; i < activePlatforms.Count; i++)
        {
            if (activePlatforms[i] == null) continue;
            
            float finalY = platformsCurrentY + platformSpawnY[i];
            Vector2 currentPos = activePlatforms[i].anchoredPosition;
            activePlatforms[i].anchoredPosition = new Vector2(currentPos.x, finalY);
        }

        // 更新最高平台距离屏幕上边界的距离（用于监控游戏进度）
        if (activePlatforms.Count > 0)
        {
            float highestPlatformY = float.MinValue;
            foreach (var platform in activePlatforms)
            {
                if (platform != null)
                {
                    highestPlatformY = Mathf.Max(highestPlatformY, platform.anchoredPosition.y);
                }
            }
            
            if (highestPlatformY > float.MinValue)
            {
                float platformTopY = highestPlatformY + platformHeight / 2f;
                distanceToTopBoundary = (gameAreaHeight / 2f) - platformTopY;
            }
        }
    }

    /// <summary>
    /// 检测角色与平台的碰撞
    /// </summary>
    private void DetectPlatformCollision()
    {
        for (int i = 0; i < activePlatforms.Count; i++)
        {
            if (activePlatforms[i] == null) continue;

            RectTransform platformRect = activePlatforms[i];
            Vector2 platformPos = platformRect.anchoredPosition;

            // 平台边界
            float platformLeft = platformPos.x - platformWidth / 2f;
            float platformRight = platformPos.x + platformWidth / 2f;
            float platformTop = platformPos.y + platformHeight / 2f;
            float platformBottom = platformPos.y - platformHeight / 2f;

            // 角色与平台碰撞检测（圆形角色与矩形平台）
            bool isHorizontallyOnPlatform = characterPosition.x + characterRadius > platformLeft && 
                                          characterPosition.x - characterRadius < platformRight;
            
            bool isVerticallyOnPlatform = characterPosition.y - characterRadius <= platformTop &&
                                         characterPosition.y - characterRadius > platformBottom - 20f;

            if (isHorizontallyOnPlatform && isVerticallyOnPlatform)
            {
                platformsTouched++;
                TriggerPlatformDownfall();

                if (showDebugInfo)
                {
                    Debug.Log($"[ClimbingGameUI] 接触平台 {i}！已触碰总数: {platformsTouched}");
                }

                // 如果启用触发生成模式，在触碰平台时生成新平台
                if (useTriggerSpawning)
                {
                    GeneratePlatformBatch();
                }

                break;
            }
        }
    }

    /// <summary>
    /// 触发平台下降
    /// </summary>
    private void TriggerPlatformDownfall()
    {
        if (!isPlatformDecelerating)
        {
            isPlatformDecelerating = true;
            platformDecelerateTimer = 0f;
            platformDecelerateStartY = platformsCurrentY;
        }
    }

    /// <summary>
    /// 基于下方追踪平台生成新平台
    /// 
    /// 逻辑：
    /// - 监控下方追踪平台的位置
    /// - 在其上方固定距离（platformHeightGap）处生成新平台
    /// - 新平台的X位置在指定范围内随机
    /// - 新平台Y位置 = 下方平台Y + platformHeightGap
    /// </summary>
    private void SpawnNewPlatformsBasedOnTracking()
    {
        // 需要至少有一个下方追踪平台
        if (bottomTrackedPlatformIndex < 0 || bottomTrackedPlatformIndex >= activePlatforms.Count)
            return;

        RectTransform bottomPlatform = activePlatforms[bottomTrackedPlatformIndex];
        if (bottomPlatform == null)
            return;

        // 获取下方追踪平台的Y位置
        float bottomPlatformY = bottomPlatform.anchoredPosition.y;
        
        // 计算新平台应该生成的Y位置（在下方平台上方固定距离）
        float newPlatformY = bottomPlatformY + platformHeightGap;
        
        // 检查是否已经有平台在接近这个位置
        bool platformAlreadyExists = false;
        foreach (var platform in activePlatforms)
        {
            if (platform != null && Mathf.Abs(platform.anchoredPosition.y - newPlatformY) < 5f)
            {
                platformAlreadyExists = true;
                break;
            }
        }
        
        // 如果不存在平台在该位置，则生成（无位置限制，可以无限生成）
        if (!platformAlreadyExists)
        {
            // 随机生成X位置（在指定范围内）
            float randomXOffset = Random.Range(-platformSpawnXRange, platformSpawnXRange);
            
            // 限制X位置在屏幕范围内
            float newPlatformX = Mathf.Clamp(randomXOffset, 
                                             -gameAreaWidth / 2f + platformWidth / 2f,
                                             gameAreaWidth / 2f - platformWidth / 2f);
            
            SpawnPlatformAtPosition(newPlatformX, newPlatformY);

            if (showDebugInfo)
            {
                Debug.Log($"[ClimbingGameUI] 在下方平台{bottomTrackedPlatformIndex}上方生成新平台！位置: ({newPlatformX:F2}, {newPlatformY:F2})");
            }
        }
    }

    /// <summary>
    /// 在指定位置生成新平台
    /// </summary>
    private void SpawnPlatformAtPosition(float spawnX, float spawnWorldY)
    {
        if (platformPrefab == null || platformContainer == null)
            return;

        RectTransform newPlatform = Instantiate(platformPrefab, platformContainer).GetComponent<RectTransform>();
        newPlatform.anchoredPosition = new Vector2(spawnX, spawnWorldY);
        newPlatform.sizeDelta = new Vector2(platformWidth, platformHeight);
        
        Image platformImage = newPlatform.GetComponent<Image>();
        if (platformImage != null)
        {
            platformImage.color = platformColor;
        }

        // 记录平台信息
        activePlatforms.Add(newPlatform);
        platformSpawnY.Add(spawnWorldY - platformsCurrentY);
    }

    /// <summary>
    /// 批量生成平台（触发式生成模式）
    /// 
    /// 逻辑：
    /// 1. 从上方追踪平台开始
    /// 2. 持续生成新平台，直到最上方的平台高度超过消失距离
    /// 3. 每次生成时检查：当前平台Y + platformHeightGap 是否仍在消失距离限制内
    /// 4. 如果是，继续生成；否则停止
    /// </summary>
    private void GeneratePlatformBatch()
    {
        if (topTrackedPlatformIndex < 0 || topTrackedPlatformIndex >= activePlatforms.Count)
            return;

        RectTransform topPlatform = activePlatforms[topTrackedPlatformIndex];
        if (topPlatform == null)
            return;

        float currentTopPlatformY = topPlatform.anchoredPosition.y;
        float screenTopBoundary = gameAreaHeight / 2f;
        
        // 继续生成平台，直到最高平台的高度距离超过消失距离
        bool shouldContinueSpawning = true;
        int generationCount = 0;
        const int maxGenerationPerBatch = 20; // 防止无限循环

        while (shouldContinueSpawning && generationCount < maxGenerationPerBatch)
        {
            // 计算下一个平台的Y位置
            float nextPlatformY = currentTopPlatformY + platformHeightGap;

            // 检查下一个平台是否会超出消失距离
            float distanceAboveScreen = nextPlatformY - screenTopBoundary;
            
            // 如果新平台的位置超过消失距离限制，停止生成
            if (distanceAboveScreen > platformDisappearDistance)
            {
                shouldContinueSpawning = false;
                break;
            }

            // 检查该位置是否已有平台
            bool platformAlreadyExists = false;
            foreach (var platform in activePlatforms)
            {
                if (platform != null && Mathf.Abs(platform.anchoredPosition.y - nextPlatformY) < 5f)
                {
                    platformAlreadyExists = true;
                    break;
                }
            }

            if (!platformAlreadyExists)
            {
                // 随机生成X位置
                float randomXOffset = Random.Range(-platformSpawnXRange, platformSpawnXRange);
                
                // 限制X位置在屏幕范围内
                float newPlatformX = Mathf.Clamp(randomXOffset, 
                                                 -gameAreaWidth / 2f + platformWidth / 2f,
                                                 gameAreaWidth / 2f - platformWidth / 2f);
                
                SpawnPlatformAtPosition(newPlatformX, nextPlatformY);

                if (showDebugInfo)
                {
                    Debug.Log($"[ClimbingGameUI] 批量生成平台！位置: ({newPlatformX:F2}, {nextPlatformY:F2})，距屏幕顶部: {distanceAboveScreen:F2}");
                }

                currentTopPlatformY = nextPlatformY;
                generationCount++;
            }
            else
            {
                // 如果该位置已有平台，尝试下一个位置
                currentTopPlatformY = nextPlatformY;
            }
        }

        // 更新顶部追踪平台
        if (topTrackedPlatformIndex >= 0 && topTrackedPlatformIndex < activePlatforms.Count)
        {
            topTrackedPlatformIndex = activePlatforms.Count - 1; // 指向最新生成的平台
        }

        if (showDebugInfo && generationCount > 0)
        {
            Debug.Log($"[ClimbingGameUI] 本次生成了 {generationCount} 个平台");
        }
    }

    /// <summary>
    /// 更新平台的Mask效果（透明度）
    /// 
    /// 根据平台是否超出视觉范围来调整其透明度：
    /// 1. 超出屏幕上方maskTopDistance距离 → 完全透明
    /// 2. 在maskTopDistance范围内 → 逐渐显示
    /// 3. 在屏幕范围内 → 完全不透明
    /// 4. 低于屏幕下方maskBottomDistance距离 → 完全透明
    /// 5. 在maskBottomDistance范围内 → 逐渐隐藏
    /// </summary>
    private void UpdatePlatformMask()
    {
        if (!enablePlatformMask)
            return;

        float screenTopBoundary = gameAreaHeight / 2f;
        float screenBottomBoundary = -gameAreaHeight / 2f;

        for (int i = 0; i < activePlatforms.Count; i++)
        {
            if (activePlatforms[i] == null)
                continue;

            Image platformImage = activePlatforms[i].GetComponent<Image>();
            if (platformImage == null)
                continue;

            Vector2 platformPos = activePlatforms[i].anchoredPosition;
            float platformY = platformPos.y;
            Color platformColor = platformImage.color;

            // 计算平台距离屏幕边界的距离
            float distanceAboveScreen = platformY - screenTopBoundary;
            float distanceBelowScreen = screenBottomBoundary - platformY;

            float alpha = 1f; // 默认完全不透明

            // 检查是否超出屏幕上方
            if (distanceAboveScreen > maskTopDistance)
            {
                alpha = 0f; // 完全隐藏
            }
            else if (distanceAboveScreen > 0)
            {
                // 在Mask范围内，逐渐显示
                alpha = 1f - (distanceAboveScreen / maskTopDistance);
            }

            // 检查是否超出屏幕下方
            else if (distanceBelowScreen > maskBottomDistance)
            {
                alpha = 0f; // 完全隐藏
            }
            else if (distanceBelowScreen > 0)
            {
                // 在Mask范围内，逐渐隐藏
                alpha = 1f - (distanceBelowScreen / maskBottomDistance);
            }

            // 应用透明度
            platformColor.a = Mathf.Clamp01(alpha);
            platformImage.color = platformColor;
        }
    }

    /// <summary>
    /// 清理超出范围的平台
    /// 
    /// 清理规则：
    /// 1. 上方平台：当平台升至屏幕上边界之上 platformDisappearDistance 距离时删除
    /// 2. 下方平台：当平台降至屏幕下边界之下 bottomDisappearDistance 距离时删除
    /// </summary>
    private void CleanupPlatforms()
    {
        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            if (activePlatforms[i] == null)
            {
                activePlatforms.RemoveAt(i);
                platformSpawnY.RemoveAt(i);
                continue;
            }

            Vector2 platformPos = activePlatforms[i].anchoredPosition;

            // 删除超出屏幕上方的平台
            if (platformPos.y > gameAreaHeight / 2f + platformDisappearDistance)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[ClimbingGameUI] 删除上方平台 {i}（位置: {platformPos.y:F2}）");
                }
                Destroy(activePlatforms[i].gameObject);
                activePlatforms.RemoveAt(i);
                platformSpawnY.RemoveAt(i);
            }
            // 删除超出屏幕下方的平台
            else if (platformPos.y < -gameAreaHeight / 2f - bottomDisappearDistance)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[ClimbingGameUI] 删除下方平台 {i}（位置: {platformPos.y:F2}）");
                }
                Destroy(activePlatforms[i].gameObject);
                activePlatforms.RemoveAt(i);
                platformSpawnY.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 更新追踪的平台索引
    /// 
    /// 上方追踪平台升级：
    /// - 当上方追踪平台升至屏幕上方后被删除，则寻找下一个存在的平台作为新的上方追踪平台
    /// 
    /// 下方追踪平台升级：
    /// - 当下方追踪平台降至屏幕下方后被删除，则寻找上面一个存在的平台作为新的下方追踪平台
    /// </summary>
    private void UpdateTrackedPlatformIndices()
    {
        // 更新上方追踪平台索引
        if (topTrackedPlatformIndex < 0 || topTrackedPlatformIndex >= activePlatforms.Count || activePlatforms[topTrackedPlatformIndex] == null)
        {
            // 上方追踪平台已不存在，寻找下一个平台
            topTrackedPlatformIndex = -1;
            
            // 优先选择最上方的存在的平台
            for (int i = activePlatforms.Count - 1; i >= 0; i--)
            {
                if (activePlatforms[i] != null)
                {
                    topTrackedPlatformIndex = i;
                    if (showDebugInfo)
                    {
                        Debug.Log($"[ClimbingGameUI] 上方追踪平台升级到索引 {i}");
                    }
                    break;
                }
            }
        }

        // 更新下方追踪平台索引
        if (bottomTrackedPlatformIndex < 0 || bottomTrackedPlatformIndex >= activePlatforms.Count || activePlatforms[bottomTrackedPlatformIndex] == null)
        {
            // 下方追踪平台已不存在，寻找上面的平台
            bottomTrackedPlatformIndex = -1;
            
            // 选择最下方的存在的平台
            for (int i = 0; i < activePlatforms.Count; i++)
            {
                if (activePlatforms[i] != null)
                {
                    bottomTrackedPlatformIndex = i;
                    if (showDebugInfo)
                    {
                        Debug.Log($"[ClimbingGameUI] 下方追踪平台升级到索引 {i}");
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 检查游戏失败条件
    /// 
    /// 失败条件1：下方追踪平台不存在（已被删除）
    /// 失败条件2：没有任何活跃的平台存在（所有平台都被删除）
    /// 失败条件3：无法找到任何有效的平台用于继续游戏
    /// </summary>
    private void CheckGameFailCondition()
    {
        // 检查下方追踪平台是否有效
        bool bottomTrackedValid = bottomTrackedPlatformIndex >= 0 && 
                                 bottomTrackedPlatformIndex < activePlatforms.Count && 
                                 activePlatforms[bottomTrackedPlatformIndex] != null;

        if (!bottomTrackedValid)
        {
            // 尝试找到任何存活的平台
            bool hasAnyActivePlatform = false;
            for (int i = 0; i < activePlatforms.Count; i++)
            {
                if (activePlatforms[i] != null)
                {
                    hasAnyActivePlatform = true;
                    break;
                }
            }

            // 如果没有任何活跃平台，游戏失败
            if (!hasAnyActivePlatform)
            {
                if (showDebugInfo)
                {
                    Debug.Log("[ClimbingGameUI] 游戏失败：所有平台都已删除，找不到下一个平台！");
                }
                GameOver();
            }
            // 如果有活跃平台但下方追踪平台不存在，说明追踪系统已自动升级
            // 这不算失败，会在下一帧的UpdateTrackedPlatformIndices中重新指定
            else if (showDebugInfo)
            {
                Debug.Log("[ClimbingGameUI] 下方追踪平台已删除，正在寻找替代平台...");
            }
        }
    }

    /// <summary>
    /// 游戏结束处理
    /// </summary>
    private void GameOver()
    {
        if (isGameRunning)
        {
            isGameRunning = false;
            isGameOverAnimating = true;
            gameOverFlashTimer = 0f;

            Debug.Log($"[ClimbingGameUI] 游戏结束！最高高度: {highestY:F2}, 触碰平台数: {platformsTouched}");
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        // 清空平台
        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            if (activePlatforms[i] != null)
            {
                Destroy(activePlatforms[i].gameObject);
            }
        }
        activePlatforms.Clear();
        platformSpawnY.Clear();

        // 重置状态
        isGameRunning = true;
        isGameOverAnimating = false;
        platformsTouched = 0;
        highestY = 0f;
        platformsCurrentY = 0f;
        isPlatformDecelerating = false;
        distanceToTopBoundary = 0f;
        currentScore = 0f; // 重置当前分数
        finalScore = 0f; // 重置最终分数
        currentDynamicSpeed = basePlatformUpSpeed; // 重置动态速度

        characterPosition = Vector2.zero;
        characterRect.anchoredPosition = characterPosition;

        phoneDetector.Calibrate();

        // 生成初始平台
        if (platformPrefab != null && platformContainer != null)
        {
            RectTransform initialPlatform = Instantiate(platformPrefab, platformContainer).GetComponent<RectTransform>();
            initialPlatform.anchoredPosition = new Vector2(0, initialPlatformYOffset); // 玩家下方，偏移量可在Inspector调整
            initialPlatform.sizeDelta = new Vector2(platformWidth, platformHeight);
            
            Image platformImage = initialPlatform.GetComponent<Image>();
            if (platformImage != null)
            {
                platformImage.color = platformColor;
            }

            activePlatforms.Add(initialPlatform);
            platformSpawnY.Add(initialPlatformYOffset);

            // 初始化追踪
            topTrackedPlatformIndex = 0;
            bottomTrackedPlatformIndex = 0;
            distanceToTopBoundary = gameAreaHeight / 2f - platformHeight / 2f;

            if (showDebugInfo)
            {
                Debug.Log($"[ClimbingGameUI] 游戏已重启！初始平台Y: {initialPlatformYOffset:F2}, 平台高度间隔: {platformHeightGap:F2}");
            }

            // 如果启用触发生成，游戏开始时生成一批初始平台
            if (useTriggerSpawning)
            {
                GeneratePlatformBatch();
            }
        }
    }

    /// <summary>
    /// 启动游戏
    /// </summary>
    public void StartGame()
    {
        if (gameCanvas == null)
        {
            gameCanvas = gameAreaRect.GetComponentInParent<Canvas>();
        }

        if (!gameCanvas.gameObject.activeSelf)
        {
            gameCanvas.gameObject.SetActive(true);
        }

        RestartGame();
        Debug.Log("[ClimbingGameUI] 游戏已启动");
    }

    /// <summary>
    /// 停止游戏
    /// </summary>
    public void StopGame()
    {
        isGameRunning = false;

        if (gameCanvas == null)
        {
            gameCanvas = gameAreaRect.GetComponentInParent<Canvas>();
        }

        gameCanvas.gameObject.SetActive(false);
        Debug.Log("[ClimbingGameUI] 游戏已关闭");
    }

    /// <summary>
    /// 创建游戏结束红屏闪烁UI
    /// </summary>
    private void CreateGameOverFlashUI()
    {
        if (gameCanvas == null)
            return;

        Transform existingFlash = gameCanvas.transform.Find("GameOverFlash");
        if (existingFlash != null)
        {
            gameOverFlashImage = existingFlash.GetComponent<Image>();
            return;
        }

        GameObject flashGO = new GameObject("GameOverFlash");
        flashGO.transform.SetParent(gameCanvas.transform, false);
        
        RectTransform flashRect = flashGO.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        gameOverFlashImage = flashGO.AddComponent<Image>();
        gameOverFlashImage.color = new Color(1f, 0f, 0f, 0f);
        gameOverFlashImage.raycastTarget = false;

        flashGO.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 创建分数显示UI
    /// </summary>

    /// <summary>
    /// 更新分数显示（显示平台一共向上升的距离）
    /// 分数 = platformsCurrentY，代表平台总共向上升过多少，也就是玩家总共下降了多少（绝对值）
    /// 分数从 0 开始，随着游戏进行持续增加，游戏重启时重置为 0
    /// 直接更新 Inspector 中指定的 TextMeshProUGUI 组件
    /// </summary>
    private void UpdateScore()
    {
        if (!enableScoreSystem)
            return;

        // 分数 = platformsCurrentY（平台向上升的总距离）
        currentScore = platformsCurrentY;

        // 计算当前分数的绝对值
        float absoluteScore = Mathf.Abs(currentScore);

        // 判断当前分数的绝对值是否高于最终分数
        // 如果高于，更新最终分数；否则保持最终分数不变
        if (absoluteScore > finalScore)
        {
            finalScore = absoluteScore;
        }

        // 更新分数UI显示（显示最终分数，而不是当前分数）
        if (scoreText != null)
        {
            // 根据scoreDisplayScale显示分数（可以用来放大或缩小显示的数字）
            int displayScore = Mathf.RoundToInt(finalScore * scoreDisplayScale);
            scoreText.text = $"score: {displayScore}";
        }
    }

    /// <summary>
    /// 更新动态平台上升速度
    /// 
    /// 逻辑：
    /// currentSpeed = basePlatformUpSpeed + (currentScore / 100) * speedIncreasePerScore
    /// 例如：
    ///   score = 0：speed = 50
    ///   score = 100：speed = 50 + 0.05 = 50.05
    ///   score = 1000：speed = 50 + 0.5 = 50.5
    ///   score = 10000：speed = 50 + 5 = 55
    /// </summary>
    private void UpdateDynamicSpeed()
    {
        if (!enableDynamicSpeed)
        {
            currentDynamicSpeed = platformUpSpeed;
            return;
        }

        // 计算速度增幅
        float speedIncrease = (currentScore / 100f) * speedIncreasePerScore;
        currentDynamicSpeed = basePlatformUpSpeed + speedIncrease;

        // 限制最大速度
        currentDynamicSpeed = Mathf.Min(currentDynamicSpeed, maxPlatformUpSpeed);

        if (showDebugInfo && Mathf.FloorToInt(currentScore) % 500 == 0)
        {
            Debug.Log($"[ClimbingGameUI] 分数: {currentScore:F0}, 速度: {currentDynamicSpeed:F2} px/s");
        }
    }
    private void HandleGameOverAnimation()
    {
        if (gameOverFlashImage == null)
            return;

        gameOverFlashTimer += Time.deltaTime;
        float flashDuration = 0.3f;
        float totalFlashTime = flashDuration * 2f * 2f;

        if (gameOverFlashTimer >= totalFlashTime)
        {
            isGameOverAnimating = false;
            gameOverFlashImage.color = new Color(1f, 0f, 0f, 0f);
            
            if (gameCanvas != null)
            {
                gameCanvas.gameObject.SetActive(false);
            }

            Debug.Log("[ClimbingGameUI] 游戏已关闭");
            return;
        }

        float cycleTime = gameOverFlashTimer % (flashDuration * 2f);
        float alpha = cycleTime < flashDuration ? 1f : 0f;

        gameOverFlashImage.color = new Color(1f, 0f, 0f, alpha);
    }

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            GUILayout.BeginArea(new Rect(10, 170, 450, 550));
            GUILayout.Box("=== 平台追踪系统 ===");

            // 角色和高度信息
            GUILayout.Label($"角色位置 X: {characterPosition.x:F1}");
            GUILayout.Label($"平台Y偏移: {platformsCurrentY:F1}");
            GUILayout.Label($"激活平台数: {activePlatforms.Count}");
            GUILayout.Label($"已触碰平台: {platformsTouched}");
            GUILayout.Label($"最高高度: {highestY:F2}");
            
            GUILayout.Space(5);
            GUILayout.Label("=== 追踪信息 ===");
            GUILayout.Label($"上方追踪索引: {topTrackedPlatformIndex}");
            GUILayout.Label($"下方追踪索引: {bottomTrackedPlatformIndex}");
            GUILayout.Label($"最高平台到上边界: {distanceToTopBoundary:F2}");
            GUILayout.Label($"平台高度间隔: {platformHeightGap:F2}");
            GUILayout.Label($"平台X位置范围: ±{platformSpawnXRange:F2}");
            
            GUILayout.Space(5);
            GUILayout.Label("=== 状态信息 ===");
            GUILayout.Label($"平台减速中: {isPlatformDecelerating}");
            GUILayout.Label($"游戏运行中: {isGameRunning}");
            GUILayout.Label($"游戏结束动画: {isGameOverAnimating}");

            GUILayout.Space(10);
            GUILayout.Label("=== 平台列表 ===");
            for (int i = 0; i < activePlatforms.Count; i++)
            {
                if (activePlatforms[i] != null)
                {
                    Vector2 pos = activePlatforms[i].anchoredPosition;
                    string marker = "";
                    if (i == topTrackedPlatformIndex) marker += "[TOP] ";
                    if (i == bottomTrackedPlatformIndex) marker += "[BOT] ";
                    GUILayout.Label($"{marker}平台{i}: Y={pos.y:F1}");
                }
            }

            GUILayout.Space(15);
            GUILayout.Label("=== 游戏控制 ===");

            if (!isGameRunning && !isGameOverAnimating && GUILayout.Button("▶ 启动游戏", GUILayout.Height(40)))
            {
                StartGame();
            }

            if (isGameRunning)
            {
                if (GUILayout.Button("⏸ 暂停游戏", GUILayout.Height(40)))
                {
                    StopGame();
                }

                if (GUILayout.Button("🔄 重新开始", GUILayout.Height(40)))
                {
                    RestartGame();
                }
            }

            if (isGameOverAnimating && GUILayout.Button("🔄 重新开始", GUILayout.Height(40)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// Debug用：右键脚本选择"Start Game"来启动游戏
    /// </summary>
    [ContextMenu("Start Game")]
    public void DebugStartGame()
    {
        StartGame();
        if (showDebugInfo)
        {
            Debug.Log("[ClimbingGameUI] 通过ContextMenu启动游戏");
        }
    }

    /// <summary>
    /// Debug用：右键脚本选择"Restart Game"来重新开始游戏
    /// </summary>
    [ContextMenu("Restart Game")]
    public void DebugRestartGame()
    {
        RestartGame();
        if (showDebugInfo)
        {
            Debug.Log("[ClimbingGameUI] 通过ContextMenu重新开始游戏");
        }
    }

    /// <summary>
    /// Debug用：右键脚本选择"Stop Game"来停止游戏
    /// </summary>
    [ContextMenu("Stop Game")]
    public void DebugStopGame()
    {
        StopGame();
        if (showDebugInfo)
        {
            Debug.Log("[ClimbingGameUI] 通过ContextMenu停止游戏");
        }
    }

    /// <summary>
    /// Debug用：诊断游戏当前状态
    /// </summary>
    [ContextMenu("Diagnose Game State")]
    public void DiagnoseGameState()
    {
        Debug.Log("=== 游戏诊断信息 ===");
        Debug.Log($"游戏运行中: {isGameRunning}");
        Debug.Log($"游戏结束动画: {isGameOverAnimating}");
        Debug.Log($"激活平台数: {activePlatforms.Count}");
        Debug.Log($"上方追踪索引: {topTrackedPlatformIndex}");
        Debug.Log($"下方追踪索引: {bottomTrackedPlatformIndex}");
        Debug.Log($"最高平台到上边界: {distanceToTopBoundary:F2}");
        Debug.Log($"平台高度间隔: {platformHeightGap:F2}");
        Debug.Log($"平台X随机范围: ±{platformSpawnXRange:F2}");
        Debug.Log($"角色位置: ({characterPosition.x:F1}, {characterPosition.y:F1})");
        Debug.Log($"平台Y偏移: {platformsCurrentY:F1}");
        Debug.Log($"触碰平台数: {platformsTouched}");
        Debug.Log($"最高高度: {highestY:F2}");
        
        Debug.Log("=== 平台详情 ===");
        for (int i = 0; i < activePlatforms.Count; i++)
        {
            if (activePlatforms[i] != null)
            {
                Vector2 pos = activePlatforms[i].anchoredPosition;
                Debug.Log($"平台{i}: Y={pos.y:F1} (生成Y={platformSpawnY[i]:F1})");
            }
        }
    }
}