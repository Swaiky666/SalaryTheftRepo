using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class VRFoodConsumer : MonoBehaviour
{
    [Header("激光检测设置")]
    public Transform headTransform; // 头部Transform（通常是Main Camera）
    public float laserLength = 0.5f; // 激光长度
    public float coneAngle = 30f; // 圆锥角度（度）
    public LayerMask foodLayerMask = -1; // 食物层级遮罩

    [Header("消费设置")]
    public float consumeDuration = 2f; // 吃东西的持续时间（秒）
    public ParticleSystem eatingEffectPrefab; // 吃东西时的粒子系统预制件
    public Transform eatingEffectSpawnPoint; // 粒子系统生成位置（例如嘴巴位置）

    [Header("UI设置")]
    public Slider speedBoostSlider; // 加速buff倒计时滑块（拖拽你的Slider到这里）

    [Header("调试设置")]
    public bool showDebugLaser = true; // 是否显示激光射线

    // 私有变量
    private CharacterController characterController;
    private GameLogicSystem gameLogicSystem;
    private CharacterStatus characterStatus; // 添加角色状态引用

    // 消费状态
    private bool isEating = false; // 是否正在吃东西
    private Coroutine eatingCoroutine;
    private ParticleSystem currentEatingEffect; // 当前实例化的粒子系统

    // 速度加成相关
    private bool hasSpeedBoost = false;
    private float originalMoveSpeed = 2f; // 原始移动速度
    private float boostedMoveSpeed = 2f; // 加成后的移动速度
    private Coroutine speedBoostCoroutine;

    void Start()
    {
        // 获取组件
        characterController = GetComponent<CharacterController>();
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        characterStatus = GetComponent<CharacterStatus>(); // 获取角色状态组件

        // 如果没有指定头部Transform，尝试找到Main Camera
        if (headTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                headTransform = mainCamera.transform;
            }
        }

        // 如果没有指定粒子系统生成点，使用头部Transform
        if (eatingEffectSpawnPoint == null)
        {
            eatingEffectSpawnPoint = headTransform;
        }

        // 初始化加速buff UI状态
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(false);
        }

        // 获取Character Controller的原始移动速度
        // 注意：Character Controller本身没有moveSpeed属性
        // 你可能需要根据你的移动脚本来调整这部分
        if (characterController != null)
        {
            // 这里假设你有一个移动脚本，你需要根据实际情况调整
            var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
            if (moveProvider != null)
            {
                originalMoveSpeed = moveProvider.moveSpeed;
            }
        }
    }

    void Update()
    {
        // 激光检测食物并自动消费
        DetectAndConsumeFood();
    }

    /// <summary>
    /// 激光检测食物并自动消费（圆锥形检测）
    /// </summary>
    private void DetectAndConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // 调试用圆锥激光显示
        if (showDebugLaser)
        {
            DrawDebugCone();
        }

        // 圆锥形检测
        FoodItem closestFood = DetectFoodInCone();

        if (closestFood != null)
        {
            // 激光碰到食物就直接开始吃
            if (eatingCoroutine != null)
            {
                StopCoroutine(eatingCoroutine);
            }
            eatingCoroutine = StartCoroutine(ConsumeFood(closestFood));
        }
    }

    /// <summary>
    /// 在圆锥范围内检测食物
    /// </summary>
    /// <returns>最近的食物，如果没有则返回null</returns>
    private FoodItem DetectFoodInCone()
    {
        Vector3 headPosition = headTransform.position;
        Vector3 headForward = headTransform.forward;

        // 使用OverlapSphere找到范围内的所有碰撞体
        Collider[] colliders = Physics.OverlapSphere(headPosition, laserLength, foodLayerMask);

        FoodItem closestFood = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            // 计算到食物的方向
            Vector3 directionToFood = (collider.transform.position - headPosition).normalized;

            // 计算角度
            float angle = Vector3.Angle(headForward, directionToFood);

            // 检查是否在圆锥角度内
            if (angle <= coneAngle * 0.5f)
            {
                // 检查是否有食物组件
                FoodItem foodItem = collider.GetComponent<FoodItem>();
                if (foodItem != null)
                {
                    // 找到最近的食物
                    float distance = Vector3.Distance(headPosition, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestFood = foodItem;
                    }
                }
            }
        }

        return closestFood;
    }

    /// <summary>
    /// 绘制调试用的圆锥激光
    /// </summary>
    private void DrawDebugCone()
    {
        Vector3 headPosition = headTransform.position;
        Vector3 headForward = headTransform.forward;
        Vector3 headUp = headTransform.up;
        Vector3 headRight = headTransform.right;

        // 计算圆锥末端的圆形半径
        float coneRadius = laserLength * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);

        // 圆锥末端中心点
        Vector3 coneEndCenter = headPosition + headForward * laserLength;

        // 绘制中心线
        Debug.DrawRay(headPosition, headForward * laserLength, Color.red);

        // 绘制圆锥边缘线（8条线形成圆锥轮廓）
        int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            Vector3 direction = headUp * Mathf.Sin(angle) + headRight * Mathf.Cos(angle);
            Vector3 coneEdgePoint = coneEndCenter + direction * coneRadius;

            // 从头部到圆锥边缘的线
            Debug.DrawLine(headPosition, coneEdgePoint, Color.yellow);

            // 圆锥末端的圆形轮廓
            if (i < segments - 1)
            {
                float nextAngle = (360f / segments) * (i + 1) * Mathf.Deg2Rad;
                Vector3 nextDirection = headUp * Mathf.Sin(nextAngle) + headRight * Mathf.Cos(nextAngle);
                Vector3 nextConeEdgePoint = coneEndCenter + nextDirection * coneRadius;
                Debug.DrawLine(coneEdgePoint, nextConeEdgePoint, Color.green);
            }
            else
            {
                // 连接最后一条线到第一个点
                Vector3 firstDirection = headUp * Mathf.Sin(0) + headRight * Mathf.Cos(0);
                Vector3 firstConeEdgePoint = coneEndCenter + firstDirection * coneRadius;
                Debug.DrawLine(coneEdgePoint, firstConeEdgePoint, Color.green);
            }
        }
    }

    /// <summary>
    /// 实例化吃东西的粒子特效
    /// </summary>
    private void StartEatingEffect()
    {
        if (eatingEffectPrefab != null && eatingEffectSpawnPoint != null)
        {
            // 在指定位置实例化粒子系统
            currentEatingEffect = Instantiate(eatingEffectPrefab, eatingEffectSpawnPoint.position, eatingEffectSpawnPoint.rotation);

            // 将粒子系统设置为eatingEffectSpawnPoint的子对象，这样会跟随移动
            currentEatingEffect.transform.SetParent(eatingEffectSpawnPoint);

            // 播放粒子系统
            currentEatingEffect.Play();

            Debug.Log("开始播放吃东西粒子特效");
        }
        else
        {
            Debug.LogWarning("吃东西粒子特效预制件或生成点未设置");
        }
    }

    /// <summary>
    /// 应用食物的粒子颜色到当前的吃东西特效
    /// </summary>
    /// <param name="startColor">粒子的开始颜色</param>
    /// <param name="endColor">粒子的结束颜色</param>
    private void ApplyParticleColors(Color startColor, Color endColor)
    {
        if (currentEatingEffect != null)
        {
            // 获取主模块
            var main = currentEatingEffect.main;

            // 方式一：使用双色 MinMaxGradient (推荐)
            ParticleSystem.MinMaxGradient gradient = new ParticleSystem.MinMaxGradient(startColor, endColor);

            // ⭐ 修复 CS0426 错误：直接使用 ParticleSystemGradientMode 枚举名
            // 这是 Unity C# 脚本中访问此嵌套枚举的最常见和正确方式。
            gradient.mode = ParticleSystemGradientMode.TwoColors;

            main.startColor = gradient;

            Debug.Log($"粒子颜色已设置为 Start: {startColor}, End: {endColor}");
        }
    }


    /// <summary>
    /// 停止并销毁吃东西的粒子特效
    /// </summary>
    private void StopEatingEffect()
    {
        if (currentEatingEffect != null)
        {
            // 停止粒子系统
            currentEatingEffect.Stop();

            // 等待粒子系统完全停止后销毁（可选）
            StartCoroutine(DestroyEffectAfterStop(currentEatingEffect));

            currentEatingEffect = null;

            Debug.Log("停止吃东西粒子特效");
        }
    }

    /// <summary>
    /// 等待粒子系统停止后销毁
    /// </summary>
    private IEnumerator DestroyEffectAfterStop(ParticleSystem effect)
    {
        // 等待粒子系统不再播放
        while (effect != null && effect.isPlaying)
        {
            yield return null;
        }

        // 销毁粒子系统GameObject
        if (effect != null)
        {
            Destroy(effect.gameObject);
        }
    }

    /// <summary>
    /// 消费食物协程（带延迟）
    /// </summary>
    /// <param name="foodItem">要消费的食物</param>
    private IEnumerator ConsumeFood(FoodItem foodItem)
    {
        isEating = true;

        // 设置摸鱼状态为true
        if (characterStatus != null)
        {
            characterStatus.isSlackingAtWork = true;
        }

        // 获取食物效果
        FoodEffect effect = foodItem.GetFoodEffect();

        // 开始吃东西粒子特效
        StartEatingEffect();

        // ★★★ 关键修改：应用食物的颜色属性到粒子系统 ★★★
        ApplyParticleColors(effect.particleStartColor, effect.particleEndColor);


        Debug.Log($"开始食用: {foodItem.foodName}");


        // 如果有压力减少效果，在吃东西过程中渐进式减少压力
        float stressReductionPerSecond = 0f;
        if (effect.hasStressReduction)
        {
            stressReductionPerSecond = effect.stressReduction / consumeDuration;
        }

        // 吃东西过程
        float elapsedTime = 0f;
        while (elapsedTime < consumeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 渐进式减少压力
            if (effect.hasStressReduction && gameLogicSystem != null)
            {
                float stressToReduce = stressReductionPerSecond * Time.deltaTime;
                gameLogicSystem.ReduceStress(stressToReduce);
            }

            yield return null;
        }

        // 吃完后立即应用速度加成
        if (effect.hasSpeedBoost)
        {
            ApplySpeedBoost(effect.speedMultiplier, effect.speedBoostDuration);
            Debug.Log($"获得速度加成: {effect.speedMultiplier}x, 持续{effect.speedBoostDuration}秒");
        }

        // 停止吃东西粒子特效
        StopEatingEffect();

        // 消费食物（删除物体）
        foodItem.OnConsume();

        Debug.Log($"食用完成: {foodItem.foodName}");

        isEating = false;

        // 设置摸鱼状态为false
        if (characterStatus != null)
        {
            characterStatus.isSlackingAtWork = false;
        }

        eatingCoroutine = null;
    }

    /// <summary>
    /// 应用食物效果（现在只处理速度加成，压力减少在协程中处理）
    /// </summary>
    /// <param name="effect">食物效果</param>
    private void ApplyFoodEffect(FoodEffect effect)
    {
        // 速度加成现在在协程结束时应用
        // 压力减少现在在协程过程中渐进式处理
    }

    /// <summary>
    /// 应用速度加成
    /// </summary>
    /// <param name="multiplier">速度倍数</param>
    /// <param name="duration">持续时间</param>
    private void ApplySpeedBoost(float multiplier, float duration)
    {
        // 如果已经有速度加成，先停止之前的
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }

        speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    /// <summary>
    /// 速度加成协程
    /// </summary>
    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        hasSpeedBoost = true;
        boostedMoveSpeed = originalMoveSpeed * multiplier;

        // 显示加速buff UI
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(true);
            speedBoostSlider.maxValue = duration;
            speedBoostSlider.value = duration;
        }

        // 应用速度加成到移动组件
        // 注意：这里需要根据你实际使用的移动脚本来调整
        var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = boostedMoveSpeed;
        }

        // 倒计时更新slider
        float remainingTime = duration;
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            remainingTime = Mathf.Max(0, remainingTime);

            // 更新slider值
            if (speedBoostSlider != null)
            {
                speedBoostSlider.value = remainingTime;
            }

            yield return null;
        }

        // 恢复原始速度
        hasSpeedBoost = false;
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = originalMoveSpeed;
        }

        // 隐藏加速buff UI
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(false);
        }

        speedBoostCoroutine = null;
        Debug.Log("速度加成效果结束");
    }

    /// <summary>
    /// 手动触发吃东西（如果需要的话）
    /// 现在主要是激光自动触发，这个方法作为备用
    /// </summary>
    public void ManualConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // 使用圆锥形检测
        FoodItem closestFood = DetectFoodInCone();

        if (closestFood != null)
        {
            if (eatingCoroutine != null)
            {
                StopCoroutine(eatingCoroutine);
            }
            eatingCoroutine = StartCoroutine(ConsumeFood(closestFood));
        }
    }

    /// <summary>
    /// 停止吃东西（如果需要中断的话）
    /// </summary>
    public void StopEating()
    {
        if (isEating && eatingCoroutine != null)
        {
            StopCoroutine(eatingCoroutine);

            // 停止粒子特效
            StopEatingEffect();

            isEating = false;

            // 设置摸鱼状态为false
            if (characterStatus != null)
            {
                characterStatus.isSlackingAtWork = false;
            }

            eatingCoroutine = null;
            Debug.Log("停止食用");
        }
    }

    /// <summary>
    /// 停止速度加成（如果需要中断的话）
    /// </summary>
    public void StopSpeedBoost()
    {
        if (hasSpeedBoost && speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);

            // 恢复原始速度
            hasSpeedBoost = false;
            var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
            if (moveProvider != null)
            {
                moveProvider.moveSpeed = originalMoveSpeed;
            }

            // 隐藏加速buff UI
            if (speedBoostSlider != null)
            {
                speedBoostSlider.gameObject.SetActive(false);
            }

            speedBoostCoroutine = null;
            Debug.Log("速度加成效果被中断");
        }
    }

    // 属性访问器
    public bool IsEating => isEating;
    public bool HasSpeedBoost => hasSpeedBoost;
    public float CurrentMoveSpeed => hasSpeedBoost ? boostedMoveSpeed : originalMoveSpeed;
    public float SpeedBoostTimeRemaining => speedBoostSlider != null ? speedBoostSlider.value : 0f;

    // 调试显示
    void OnDrawGizmosSelected()
    {
        if (headTransform != null)
        {
            Vector3 headPosition = headTransform.position;
            Vector3 headForward = headTransform.forward;
            Vector3 headUp = headTransform.up;
            Vector3 headRight = headTransform.right;

            // 计算圆锥末端的圆形半径
            float coneRadius = laserLength * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);
            Vector3 coneEndCenter = headPosition + headForward * laserLength;

            // 绘制圆锥轮廓
            Gizmos.color = Color.red;

            // 中心线
            Gizmos.DrawRay(headPosition, headForward * laserLength);

            // 圆锥边缘线
            int segments = 12;
            Vector3[] conePoints = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle = (360f / segments) * i * Mathf.Deg2Rad;
                Vector3 direction = headUp * Mathf.Sin(angle) + headRight * Mathf.Cos(angle);
                Vector3 coneEdgePoint = coneEndCenter + direction * coneRadius;
                conePoints[i] = coneEdgePoint;

                // 从头部到圆锥边缘的线
                Gizmos.DrawLine(headPosition, coneEdgePoint);
            }

            // 绘制圆锥末端的圆形
            Gizmos.color = Color.yellow;
            for (int i = 0; i < segments; i++)
            {
                int nextIndex = (i + 1) % segments;
                Gizmos.DrawLine(conePoints[i], conePoints[nextIndex]);
            }

            // 绘制圆锥末端的中心点
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(coneEndCenter, 0.05f);
        }
    }

    void OnDestroy()
    {
        // 确保销毁时清理粒子特效
        if (currentEatingEffect != null)
        {
            Destroy(currentEatingEffect.gameObject);
        }
    }
}