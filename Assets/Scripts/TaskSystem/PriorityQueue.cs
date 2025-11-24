using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CS400 Application: Min-Heap based Priority Queue
/// Time Complexity:
/// - Enqueue: O(log n) - heapify up
/// - Dequeue: O(log n) - heapify down
/// - Peek: O(1) - top element access
/// Space Complexity: O(n) - array storage
/// 
/// This implementation demonstrates understanding of heap data structures
/// from CS400 Binary Search Trees unit (Week 1-2)
/// </summary>
public class PriorityQueue<T>
{
    private List<PriorityQueueNode<T>> heap;
    private Dictionary<T, int> itemToIndexMap; // O(1) lookup for contains/update operations
    
    public int Count => heap.Count;
    
    public PriorityQueue()
    {
        heap = new List<PriorityQueueNode<T>>();
        itemToIndexMap = new Dictionary<T, int>();
    }
    
    /// <summary>
    /// Enqueue with priority
    /// Time Complexity: O(log n)
    /// </summary>
    public void Enqueue(T item, float priority)
    {
        if (itemToIndexMap.ContainsKey(item))
        {
            // Update priority if item already exists
            UpdatePriority(item, priority);
            return;
        }
        
        var node = new PriorityQueueNode<T>(item, priority);
        heap.Add(node);
        int index = heap.Count - 1;
        itemToIndexMap[item] = index;
        
        HeapifyUp(index);
    }
    
    /// <summary>
    /// Dequeue highest priority item (lowest priority value)
    /// Time Complexity: O(log n)
    /// </summary>
    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Priority queue is empty");
        
        T result = heap[0].Item;
        itemToIndexMap.Remove(result);
        
        // Move last element to root
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);
        
        if (heap.Count > 0)
        {
            itemToIndexMap[heap[0].Item] = 0;
            HeapifyDown(0);
        }
        
        return result;
    }
    
    /// <summary>
    /// Peek at highest priority item without removing
    /// Time Complexity: O(1)
    /// </summary>
    public T Peek()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Priority queue is empty");
        
        return heap[0].Item;
    }
    
    /// <summary>
    /// Check if item exists in queue
    /// Time Complexity: O(1) - using hashtable lookup
    /// </summary>
    public bool Contains(T item)
    {
        return itemToIndexMap.ContainsKey(item);
    }
    
    /// <summary>
    /// Update priority of existing item
    /// Time Complexity: O(log n)
    /// </summary>
    public void UpdatePriority(T item, float newPriority)
    {
        if (!itemToIndexMap.ContainsKey(item))
            return;
        
        int index = itemToIndexMap[item];
        float oldPriority = heap[index].Priority;
        heap[index].Priority = newPriority;
        
        // Heapify in appropriate direction
        if (newPriority < oldPriority)
            HeapifyUp(index);
        else if (newPriority > oldPriority)
            HeapifyDown(index);
    }
    
    /// <summary>
    /// Remove specific item from queue
    /// Time Complexity: O(log n)
    /// </summary>
    public bool Remove(T item)
    {
        if (!itemToIndexMap.ContainsKey(item))
            return false;
        
        int index = itemToIndexMap[item];
        itemToIndexMap.Remove(item);
        
        int lastIndex = heap.Count - 1;
        if (index == lastIndex)
        {
            heap.RemoveAt(lastIndex);
            return true;
        }
        
        // Replace with last element
        heap[index] = heap[lastIndex];
        heap.RemoveAt(lastIndex);
        itemToIndexMap[heap[index].Item] = index;
        
        // Heapify in appropriate direction
        HeapifyUp(index);
        HeapifyDown(index);
        
        return true;
    }
    
    public void Clear()
    {
        heap.Clear();
        itemToIndexMap.Clear();
    }
    
    /// <summary>
    /// Restore heap property upward
    /// Time Complexity: O(log n) - height of tree
    /// </summary>
    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            
            // Min-heap: parent should be smaller
            if (heap[index].Priority >= heap[parentIndex].Priority)
                break;
            
            Swap(index, parentIndex);
            index = parentIndex;
        }
    }
    
    /// <summary>
    /// Restore heap property downward
    /// Time Complexity: O(log n) - height of tree
    /// </summary>
    private void HeapifyDown(int index)
    {
        while (true)
        {
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            int smallest = index;
            
            // Find smallest among node and its children
            if (leftChild < heap.Count && heap[leftChild].Priority < heap[smallest].Priority)
                smallest = leftChild;
            
            if (rightChild < heap.Count && heap[rightChild].Priority < heap[smallest].Priority)
                smallest = rightChild;
            
            // If current node is smallest, heap property is satisfied
            if (smallest == index)
                break;
            
            Swap(index, smallest);
            index = smallest;
        }
    }
    
    /// <summary>
    /// Swap two nodes and update hashtable indices
    /// Time Complexity: O(1)
    /// </summary>
    private void Swap(int i, int j)
    {
        var temp = heap[i];
        heap[i] = heap[j];
        heap[j] = temp;
        
        // Update hashtable
        itemToIndexMap[heap[i].Item] = i;
        itemToIndexMap[heap[j].Item] = j;
    }
    
    /// <summary>
    /// Get all items in priority order (for debugging)
    /// Time Complexity: O(n log n)
    /// </summary>
    public List<T> GetAllInPriorityOrder()
    {
        var result = new List<T>();
        var tempQueue = new PriorityQueue<T>();
        
        // Copy all items to temp queue
        foreach (var node in heap)
        {
            tempQueue.Enqueue(node.Item, node.Priority);
        }
        
        // Dequeue in order
        while (tempQueue.Count > 0)
        {
            result.Add(tempQueue.Dequeue());
        }
        
        return result;
    }
}

/// <summary>
/// Node class for priority queue
/// </summary>
public class PriorityQueueNode<T>
{
    public T Item { get; set; }
    public float Priority { get; set; }
    
    public PriorityQueueNode(T item, float priority)
    {
        Item = item;
        Priority = priority;
    }
}