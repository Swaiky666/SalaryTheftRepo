using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 爬平台小游戏UI版本
/// 挂在Canvas或其他UI管理器上，用RectTransform管理所有元素
/// </summary>
public class ClimbingGameUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Canvas gameCanvas; // 游戏Canvas
    [SerializeField] private RectTransform gameAreaRect; // 游戏区域（背景）
    [SerializeField] private RectTransform characterRect; // 角色Image的RectTransform
    [SerializeField] private Image characterImage; // 角色Image组件
    [SerializeField] private Transform platformContainer; // 平台容器（用于生成平台）
    [SerializeField] private Image platformPrefab; // 平台预制体（Image组件）

    [Header("角色设置")]
    [SerializeField] private float moveSpeed = 800f; // 水平移动速度（像素/秒）
    [SerializeField] private float jumpForce = 500f; // 跳跃力度
    [SerializeField] private float fallSpeedThreshold = 50f; // 触发跳跃的最小下降速度
    [SerializeField] private float characterRadius = 20f; // 角色碰撞半径（像素）

    [Header("平台设置")]
    [SerializeField] private float platformWidth = 100f; // 平台宽度（像素）
    [SerializeField] private float platformHeight = 15f; // 平台高度（像素）
    [SerializeField] private float platformSpawnInterval = 1.5f; // 平台生成间隔
    [SerializeField] private Color platformColor = Color.white; // 平台颜色

    [Header("游戏设置")]
    [SerializeField] private float jumpCooldown = 0.1f; // 跳跃冷却
    [SerializeField] private float gravity = 1000f; // 重力加速度（像素/秒²）
    [SerializeField] private PhoneShakeDetector phoneDetector; // 手机检测器引用

    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;

    // 内部变量
    private Vector2 characterPosition; // 角色位置（像素坐标，相对于gameAreaRect）
    private Vector2 characterVelocity; // 角色速度
    private float gameAreaWidth;
    private float gameAreaHeight;
    private Vector2 gameAreaStartPos; // 游戏区域的起始位置
    
    private bool isOnGround = false;
    private bool isJumping = false;
    private float lastJumpTime = -1f;
    private float highestPosition = 0f;

    private List<RectTransform> activePlatforms = new List<RectTransform>();
    private float lastPlatformSpawnTime = 0f;

    private bool gameActive = true;
    private int platformsClimbed = 0;

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

        // 获取游戏区域的尺寸
        gameAreaWidth = gameAreaRect.rect.width;
        gameAreaHeight = gameAreaRect.rect.height;
        gameAreaStartPos = gameAreaRect.anchoredPosition;

        // 初始化角色位置（屏幕中心底部）
        characterPosition = new Vector2(0, 50f);
        characterVelocity = Vector2.zero;
        highestPosition = characterPosition.y;

        phoneDetector.Calibrate();

        Debug.Log($"[ClimbingGameUI] 游戏初始化完成。游戏区域: {gameAreaWidth}x{gameAreaHeight}");
    }

    private void Update()
    {
        if (!gameActive || characterRect == null)
            return;

        // 处理水平移动
        HandleHorizontalMovement();

        // 处理垂直运动
        HandleVerticalMovement();

        // 检测平台碰撞
        DetectPlatformCollision();

        // 检测失败条件
        CheckFailCondition();

        // 生成平台
        SpawnPlatforms();

        // 清理平台
        CleanupPlatforms();

        // 更新角色视觉位置
        UpdateCharacterVisuals();

        // 更新最高位置
        if (characterPosition.y > highestPosition)
        {
            highestPosition = characterPosition.y;
        }
    }

    /// <summary>
    /// 处理水平移动 - 根据手机倾斜
    /// </summary>
    private void HandleHorizontalMovement()
    {
        float tiltInput = phoneDetector.GetTiltInput();

        // 计算目标X位置
        float targetX = tiltInput * (gameAreaWidth / 2f - characterRadius);
        
        // 平滑移动
        characterPosition.x = Mathf.Lerp(characterPosition.x, targetX, moveSpeed * Time.deltaTime / 1000f);
        
        // 限制在游戏区域内
        characterPosition.x = Mathf.Clamp(characterPosition.x, -gameAreaWidth / 2f + characterRadius, gameAreaWidth / 2f - characterRadius);
    }

    /// <summary>
    /// 处理垂直运动 - 重力和跳跃
    /// </summary>
    private void HandleVerticalMovement()
    {
        // 应用重力
        characterVelocity.y -= gravity * Time.deltaTime;

        // 限制下降速度，防止下坠太快
        characterVelocity.y = Mathf.Max(characterVelocity.y, -800f);

        // 更新位置
        characterPosition.y += characterVelocity.y * Time.deltaTime;

        // 检测可以跳跃的条件
        if (isOnGround && characterVelocity.y < fallSpeedThreshold && Time.time - lastJumpTime > jumpCooldown)
        {
            // 自动跳跃
            characterVelocity.y = jumpForce;
            isJumping = true;
            lastJumpTime = Time.time;
            isOnGround = false;

            if (showDebugInfo)
            {
                Debug.Log($"[ClimbingGameUI] 跳跃！当前高度: {characterPosition.y:F2}");
            }
        }
    }

    /// <summary>
    /// 检测与平台的碰撞
    /// </summary>
    private void DetectPlatformCollision()
    {
        isOnGround = false;

        // 仅在下坠时检测碰撞
        if (!isJumping && characterVelocity.y <= 0)
        {
            foreach (RectTransform platformRect in activePlatforms)
            {
                if (platformRect == null) continue;

                Vector2 platformPos = platformRect.anchoredPosition;
                float platformLeft = platformPos.x - platformWidth / 2f;
                float platformRight = platformPos.x + platformWidth / 2f;
                float platformTop = platformPos.y + platformHeight / 2f;
                float platformBottom = platformPos.y - platformHeight / 2f;

                // 检查角色是否在平台上
                bool isHorizontallyOnPlatform = characterPosition.x + characterRadius > platformLeft && 
                                               characterPosition.x - characterRadius < platformRight;
                
                bool isVerticallyOnPlatform = characterPosition.y - characterRadius <= platformTop &&
                                              characterPosition.y - characterRadius > platformBottom - 20f;

                if (isHorizontallyOnPlatform && isVerticallyOnPlatform)
                {
                    isOnGround = true;
                    characterPosition.y = platformTop + characterRadius;
                    characterVelocity.y = 0;
                    platformsClimbed++;

                    if (showDebugInfo)
                    {
                        Debug.Log($"[ClimbingGameUI] 接触平台！总共: {platformsClimbed}");
                    }
                    break;
                }
            }
        }

        // 上升阶段结束
        if (isJumping && characterVelocity.y <= 0)
        {
            isJumping = false;
        }
    }

    /// <summary>
    /// 检测失败条件
    /// </summary>
    private void CheckFailCondition()
    {
        // 如果角色掉出游戏区域底部
        if (characterPosition.y < -gameAreaHeight / 2f - 50f)
        {
            GameOver();
        }
    }

    /// <summary>
    /// 生成平台
    /// </summary>
    private void SpawnPlatforms()
    {
        if (platformPrefab == null || platformContainer == null)
            return;

        if (Time.time - lastPlatformSpawnTime > platformSpawnInterval)
        {
            // 平台生成高度（始终在角色上方）
            float spawnY = characterPosition.y + gameAreaHeight * 0.6f;

            // 随机X位置
            float randomX = Random.Range(-gameAreaWidth / 2f + platformWidth / 2f, gameAreaWidth / 2f - platformWidth / 2f);

            // 创建平台
            RectTransform platformRect = Instantiate(platformPrefab, platformContainer).GetComponent<RectTransform>();
            platformRect.anchoredPosition = new Vector2(randomX, spawnY);
            platformRect.sizeDelta = new Vector2(platformWidth, platformHeight);
            platformRect.GetComponent<Image>().color = platformColor;

            activePlatforms.Add(platformRect);
            lastPlatformSpawnTime = Time.time;
        }
    }

    /// <summary>
    /// 清理超出屏幕的平台
    /// </summary>
    private void CleanupPlatforms()
    {
        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            if (activePlatforms[i] == null || activePlatforms[i].anchoredPosition.y < characterPosition.y - gameAreaHeight)
            {
                if (activePlatforms[i] != null)
                {
                    Destroy(activePlatforms[i].gameObject);
                }
                activePlatforms.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 更新角色的视觉显示
    /// </summary>
    private void UpdateCharacterVisuals()
    {
        characterRect.anchoredPosition = characterPosition;
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    private void GameOver()
    {
        gameActive = false;

        Debug.Log($"[ClimbingGameUI] 游戏结束！最高高度: {highestPosition:F2}, 经过平台数: {platformsClimbed}");

        // 可以在这里显示游戏结束UI
        // ShowGameOverPanel();
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        // 重置位置和速度
        characterPosition = new Vector2(0, 50f);
        characterVelocity = Vector2.zero;

        // 清空平台
        foreach (RectTransform platform in activePlatforms)
        {
            if (platform != null)
            {
                Destroy(platform.gameObject);
            }
        }
        activePlatforms.Clear();

        // 重置游戏状态
        gameActive = true;
        isOnGround = false;
        isJumping = false;
        platformsClimbed = 0;
        highestPosition = 50f;
        lastPlatformSpawnTime = 0f;

        phoneDetector.Calibrate();

        Debug.Log("[ClimbingGameUI] 游戏已重启");
    }

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            GUILayout.BeginArea(new Rect(10, 170, 300, 200));
            GUILayout.Box("Climbing Game UI State");

            GUILayout.Label($"Character Pos: ({characterPosition.x:F1}, {characterPosition.y:F1})");
            GUILayout.Label($"Character Vel: ({characterVelocity.x:F1}, {characterVelocity.y:F1})");
            GUILayout.Label($"Highest Height: {highestPosition:F2}");
            GUILayout.Label($"Platforms Climbed: {platformsClimbed}");
            GUILayout.Label($"Active Platforms: {activePlatforms.Count}");
            GUILayout.Label($"On Ground: {isOnGround}");
            GUILayout.Label($"Is Jumping: {isJumping}");
            GUILayout.Label($"Game Active: {gameActive}");

            if (!gameActive && GUILayout.Button("Restart Game", GUILayout.Height(30)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }
    }
}