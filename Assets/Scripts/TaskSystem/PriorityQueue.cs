using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Priority Queue Node
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

        if (Math.Abs(oldPriority - newPriority) < 0.0001f) return; // Ignore minimal change

        heap[index] = new PriorityQueueNode<T>(item, newPriority);

        if (newPriority < oldPriority)
        {
            HeapifyUp(index);
        }
        else
        {
            HeapifyDown(index);
        }
    }

    /// <summary>
    /// Remove an item from the queue (Time Complexity: O(log n))
    /// </summary>
    public bool Remove(T item)
    {
        if (!itemToIndexMap.ContainsKey(item))
        {
            return false;
        }

        int index = itemToIndexMap[item];
        RemoveAt(index);
        return true;
    }

    private void RemoveAt(int index)
    {
        int lastIndex = Count - 1;
        if (index == lastIndex)
        {
            // If it's the last element, just remove it
            itemToIndexMap.Remove(heap[index].Item);
            heap.RemoveAt(index);
            return;
        }

        // Swap the element to be removed with the last element
        Swap(index, lastIndex);

        // Remove the element from the end
        T itemToRemove = heap[lastIndex].Item;
        itemToIndexMap.Remove(itemToRemove);
        heap.RemoveAt(lastIndex);

        // Re-heapify the swapped element
        if (Count > 0)
        {
            HeapifyDown(index); // Try to move down first
            HeapifyUp(index);   // If not moved down, try to move up
        }
    }

    /// <summary>
    /// Check if an item exists (Time Complexity: O(1))
    /// </summary>
    public bool Contains(T item)
    {
        return itemToIndexMap.ContainsKey(item);
    }

    /// <summary>
    /// Clear the entire queue (Time Complexity: O(1))
    /// </summary>
    public void Clear()
    {
        heap.Clear();
        itemToIndexMap.Clear();
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

        // Deep copy items into a temporary queue
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