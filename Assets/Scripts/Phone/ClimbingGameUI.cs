using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private float moveSpeed = 800f; // 水平移动速度
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
    [SerializeField] private float spawnDistanceThreshold = 150f; // 上方平台距离屏幕上边界多远时生成新平台
    [SerializeField] private float platformDisappearDistance = 100f; // 平台超出屏幕上方多远时删除
    [SerializeField] private float bottomDisappearDistance = 100f; // 平台低于屏幕下方多远时删除

    [Header("游戏设置")]
    [SerializeField] private PhoneShakeDetector phoneDetector;

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

        // 4. 基于上方追踪平台生成新平台
        SpawnNewPlatformsBasedOnTracking();

        // 5. 清理超出范围的平台
        CleanupPlatforms();

        // 6. 更新追踪平台索引
        UpdateTrackedPlatformIndices();

        // 7. 检查游戏失败条件
        CheckGameFailCondition();
    }

    /// <summary>
    /// 处理角色水平移动（根据手机倾斜）
    /// </summary>
    private void HandleCharacterMovement()
    {
        float tiltInput = phoneDetector.GetTiltInput();
        float targetX = tiltInput * (gameAreaWidth / 2f - characterRadius);
        targetX = Mathf.Clamp(targetX, -gameAreaWidth / 2f + characterRadius, gameAreaWidth / 2f - characterRadius);
        
        characterPosition.x = Mathf.Lerp(characterPosition.x, targetX, moveSpeed * Time.deltaTime / 1000f);
        characterPosition.y = 0f; // Y始终锁定在0
        
        characterRect.anchoredPosition = characterPosition;
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
            // 平台持续向上升
            platformsCurrentY += platformUpSpeed * Time.deltaTime;
        }

        highestY = Mathf.Max(highestY, platformsCurrentY);

        // 应用位置到所有平台
        for (int i = 0; i < activePlatforms.Count; i++)
        {
            if (activePlatforms[i] == null) continue;
            
            float finalY = platformsCurrentY + platformSpawnY[i];
            activePlatforms[i].anchoredPosition = new Vector2(0, finalY);
        }

        // 更新上方追踪平台距离屏幕上边界的距离
        if (topTrackedPlatformIndex >= 0 && topTrackedPlatformIndex < activePlatforms.Count)
        {
            RectTransform trackedPlatform = activePlatforms[topTrackedPlatformIndex];
            if (trackedPlatform != null)
            {
                float platformTopY = trackedPlatform.anchoredPosition.y + platformHeight / 2f;
                // 屏幕上边界是gameAreaHeight/2，所以距离 = 上边界 - 平台顶部
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
    /// 基于上方追踪平台生成新平台
    /// 
    /// 逻辑：
    /// - 监控上方追踪平台到屏幕上边界的距离
    /// - 当距离大于阈值时，在屏幕顶部生成新平台
    /// - 新平台会在后续的UpdateTrackedPlatformIndices中成为新的上方追踪平台
    /// </summary>
    private void SpawnNewPlatformsBasedOnTracking()
    {
        // 需要至少有一个被追踪的平台
        if (topTrackedPlatformIndex < 0 || topTrackedPlatformIndex >= activePlatforms.Count)
            return;

        // 当距离大于阈值时，生成新平台
        if (distanceToTopBoundary > spawnDistanceThreshold)
        {
            // 新平台在屏幕顶部生成
            float spawnPositionY = gameAreaHeight / 2f - platformHeight / 2f; // 屏幕顶部边缘
            
            SpawnPlatform(spawnPositionY);

            if (showDebugInfo)
            {
                Debug.Log($"[ClimbingGameUI] 生成新平台！距离: {distanceToTopBoundary:F2} > 阈值: {spawnDistanceThreshold}");
            }
        }
    }

    /// <summary>
    /// 生成新平台
    /// </summary>
    private void SpawnPlatform(float spawnWorldY)
    {
        if (platformPrefab == null || platformContainer == null)
            return;

        RectTransform newPlatform = Instantiate(platformPrefab, platformContainer).GetComponent<RectTransform>();
        newPlatform.anchoredPosition = new Vector2(0, spawnWorldY);
        newPlatform.sizeDelta = new Vector2(platformWidth, platformHeight);
        
        Image platformImage = newPlatform.GetComponent<Image>();
        if (platformImage != null)
        {
            platformImage.color = platformColor;
        }

        // 记录平台信息
        // 注意：这里spawnWorldY已经包含了当前的platformsCurrentY偏移
        // 所以initialY = spawnWorldY - platformsCurrentY
        activePlatforms.Add(newPlatform);
        platformSpawnY.Add(spawnWorldY - platformsCurrentY);
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
        if (topTrackedPlatformIndex >= activePlatforms.Count || activePlatforms[topTrackedPlatformIndex] == null)
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
        if (bottomTrackedPlatformIndex >= activePlatforms.Count || activePlatforms[bottomTrackedPlatformIndex] == null)
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

        characterPosition = Vector2.zero;
        characterRect.anchoredPosition = characterPosition;

        phoneDetector.Calibrate();

        // 生成初始平台
        if (platformPrefab != null && platformContainer != null)
        {
            RectTransform initialPlatform = Instantiate(platformPrefab, platformContainer).GetComponent<RectTransform>();
            initialPlatform.anchoredPosition = new Vector2(0, 0); // 玩家正下方
            initialPlatform.sizeDelta = new Vector2(platformWidth, platformHeight);
            
            Image platformImage = initialPlatform.GetComponent<Image>();
            if (platformImage != null)
            {
                platformImage.color = platformColor;
            }

            activePlatforms.Add(initialPlatform);
            platformSpawnY.Add(0f);

            // 初始化追踪
            topTrackedPlatformIndex = 0;
            bottomTrackedPlatformIndex = 0;
            // 初始距离 = 屏幕上边界 - 初始平台顶部 = gameAreaHeight/2 - platformHeight/2
            distanceToTopBoundary = gameAreaHeight / 2f - platformHeight / 2f;

            if (showDebugInfo)
            {
                Debug.Log($"[ClimbingGameUI] 游戏已重启！初始距离到上边界: {distanceToTopBoundary:F2}");
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
    /// 处理游戏结束的红屏闪烁动画
    /// </summary>
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
            GUILayout.Label($"到上边界距离: {distanceToTopBoundary:F2}");
            GUILayout.Label($"生成阈值: {spawnDistanceThreshold:F2}");
            
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
        Debug.Log($"到上边界距离: {distanceToTopBoundary:F2}");
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