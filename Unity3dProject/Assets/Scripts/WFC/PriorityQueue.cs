using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue<T> where T : IComparable<T>
{
    private List<T> _heap;

    public PriorityQueue()
    {
        _heap = new List<T>();
    }

    public int Count
    {
        get { return _heap.Count; }
    }

    public void Enqueue(T item, int priority)
    {
        // Add the item to the heap
        _heap.Add(item);

        // Reorder the heap
        int index = _heap.Count - 1;
        int parent = (index - 1) / 2;
        while (index > 0 && _heap[parent].CompareTo(item) > 0)
        {
            _heap[index] = _heap[parent];
            index = parent;
            parent = (index - 1) / 2;
        }
        _heap[index] = item;
    }

    public T Dequeue()
    {
        // Remove the root node
        T min = _heap[0];
        T last = _heap[_heap.Count - 1];
        _heap.RemoveAt(_heap.Count - 1);
        if (_heap.Count > 0)
        {
            // Reorder the heap
            int index = 0;
            int left = 1;
            int right = 2;
            while (right < _heap.Count)
            {
                if (_heap[left].CompareTo(_heap[right]) < 0)
                {
                    if (_heap[left].CompareTo(last) < 0)
                    {
                        _heap[index] = _heap[left];
                        index = left;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (_heap[right].CompareTo(last) < 0)
                    {
                        _heap[index] = _heap[right];
                        index = right;
                    }
                    else
                    {
                        break;
                    }
                }
                left = 2 * index + 1;
                right = 2 * index + 2;
            }
            if (left < _heap.Count && _heap[left].CompareTo(last) < 0)
            {
                _heap[index] = _heap[left];
                index = left;
            }
            _heap[index] = last;
        }
        return min;
    }
}


