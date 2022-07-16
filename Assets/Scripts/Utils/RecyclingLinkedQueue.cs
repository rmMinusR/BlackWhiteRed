using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Read/write like a queue.
/// Performance of a linked list.
/// GC-friendliness of a circular buffer.
/// </summary>
/// <typeparam name="T">Type stored</typeparam>
[Serializable]
public class RecyclingLinkedQueue<T> : IEnumerable<T>, ISerializationCallbackReceiver
{
    [NonSerialized] private Node head;
    [NonSerialized] private Node tail;
    [NonSerialized] private int count;
    public Node Head { get => head; private set => head = value; }
    public Node Tail { get => tail; private set => tail = value; }
    public int Count { get => count; private set => count = value; }

    [Serializable]
    public class Node
    {
        public T value;

        [NonSerialized] private Node __next;
        public Node next { get => __next; internal set => __next = value; }

        private static List<Node> recycled = new List<Node>();
        internal static Node New(T val)
        {
            //Get or instantiate
            Node n;
            if (recycled.Count > 0)
            {
                n = recycled[recycled.Count - 1];
                recycled.RemoveAt(recycled.Count - 1);
            }
            else
            {
                n = new Node();
            }
            n.value = val;
            n.next = null;
            return n;
        }

        internal static void Recycle(Node n)
        {
            recycled.Add(n);
        }
    }

    public void Enqueue(T val)
    {
        Node newTail = Node.New(val);
        if (tail != null)
            tail.next = newTail;
        if (count == 0)
        { head = newTail; tail = newTail; }
        tail = newTail;
        ++count;
    }

    public void Insert(int index, T val)
    {
        if (index < 0 || index > count)
            throw new IndexOutOfRangeException();

        Node @new = Node.New(val);
        if (count != 0)
        {
            Node before = GetNodeAt(index - 1);
            Node after = before.next;
            if (before != null)
                before.next = @new;
            @new.next = after;
            ++count;
        }
        else
        {
            head = @new;
            tail = @new;
            ++count;
        }
    }

    //WARNING: Not necessarily safe! Doesn't verify that node belongs to this data structure.
    public void Insert(Node before, T val)
    {
        Node @new = Node.New(val);

        Node after = before.next;
        if (before != null)
            before.next = @new;
        @new.next = after;
        ++count;
    }

    public T Peek() => head.value;

    public T Dequeue()
    {
        T val = head.value;
        DropHead();
        return val;
    }

    public void DropHead()
    {
        //Advance head forward, recycling
        Node next = head.next;
        Node.Recycle(head);
        head = next;

        --count;
    }

    public void Clear()
    {
        for (Node i = head; i != null; i = i.next)
            Node.Recycle(i);
        head = null;
        tail = null;
        count = 0;
    }

    private Node GetNodeAt(int index)
    {
        if (index < 0 || index >= count)
            return null;
        Node ptr = head;
        for (int i = 0; i < index; ++i)
            ptr = ptr.next;
        return ptr;
    }

    public Node FindNode(Func<T, bool> selector)
    {
        for (Node i = head; i != null; i = i.next)
            if (selector(i.value))
                return i;
        return null;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (Node i = head; i != null; i = i.next)
            yield return i.value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #region Serialization

    [SerializeField] private List<T> _serializedRepr = new List<T>();

    public void OnBeforeSerialize()
    {
        //Ensure same size
        while (_serializedRepr.Count > count)
            _serializedRepr.RemoveAt(_serializedRepr.Count - 1);
        while (_serializedRepr.Count < count)
            _serializedRepr.Add(default);

        //Write values
        Node n = head;
        for (int i = 0; i < count; ++i)
        {
            _serializedRepr[i] = n.value;
            n = n.next;
        }
    }

    public void OnAfterDeserialize()
    {
        //Ensure same size
        while (count > _serializedRepr.Count)
            DropHead();
        while (count < _serializedRepr.Count)
            Enqueue(default);

        //Write values
        Node n = head;
        for (int i = 0; i < _serializedRepr.Count; ++i)
        {
            n.value = _serializedRepr[i];
            n = n.next;
        }
    }

    #endregion
}
