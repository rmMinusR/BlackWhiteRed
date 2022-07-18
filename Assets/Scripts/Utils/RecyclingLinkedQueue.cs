using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [NonSerialized] private Node _head = null;
    [NonSerialized] private Node _tail = null;
    [NonSerialized] private int _count = 0;
    public Node Head
    {
        get => Count > 0 ? _head : throw new IndexOutOfRangeException("Collection is empty");
        private set => _head = value;
    }
    public Node Tail
    {
        get => Count > 0 ? _tail : throw new IndexOutOfRangeException("Collection is empty");
        private set => _tail = value;
    }
    public int Count
    {
        get => _count;
        private set => _count = value;
    }

    [Serializable]
    public class Node
    {
        public T value;

        [NonSerialized] private Node __next;
        public Node next { get => __next; internal set => __next = value; }

        private static HashSet<Node> recycled = new HashSet<Node>();
        internal static Node New(T val)
        {
            //Get or instantiate
            Node n;
            if (recycled.Count > 0)
            {
                n = recycled.First();
                recycled.Remove(n);
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
        if (_count == 0) _head = newTail;
        if (_tail != null) _tail.next = newTail;
        _tail = newTail;
        ++_count;
    }

    public void Insert(int index, T val)
    {
        if (index < 0 || index > _count) throw new IndexOutOfRangeException();

        Node @new = Node.New(val);
        if (_count != 0)
        {
            if (index > 0)
            {
                Node before = GetNodeAt(index - 1);
                Node after = before.next;
                before.next = @new;
                @new.next = after;
            }
            else
            {
                //Index = 0
                Node after = Head;
                Head = @new;
                @new.next = after;
            }
        }
        else
        {
            _head = @new;
            _tail = @new;
        }
        ++_count;
    }

    //WARNING: Not necessarily safe! Doesn't verify that node belongs to this data structure.
    public void Insert(Node before, T val)
    {
        Node @new = Node.New(val);

        Node after = before.next;
        if (before != null) before.next = @new;
        @new.next = after;
        ++_count;
    }

    public T Peek() => Head.value;

    public T Dequeue()
    {
        T val = Head.value;
        DropHead();
        return val;
    }

    public void DropHead()
    {
        //Advance head forward, recycling
        Node next = Head.next;
        Node.Recycle(_head);
        _head = next;

        --_count;
    }

    public void Clear()
    {
        for (Node i = _head; i != null; i = i.next) Node.Recycle(i);
        _head = null;
        _tail = null;
        _count = 0;
    }

    public Node GetNodeAt(int index)
    {
        if (index < 0 || index >= _count) throw new IndexOutOfRangeException();
        Node ptr = _head;
        for (int i = 0; i < index; ++i) ptr = ptr.next;
        return ptr;
    }

    public Node FindNode(Func<T, bool> selector)
    {
        for (Node i = _head; i != null; i = i.next) if (selector(i.value)) return i;
        return null;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (Node i = _head; i != null; i = i.next) yield return i.value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #region Serialization

    [SerializeField] private List<T> _serializedRepr = new List<T>();

    public void OnBeforeSerialize()
    {
        //Ensure same size
        while (_serializedRepr.Count > _count) _serializedRepr.RemoveAt(_serializedRepr.Count - 1);
        while (_serializedRepr.Count < _count) _serializedRepr.Add(default);

        //Write values
        Node n = _head;
        for (int i = 0; i < _count; ++i)
        {
            _serializedRepr[i] = n.value;
            n = n.next;
        }
    }

    public void OnAfterDeserialize()
    {
        //Ensure same size
        while (_count > _serializedRepr.Count) DropHead();
        while (_count < _serializedRepr.Count) Enqueue(default);

        //Write values
        Node n = _head;
        for (int i = 0; i < _serializedRepr.Count; ++i)
        {
            n.value = _serializedRepr[i];
            n = n.next;
        }
    }

    #endregion
}
