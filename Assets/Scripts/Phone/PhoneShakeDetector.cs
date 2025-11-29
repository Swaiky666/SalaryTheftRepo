using UnityEngine;

/// <summary>
/// 挂在“手机”物体上的脚本，用于检测相对于中立姿态，
/// 绕手机“屏幕法线”（本地 Z 轴）的旋转量，用来控制左右移动。
///
/// 思路：
/// 1. 校准时记下当前 rotation = neutralRotation；
/// 2. 每帧计算 delta = Inverse(neutralRotation) * currentRotation；
///    这个 delta 就是在“中立坐标系”里的相对旋转；
/// 3. 取 delta 的欧拉角；它的 z 分量就是绕本地 z 轴的旋转（roll）；
/// 4. roll 映射到 [-1, 1]，做死区 + 平滑，最后用在角色左右移动上。
/// </summary>
public class PhoneShakeDetector : MonoBehaviour
{
    [Header("绕本地 Z 轴（屏幕法线）的旋转映射")]
    [Tooltip("达到最大左右移动速度时，相对中立姿态的最大 roll 角度（度）")]
    [SerializeField] private float maxRollAngle = 45f;

    [Tooltip("死区阈值（小于这个角度视为 0 防止抖动）")]
    [SerializeField] private float deadZoneThreshold = 3f;

    [Tooltip("低通滤波强度：0~1，越大越跟手，越小越平滑")]
    [SerializeField] private float filterStrength = 0.15f;

    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;

    // 中立姿态（校准时的 rotation）
    private Quaternion neutralRotation;

    // 平滑后的输入（-1 ~ 1），用来控制左右移动
    private float smoothedTiltInput = 0f;

    // 当前相对中立姿态，绕本地 Z 轴的 roll 角度（-180 ~ 180 度）
    private float currentRollAngle = 0f;

    private void Start()
    {
        // 你也可以在外部手动调用 Calibrate()
        Calibrate();
    }

    /// <summary>
    /// 校准（初始化）——把当前姿态当成中立姿态。
    /// 在游戏开始 / 玩家按下“重置姿态”按钮时调用。
    /// </summary>
    public void Calibrate()
    {
        neutralRotation = transform.rotation;
        smoothedTiltInput = 0f;
        currentRollAngle = 0f;

        if (showDebugInfo)
        {
            Debug.Log("[PhoneShakeDetector] 已校准中立姿态");
        }
    }

    /// <summary>
    /// 获取归一化的左右输入（-1 到 1）
    /// -1：向左最快，0：不动，1：向右最快
    /// </summary>
    public float GetTiltInput()
    {
        return smoothedTiltInput;
    }

    /// <summary>
    /// 获取当前绕本地 Z 轴（屏幕法线）的 roll 角度（相对中立姿态，-180 ~ 180 度）
    /// 主要用于调试。
    /// </summary>
    public float GetRollAngle()
    {
        return currentRollAngle;
    }

    private void Update()
    {
        // 当前姿态
        Quaternion currentRotation = transform.rotation;

        // 关键一步：把当前姿态写成“中立 → 当前”的相对旋转
        // delta = neutral^(-1) * current
        // 这个 delta 就是在“中立坐标系”下的旋转
        Quaternion delta = Quaternion.Inverse(neutralRotation) * currentRotation;

        // 把 delta 转成欧拉角（单位：度）
        Vector3 deltaEuler = delta.eulerAngles;

        // Unity 的 eulerAngles 范围是 [0, 360)，我们要映射到 (-180, 180]
        float roll = deltaEuler.z;
        if (roll > 180f) roll -= 360f;

        currentRollAngle = roll;

        // 根据 roll 计算原始输入值（还没平滑）
        float rawTiltInput = 0f;

        // 死区处理：小角度抖动直接忽略
        if (Mathf.Abs(roll) > deadZoneThreshold)
        {
            // roll = maxRollAngle → 输入为 1（向右）
            // roll = -maxRollAngle → 输入为 -1（向左）
            rawTiltInput = -Mathf.Clamp(roll / maxRollAngle, -1f, 1f);
        }

        // 低通滤波（平滑输入，类似 Lab9 里对 a0/a1 的平滑效果）
        smoothedTiltInput = Mathf.Lerp(smoothedTiltInput, rawTiltInput, filterStrength);
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 380, 200));
        GUILayout.Box("=== 手机绕本地 Z 轴（屏幕法线）的旋转检测 ===");

        GUILayout.Label($"当前 roll（绕本地 Z）: {currentRollAngle:F2}°");
        GUILayout.Label($"归一化输入: {smoothedTiltInput:F3}");

        GUILayout.Space(5);
        GUILayout.Label("映射说明（相对于校准时的姿态）：");
        GUILayout.Label($"  roll =   0° → 不动 (0)");
        GUILayout.Label($"  roll = +{maxRollAngle}° → 向右最快 (1)");
        GUILayout.Label($"  roll = -{maxRollAngle}° → 向左最快 (-1)");
        GUILayout.Label($"  死区: ±{deadZoneThreshold}° 内视为 0");

        if (GUILayout.Button("重新校准", GUILayout.Height(30)))
        {
            Calibrate();
        }

        GUILayout.EndArea();
    }
}
