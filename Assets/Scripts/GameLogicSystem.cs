using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameLogicSystem : MonoBehaviour
{
    // --- PlayerPrefs Keys for Persistence ---
    private const string JobLevelKey = "JobLevel";
    private const string StressRateKey = "StressRate";

    [Header("工作时间设置")]
    [SerializeField] private float workTimeMinutes = 8f; // 工作时间（分钟）
    [SerializeField] private Slider workTimeSlider; // 工作时间显示的滑块

    [Header("工作进度")]
    [SerializeField] private float workProgress = 0f; // 工作进度 0-100
    [SerializeField] private Slider workProgressSlider; // 工作进度显示的滑块

    [Header("薪资设置")]
    [SerializeField] private int baseSalary = 800; // 基础薪资
    [SerializeField] private int currentSalaryDeduction = 0; // 当前扣除的薪资
    [SerializeField] private TextMeshProUGUI salaryText; // 薪资显示文本
    [SerializeField] private TextMeshProUGUI deductionText; // 扣除薪资显示文本（可选）

    [Header("职位设置")]
    [SerializeField] private TextMeshProUGUI positionText; // 职位显示文本

    [Header("压力指数设置")]
    // 此值在InitializeSystem中从PlayerPrefs加载，并在成功时更新
    [SerializeField] private float stressIncreaseRate = 1f; // 压力增长速度倍率
    [SerializeField] private float stressLevel = 0f; // 当前压力值 0-100
    [SerializeField] private Slider stressSlider; // 压力指数显示的滑块
    [SerializeField] private Image stressSliderFill; // 压力滑块的填充图像（用于改变颜色）
    [SerializeField] private TextMeshProUGUI stressText; // 压力指数显示文本

    // 私有变量
    private float workTimeRemaining; // 剩余工作时间（秒）
    private float maxWorkTime; // 最大工作时间（秒）
    private int currentSalary; // 当前薪资
    private JobLevel currentJobLevel = JobLevel.Junior; // 当前职位等级
    private float baseStressGrowthRate; // 基础压力增长速度（每秒）
    private bool isGameOver = false; // 游戏是否结束的标记

    // 职位等级枚举
    public enum JobLevel
    {
        Junior = 0,    // 初级员工
        Intermediate = 1, // 中级员工
        Senior = 2     // 高级员工
    }

    // 事件委托
    public static event Action<float> OnWorkTimeChanged; // 工作时间变化事件
    public static event Action<float> OnWorkProgressChanged; // 工作进度变化事件
    public static event Action<int> OnSalaryChanged; // 薪资变化事件
    public static event Action<int> OnSalaryDeducted; // 薪资扣除事件
    public static event Action<JobLevel> OnJobLevelChanged; // 职位变化事件
    public static event Action<float> OnStressChanged; // 压力变化事件
    public static event Action<float> OnStressPenalty; // 压力惩罚事件（外部增加压力时触发）
    public static event Action OnGameOver; // 游戏失败结束事件（例如压力爆表或时间到未完成）
    public static event Action OnWorkSuccess; // 工作成功完成事件

    void Start()
    {
        InitializeSystem();
    }

    void Update()
    {
        if (isGameOver) return;

        UpdateWorkTime();
        UpdateStress();
        CheckGameOver(); // 检查游戏结束条件
    }

    /// <summary>
    /// **静态方法：重置游戏进度到初始状态 (最低职位, 初始难度)**
    /// 在新游戏开始时调用，清除PlayerPrefs中的持久化数据。
    /// </summary>
    public static void ResetGameProgress()
    {
        // 1. 重置职位等级为 Junior (0)
        PlayerPrefs.SetInt(JobLevelKey, (int)JobLevel.Junior);

        // 2. 重置压力倍率为 1.0f (初始值)
        PlayerPrefs.SetFloat(StressRateKey, 1f);

        // 3. 确保保存更改
        PlayerPrefs.Save();

        Debug.Log("[GameLogicSystem] 游戏进度已初始化/重置。职位: Junior, 压力倍率: 1.0");
    }

    /// <summary>
    /// 初始化系统，加载持久化数据
    /// </summary>
    private void InitializeSystem()
    {
        // 加载持久化数据或使用默认值
        // 从PlayerPrefs加载职位等级，如果不存在则默认为 Junior(0)
        currentJobLevel = (JobLevel)PlayerPrefs.GetInt(JobLevelKey, (int)JobLevel.Junior);
        // 从PlayerPrefs加载压力倍率，如果不存在则默认为 1.0f
        stressIncreaseRate = PlayerPrefs.GetFloat(StressRateKey, 1f);

        // 初始化工作时间
        maxWorkTime = workTimeMinutes * 60f; // 转换为秒
        workTimeRemaining = maxWorkTime;

        // 计算基础压力增长速度（1分钟内从0%增长到100%），使用加载的倍率
        baseStressGrowthRate = (100f / 60f) * stressIncreaseRate;

        // 初始化薪资和职位
        UpdateSalary();
        UpdatePositionText();
        ResetStress(); // 确保新的一天压力从0开始

        // 更新UI
        UpdateUI();

        Debug.Log($"[GameLogicSystem] 系统初始化: 职位: {currentJobLevel}, 压力倍率: {stressIncreaseRate:F2}");
    }

    /// <summary>
    /// 更新工作时间
    /// </summary>
    private void UpdateWorkTime()
    {
        if (workTimeRemaining > 0)
        {
            workTimeRemaining -= Time.deltaTime;
            workTimeRemaining = Mathf.Max(0, workTimeRemaining);

            // 更新滑块
            if (workTimeSlider != null)
            {
                workTimeSlider.value = workTimeRemaining / maxWorkTime;
            }

            OnWorkTimeChanged?.Invoke(workTimeRemaining);
        }
        else if (workTimeRemaining <= 0)
        {
            workTimeRemaining = 0;
        }
    }

    /// <summary>
    /// 更新压力值
    /// </summary>
    private void UpdateStress()
    {
        if (stressLevel < 100f)
        {
            stressLevel += baseStressGrowthRate * Time.deltaTime;
            stressLevel = Mathf.Min(100f, stressLevel);
            UpdateStressUI();
            OnStressChanged?.Invoke(stressLevel);
        }
        else
        {
            stressLevel = 100f;
            UpdateStressUI();
        }
    }

    /// <summary>
    /// 检查游戏结束条件 (成功或失败)
    /// </summary>
    private void CheckGameOver()
    {
        bool successConditionMet = false;
        bool failConditionMet = false;

        // 成功条件: 工作时间结束 AND 工作进度完成
        if (workTimeRemaining <= 0f && workProgress >= 100f)
        {
            Debug.Log("[GameLogicSystem] 工作成功完成！准备升级并开始下一轮。");
            successConditionMet = true;
        }
        // 失败条件1: 压力值达到100%
        else if (stressLevel >= 100f)
        {
            Debug.Log("[GameLogicSystem] 游戏失败: 压力值过高！");
            failConditionMet = true;
        }
        // 失败条件2: 工作时间结束 AND 工作进度未满
        else if (workTimeRemaining <= 0f && workProgress < 100f)
        {
            Debug.Log("[GameLogicSystem] 游戏失败: 工作未完成且时间用尽！");
            failConditionMet = true;
        }

        if (successConditionMet && !isGameOver)
        {
            isGameOver = true;
            OnWorkSuccess?.Invoke();
            HandleWorkSuccess();
        }
        else if (failConditionMet && !isGameOver)
        {
            isGameOver = true;
            OnGameOver?.Invoke(); // 触发游戏失败事件（闪烁/场景切换到StartMenu）
        }
    }

    /// <summary>
    /// 处理工作成功完成的逻辑：升职、增加难度、重置场景
    /// </summary>
    private void HandleWorkSuccess()
    {
        // 1. 升职 (如果未达到最高级)
        PromoteJobLevel();

        // 2. 增加难度 (压力增长率 * 1.2)
        const float difficultyMultiplier = 1.2f;
        stressIncreaseRate *= difficultyMultiplier;

        // 重新计算基础增长率以应用新的难度
        baseStressGrowthRate = (100f / 60f) * stressIncreaseRate;

        // 3. 保存新的难度和职位等级
        PlayerPrefs.SetFloat(StressRateKey, stressIncreaseRate);
        PlayerPrefs.SetInt(JobLevelKey, (int)currentJobLevel);
        PlayerPrefs.Save();

        Debug.Log($"[GameLogicSystem] 难度升级！新压力倍率: {stressIncreaseRate:F2}。当前职位: {currentJobLevel}");

        // 4. 重新加载场景，开始新的一天
        SceneController sceneController = FindObjectOfType<SceneController>();
        if (sceneController != null)
        {
            sceneController.LoadScene("InGame");
        }
        else
        {
            Debug.LogError("[GameLogicSystem] 未找到 SceneController 无法加载 InGame 场景！");
        }
    }

    /// <summary>
    /// 提升职位等级（在工作日成功结束后调用）
    /// </summary>
    private void PromoteJobLevel()
    {
        JobLevel nextLevel = currentJobLevel;

        if (currentJobLevel == JobLevel.Junior)
        {
            nextLevel = JobLevel.Intermediate;
        }
        else if (currentJobLevel == JobLevel.Intermediate)
        {
            nextLevel = JobLevel.Senior;
        }
        // 如果已经是Senior，则保持不变

        if (nextLevel != currentJobLevel)
        {
            SetJobLevel(nextLevel);
        }
    }

    /// <summary>
    /// 增加工作进度
    /// </summary>
    /// <param name="amount">增加的数量</param>
    public void AddWorkProgress(float amount)
    {
        workProgress += amount;
        workProgress = Mathf.Clamp(workProgress, 0f, 100f);
        UpdateWorkProgressUI();
        OnWorkProgressChanged?.Invoke(workProgress);
    }

    /// <summary>
    /// 减少工作进度
    /// </summary>
    /// <param name="amount">减少的数量</param>
    public void ReduceWorkProgress(float amount)
    {
        workProgress -= amount;
        workProgress = Mathf.Clamp(workProgress, 0f, 100f);
        UpdateWorkProgressUI();
        OnWorkProgressChanged?.Invoke(workProgress);
    }

    /// <summary>
    /// 增加压力
    /// </summary>
    /// <param name="amount">增加的压力值</param>
    /// <param name="isExternalCall">是否为外部调用（非自然增长）</param>
    public void AddStress(float amount, bool isExternalCall = false)
    {
        stressLevel += amount;
        stressLevel = Mathf.Clamp(stressLevel, 0f, 100f);
        UpdateStressUI();
        OnStressChanged?.Invoke(stressLevel);

        // 如果是外部调用（如惩罚），触发额外的闪烁效果
        if (isExternalCall)
        {
            OnStressPenalty?.Invoke(amount);
        }
    }

    /// <summary>
    /// 减少压力
    /// </summary>
    /// <param name="amount">减少的压力值</param>
    public void ReduceStress(float amount)
    {
        stressLevel -= amount;
        stressLevel = Mathf.Clamp(stressLevel, 0f, 100f);
        UpdateStressUI();
        OnStressChanged?.Invoke(stressLevel);
    }

    /// <summary>
    /// 扣除薪资（外部调用）
    /// </summary>
    /// <param name="amount">扣除的金额</param>
    /// <returns>是否成功扣除</returns>
    public bool DeductSalary(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[GameLogicSystem] 扣除金额必须大于0");
            return false;
        }

        // 计算当前实际薪资
        int actualSalary = GetActualSalary();

        // 检查是否有足够的薪资可以扣除
        if (actualSalary <= 0)
        {
            Debug.LogWarning("[GameLogicSystem] 当前薪资已为0，无法继续扣除");
            return false;
        }

        // 执行扣除
        currentSalaryDeduction += amount;

        // 确保扣除后的薪资不会低于0
        if (GetActualSalary() < 0)
        {
            currentSalaryDeduction = currentSalary;
        }

        Debug.Log($"[GameLogicSystem] 扣除薪资 ${amount}，当前总扣除: ${currentSalaryDeduction}，剩余薪资: ${GetActualSalary()}");

        // 更新UI
        UpdateSalary();

        // 触发事件
        OnSalaryDeducted?.Invoke(amount);

        return true;
    }

    /// <summary>
    /// 增加薪资（外部调用，比如奖励）
    /// </summary>
    /// <param name="amount">增加的金额</param>
    public void AddSalary(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[GameLogicSystem] 增加金额必须大于0");
            return;
        }

        // 减少扣除的薪资，而不是直接增加基础薪资
        currentSalaryDeduction = Mathf.Max(0, currentSalaryDeduction - amount);

        Debug.Log($"[GameLogicSystem] 增加薪资 ${amount}，当前总扣除: ${currentSalaryDeduction}，当前薪资: ${GetActualSalary()}");

        // 更新UI
        UpdateSalary();

        // 触发事件
        OnSalaryChanged?.Invoke(GetActualSalary());
    }

    /// <summary>
    /// 获取当前实际薪资（基础薪资 - 扣除金额）
    /// </summary>
    /// <returns>实际薪资</returns>
    public int GetActualSalary()
    {
        return Mathf.Max(0, currentSalary - currentSalaryDeduction);
    }

    /// <summary>
    /// 重置薪资扣除（用于新的工作周期）
    /// </summary>
    public void ResetSalaryDeduction()
    {
        currentSalaryDeduction = 0;
        UpdateSalary();
        Debug.Log("[GameLogicSystem] 薪资扣除已重置");
    }

    /// <summary>
    /// 设置职位等级
    /// </summary>
    /// <param name="newLevel">新的职位等级</param>
    public void SetJobLevel(JobLevel newLevel)
    {
        currentJobLevel = newLevel;
        UpdateSalary();
        UpdatePositionText();
        OnJobLevelChanged?.Invoke(currentJobLevel);
    }

    /// <summary>
    /// 更新薪资
    /// </summary>
    private void UpdateSalary()
    {
        currentSalary = baseSalary * (int)Mathf.Pow(2, (int)currentJobLevel);
        int actualSalary = GetActualSalary();

        if (salaryText != null)
        {
            salaryText.text = $"${actualSalary:N0}";
        }

        // 如果有扣除薪资显示文本，更新它
        if (deductionText != null)
        {
            deductionText.text = $"-${currentSalaryDeduction:N0}";
        }

        OnSalaryChanged?.Invoke(actualSalary);
    }

    /// <summary>
    /// 更新职位文本
    /// </summary>
    private void UpdatePositionText()
    {
        string positionName = "";
        switch (currentJobLevel)
        {
            case JobLevel.Junior:
                positionName = "Junior Employee";
                break;
            case JobLevel.Intermediate:
                positionName = "Intermediate Employee";
                break;
            case JobLevel.Senior:
                positionName = "Senior Employee";
                break;
        }

        if (positionText != null)
        {
            positionText.text = positionName;
        }
    }

    /// <summary>
    /// 更新工作进度UI
    /// </summary>
    private void UpdateWorkProgressUI()
    {
        if (workProgressSlider != null)
        {
            workProgressSlider.value = workProgress / 100f;
        }
    }

    /// <summary>
    /// 更新压力UI（包括颜色变化）
    /// </summary>
    private void UpdateStressUI()
    {
        if (stressSlider != null)
        {
            stressSlider.value = stressLevel / 100f;
        }

        // 获取压力对应的颜色
        Color stressColor = GetStressColor(stressLevel / 100f);

        // 更新滑块填充颜色
        if (stressSliderFill != null)
        {
            stressSliderFill.color = stressColor;
        }

        // 更新压力文本颜色（不改变文本内容）
        if (stressText != null)
        {
            stressText.color = stressColor;
        }
    }

    /// <summary>
    /// 根据压力百分比获取对应颜色
    /// </summary>
    /// <param name="stressPercent">压力百分比 (0-1)</param>
    /// <returns>对应的颜色</returns>
    private Color GetStressColor(float stressPercent)
    {
        Color color;

        if (stressPercent <= 0.5f)
        {
            // 0-50%: 绿色到黄色
            float t = stressPercent * 2f; // 将0-0.5映射到0-1
            color = Color.Lerp(Color.green, Color.yellow, t);
        }
        else
        {
            // 50-100%: 黄色到红色
            float t = (stressPercent - 0.5f) * 2f; // 将0.5-1映射到0-1
            color = Color.Lerp(Color.yellow, Color.red, t);
        }

        return color;
    }

    /// <summary>
    /// 更新所有UI
    /// </summary>
    private void UpdateUI()
    {
        // 更新工作时间滑块
        if (workTimeSlider != null)
        {
            workTimeSlider.value = workTimeRemaining / maxWorkTime;
        }

        // 更新工作进度滑块
        UpdateWorkProgressUI();

        // 更新压力滑块和颜色
        UpdateStressUI();

        // 更新薪资和职位文本
        UpdateSalary();
        UpdatePositionText();
    }

    /// <summary>
    /// 重置工作时间
    /// </summary>
    public void ResetWorkTime()
    {
        workTimeRemaining = maxWorkTime;
        UpdateUI();
    }

    /// <summary>
    /// 重置工作进度
    /// </summary>
    public void ResetWorkProgress()
    {
        workProgress = 0f;
        UpdateWorkProgressUI();
        OnWorkProgressChanged?.Invoke(workProgress);
    }

    /// <summary>
    /// 重置压力值
    /// </summary>
    public void ResetStress()
    {
        stressLevel = 0f;
        UpdateStressUI();
        OnStressChanged?.Invoke(stressLevel);
    }

    // 属性访问器
    public float WorkProgress => workProgress;
    public float WorkTimeRemaining => workTimeRemaining;
    public float WorkTimePercentage => workTimeRemaining / maxWorkTime;
    public int CurrentSalary => currentSalary; // 基础薪资
    public int ActualSalary => GetActualSalary(); // 实际薪资
    public int TotalDeduction => currentSalaryDeduction; // 总扣除金额
    public JobLevel CurrentJobLevel => currentJobLevel;
    public float StressLevel => stressLevel;
    public float StressPercentage => stressLevel / 100f;

    // 调试信息（仅在编辑器中显示）
#if UNITY_EDITOR
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 300));
        GUILayout.Label($"工作时间剩余: {workTimeRemaining:F1}秒");
        GUILayout.Label($"工作进度: {workProgress:F1}%");
        GUILayout.Label($"基础薪资: ${currentSalary:N0}");
        GUILayout.Label($"扣除金额: ${currentSalaryDeduction:N0}");
        GUILayout.Label($"实际薪资: ${GetActualSalary():N0}");
        GUILayout.Label($"职位等级: {currentJobLevel}");
        GUILayout.Label($"压力指数: {stressLevel:F1}%");
        GUILayout.Label($"**压力倍率**: {stressIncreaseRate:F2} (难度)");
        GUILayout.Label($"游戏状态: {(isGameOver ? "已结束" : "进行中")}");
        GUILayout.EndArea();
    }
#endif
}