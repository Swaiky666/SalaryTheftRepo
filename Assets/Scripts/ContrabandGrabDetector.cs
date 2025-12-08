using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 挂载在 XR 控制器交互器 (e.g., XR Direct Interactor) 上。
/// 监听手柄抓取和释放事件，检查被抓取的物体是否带有 ContrabandItem 脚本。
/// </summary>
[RequireComponent(typeof(XRBaseInteractor))]
public class ContrabandGrabDetector : MonoBehaviour
{
    // 引用 CharacterStatus 脚本
    private CharacterStatus characterStatus;

    // 引用当前交互器
    private XRBaseInteractor interactor;

    void Start()
    {
        // 1. 找到场景中的 CharacterStatus
        // 假设 CharacterStatus 脚本挂载在一个容易找到的 GameObject 上（如玩家或 Game Manager）
        characterStatus = FindObjectOfType<CharacterStatus>();
        if (characterStatus == null)
        {
            Debug.LogError("[ContrabandGrabDetector] 错误：场景中未找到 CharacterStatus 组件！无法更新摸鱼状态。");
            enabled = false;
            return;
        }
        else
        {
            Debug.Log($"[ContrabandGrabDetector] 成功找到 CharacterStatus 组件在对象: {characterStatus.gameObject.name}");
        }

        // 2. 获取 Interactor 并订阅事件
        interactor = GetComponent<XRBaseInteractor>();
        if (interactor != null)
        {
            Debug.Log($"[ContrabandGrabDetector] 成功找到 XRBaseInteractor ({interactor.GetType().Name})，正在订阅事件...");
            // 订阅抓取开始事件
            interactor.selectEntered.AddListener(OnGrabbed);
            // 订阅抓取释放事件
            interactor.selectExited.AddListener(OnReleased);
        }
        else
        {
            Debug.LogError("[ContrabandGrabDetector] 错误：缺少 XRBaseInteractor 组件！请确保挂载在交互器上。");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        // 清理事件订阅
        if (interactor != null)
        {
            interactor.selectEntered.RemoveListener(OnGrabbed);
            interactor.selectExited.RemoveListener(OnReleased);
        }
    }

    /// <summary>
    /// 当玩家抓取道具时调用
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        string grabbedObjectName = args.interactableObject.transform.name;
        Debug.Log($"[ContrabandGrabDetector] 🛑 Grab Event Received! 抓取的对象: {grabbedObjectName}");

        // 检查被抓取的物体 (args.interactableObject) 上是否有 ContrabandItem 组件
        // 注意：这里使用 GetComponent<T>()，它只检查 interactableObject 所在的 GameObject。
        ContrabandItem contraband = args.interactableObject.transform.GetComponent<ContrabandItem>();

        if (contraband != null)
        {
            // 如果找到了 ContrabandItem，说明这是违禁品
            if (characterStatus != null)
            {
                characterStatus.isSlackingAtWork = true;
                Debug.Log($"[ContrabandGrabDetector] ✅ 识别为违禁品 ({grabbedObjectName})！摸鱼状态 (isSlackingAtWork) 已设为: True");
            }
            else
            {
                Debug.LogError("[ContrabandGrabDetector] ❌ 找到违禁品，但 characterStatus 引用丢失，无法更新状态！");
            }
        }
        else
        {
            Debug.Log("[ContrabandGrabDetector] ❓ 抓取的不是违禁品。未找到 ContrabandItem 脚本。");
        }
    }

    /// <summary>
    /// 当玩家松开道具时调用
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        string releasedObjectName = args.interactableObject.transform.name;
        Debug.Log($"[ContrabandGrabDetector] 🗑️ Release Event Received! 释放的对象: {releasedObjectName}");

        // 检查被释放的物体 (args.interactableObject) 上是否有 ContrabandItem 组件
        ContrabandItem contraband = args.interactableObject.transform.GetComponent<ContrabandItem>();

        if (contraband != null)
        {
            // 只有当释放的是违禁品时，才将摸鱼状态设为 false
            if (characterStatus != null)
            {
                characterStatus.isSlackingAtWork = false;
                Debug.Log($"[ContrabandGrabDetector] ✅ 释放了违禁品 ({releasedObjectName})。摸鱼状态 (isSlackingAtWork) 已设为: False");
            }
            else
            {
                Debug.LogError("[ContrabandGrabDetector] ❌ 释放违禁品，但 characterStatus 引用丢失，无法更新状态！");
            }
        }
    }
}