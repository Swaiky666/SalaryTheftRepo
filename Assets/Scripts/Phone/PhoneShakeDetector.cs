using UnityEngine;

/// <summary>
/// 挂在手机物体上的脚本，用于检测手机的摇晃角度
/// 通过检测手机在世界空间中的旋转角度来判定左右倾斜
/// </summary>
public class PhoneShakeDetector : MonoBehaviour
{
    [Header("倾斜检测")]
    [SerializeField] private float deadZoneThreshold = 2f; // 死区阈值（度数），小于这个值不响应
    [SerializeField] private float maxRotationAngle = 45f; // 最大倾斜角度（度数）
    [SerializeField] private float filterStrength = 0.15f; // 低通滤波强度（0-1），越小越平滑但延迟越大

    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;

    private float smoothedRotationZ = 0f; // 平滑后的Z轴旋转
    private Quaternion initialRotation; // 初始旋转（用于相对倾斜计算）
    private bool isCalibrated = false;

    /// <summary>
    /// 获取经过死区处理和平滑的倾斜角度（-1到1）
    /// 负数表示向左倾斜，正数表示向右倾斜
    /// </summary>
    public float GetTiltInput()
    {
        if (!isCalibrated)
        {
            return 0f;
        }

        // 应用死区
        float deadzonedAngle = Mathf.Abs(smoothedRotationZ) > deadZoneThreshold ? smoothedRotationZ : 0f;

        // 归一化到-1到1的范围
        float normalizedInput = Mathf.Clamp(deadzonedAngle / maxRotationAngle, -1f, 1f);

        return normalizedInput;
    }

    /// <summary>
    /// 获取原始的倾斜角度（未经死区处理）
    /// </summary>
    public float GetRawTiltAngle()
    {
        return smoothedRotationZ;
    }

    /// <summary>
    /// 校准手机当前位置为0度参考点
    /// 在游戏开始或需要重新校准时调用
    /// </summary>
    public void Calibrate()
    {
        initialRotation = transform.rotation;
        smoothedRotationZ = 0f;
        isCalibrated = true;

        if (showDebugInfo)
        {
            Debug.Log("[PhoneShakeDetector] 已校准手机位置");
        }
    }

    private void Start()
    {
        // 游戏开始时自动校准一次
        Calibrate();
    }

    private void Update()
    {
        if (!isCalibrated)
            return;

        // 计算相对于初始旋转的增量旋转
        Quaternion deltaRotation = Quaternion.Inverse(initialRotation) * transform.rotation;
        
        // 从四元数提取欧拉角
        Vector3 deltaEuler = deltaRotation.eulerAngles;

        // 处理角度的周期性（0-360转换为-180到180）
        float rotationZ = deltaEuler.z;
        if (rotationZ > 180f)
        {
            rotationZ -= 360f;
        }

        // 应用低通滤波器来平滑抖动
        smoothedRotationZ = Mathf.Lerp(smoothedRotationZ, rotationZ, filterStrength);
    }

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Box("Phone Shake Detector");
            
            GUILayout.Label($"Raw Angle: {GetRawTiltAngle():F2}°");
            GUILayout.Label($"Tilt Input: {GetTiltInput():F3}");
            GUILayout.Label($"Calibrated: {isCalibrated}");

            if (GUILayout.Button("Recalibrate", GUILayout.Height(30)))
            {
                Calibrate();
            }

            GUILayout.EndArea();
        }
    }
}