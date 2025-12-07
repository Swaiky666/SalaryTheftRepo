using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 优先级队列节点
/// </summary>
public struct PriorityQueueNode<T>
{
    public T Item { get; }
    public float Priority { get; set; }

    public PriorityQueueNode(T item, float priority)
    {
        Item = item;
        Priority = priority;
    }
}

/// <summary>
/// CS400 Application: Min-Heap based Priority Queue
/// </summary>
public class PriorityQueue<T>
{
    private List<PriorityQueueNode<T>> heap;
    private Dictionary<T, int> itemToIndexMap;

    public int Count => heap.Count;

    public PriorityQueue()
    {
        heap = new List<PriorityQueueNode<T>>();
        itemToIndexMap = new Dictionary<T, int>();
    }

    /// <summary>
    /// Enqueue with priority (Time Complexity: O(log n))
    /// </summary>
    public void Enqueue(T item, float priority)
    {
        if (itemToIndexMap.ContainsKey(item))
        {
            UpdatePriority(item, priority);
            return;
        }

        var node = new PriorityQueueNode<T>(item, priority);
        heap.Add(node);
        int index = heap.Count - 1;
        itemToIndexMap.Add(item, index);

        HeapifyUp(index);
    }

    /// <summary>
    /// Dequeue the item with the highest priority (Time Complexity: O(log n))
    /// </summary>
    public T Dequeue()
    {
        if (Count == 0)
            throw new InvalidOperationException("PriorityQueue is empty.");

        var rootItem = heap[0].Item;
        RemoveAt(0);
        return rootItem;
    }

    /// <summary>
    /// Peek the item with the highest priority (Time Complexity: O(1))
    /// </summary>
    public T Peek()
    {
        if (Count == 0)
            throw new InvalidOperationException("PriorityQueue is empty.");
        return heap[0].Item;
    }

    /// <summary>
    /// Update the priority of an existing item (Time Complexity: O(log n))
    /// </summary>
    public void UpdatePriority(T item, float newPriority)
    {
        if (!itemToIndexMap.ContainsKey(item))
        {
            Debug.LogWarning("Item not found for priority update. Enqueueing instead.");
            Enqueue(item, newPriority);
            return;
        }

        int index = itemToIndexMap[item];
        float oldPriority = heap[index].Priority;

        heap[index] = new PriorityQueueNode<T>(item, newPriority);

        if (newPriority < oldPriority)
        {
            HeapifyUp(index);
        }
        else if (newPriority > oldPriority)
        {
            HeapifyDown(index);
        }
    }

    /// <summary>
    /// Remove a specific item from the queue (Time Complexity: O(log n))
    /// </summary>
    public void Remove(T item)
    {
        if (!itemToIndexMap.ContainsKey(item)) return;

        int index = itemToIndexMap[item];
        RemoveAt(index);
    }

    /// <summary>
    /// Check if item is in the queue (Time Complexity: O(1))
    /// </summary>
    public bool Contains(T item) => itemToIndexMap.ContainsKey(item);

    public void Clear()
    {
        heap.Clear();
        itemToIndexMap.Clear();
    }

    private void RemoveAt(int index)
    {
        itemToIndexMap.Remove(heap[index].Item);

        int lastIndex = heap.Count - 1;
        if (index != lastIndex)
        {
            Swap(index, lastIndex);
            heap.RemoveAt(lastIndex);

            HeapifyDown(index);
            HeapifyUp(index); // Safety check
        }
        else
        {
            heap.RemoveAt(lastIndex);
        }
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (heap[index].Priority < heap[parentIndex].Priority)
            {
                Swap(index, parentIndex);
                index = parentIndex;
            }
            else
            {
                break;
            }
        }
    }

    private void HeapifyDown(int index)
    {
        int count = heap.Count;
        while (true)
        {
            int leftChildIndex = 2 * index + 1;
            int rightChildIndex = 2 * index + 2;
            int smallest = index;

            if (leftChildIndex < count && heap[leftChildIndex].Priority < heap[smallest].Priority)
                smallest = leftChildIndex;

            if (rightChildIndex < count && heap[rightChildIndex].Priority < heap[smallest].Priority)
                smallest = rightChildIndex;

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int i, int j)
    {
        var temp = heap[i];
        heap[i] = heap[j];
        heap[j] = temp;

        itemToIndexMap[heap[i].Item] = i;
        itemToIndexMap[heap[j].Item] = j;
    }

    /// <summary>
    /// Get all items in priority order (Time Complexity: O(n log n))
    /// </summary>
    public List<T> GetAllInPriorityOrder()
    {
        var result = new List<T>();
        var tempQueue = new PriorityQueue<T>();

        foreach (var node in heap)
        {
            tempQueue.Enqueue(node.Item, node.Priority);
        }

        while (tempQueue.Count > 0)
        {
            result.Add(tempQueue.Dequeue());
        }

        return result;
    }
}