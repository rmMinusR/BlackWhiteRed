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
    [NonSerialized] private RecyclingNode<T> _head = null;
    [NonSerialized] private RecyclingNode<T> _tail = null;
    [NonSerialized] private int _count = 0;
    public RecyclingNode<T> Head
    {
        get => Count > 0 ? _head : throw new IndexOutOfRangeException("Collection is empty");
        private set => _head = value;
    }
    public RecyclingNode<T> Tail
    {
        get => Count > 0 ? _tail : throw new IndexOutOfRangeException("Collection is empty");
        private set => _tail = value;
    }
    public int Count
    {
        get => _count;
        private set => _count = value;
    }

    public int ManualCount()
    {
        HashSet<int> visitedIDs = new HashSet<int>();
        for (RecyclingNode<T> i = _head; i != null; i = i.next)
        {
            if (!visitedIDs.Contains(i.GetHashCode())) visitedIDs.Add(i.GetHashCode());
            else throw new InvalidProgramException("Already visited "+i);
        }
        return visitedIDs.Count;
    }

    public void Enqueue(T val)
    {
        RecyclingNode<T> newTail = RecyclingNode<T>.New(val);
        if (_count == 0) _head = newTail;
        if (_tail != null) _tail.next = newTail;
        _tail = newTail;
        ++_count;
    }

    public void Insert(int index, T val)
    {
        if (index < 0 || index > _count) throw new IndexOutOfRangeException();

        RecyclingNode<T> before = GetNodeAt(index - 1);
        RecyclingNode<T> @new = RecyclingNode<T>.New(val);
        RecyclingNode<T> after = before.next;

        //Link
        before.next = @new;
        @new.next = after;
        
        //Adjust refs (if applicable)
        if (index == 0) _head = @new;
        if (index == _count) _tail = @new;
        
        ++_count;
    }

    //WARNING: Not necessarily safe! Doesn't verify that Node<T> belongs to this data structure.
    public void Insert(RecyclingNode<T> before, T val)
    {
        if (before == null) throw new NullReferenceException();
        
        RecyclingNode<T> @new = RecyclingNode<T>.New(val);
        RecyclingNode<T> after = before.next;

        if (before == _tail) _tail = @new;

        before.next = @new;
        @new.next = after;
        ++_count;
    }

    public T Peek()
    {
        if (Count == 0) throw new IndexOutOfRangeException("Collection is empty");
        return Head.value;
    }

    public T Dequeue()
    {
        if (Count == 0) throw new IndexOutOfRangeException("Collection is empty");

        T val = Head.value;
        DropHead();
        return val;
    }

    public void DropHead()
    {
        if (Count == 0) throw new IndexOutOfRangeException("Collection is empty");

        //Advance head forward, recycling
        RecyclingNode<T> next = Head.next;
        RecyclingNode<T>.Recycle(Head);
        if (_count > 1) _head = next;
        else
        {
            _head = null;
            _tail = null;
        }

        --_count;
    }

    public void Clear()
    {
        for (RecyclingNode<T> i = _head; i != null; i = i.next) RecyclingNode<T>.Recycle(i);
        _head = null;
        _tail = null;
        _count = 0;
    }

    public RecyclingNode<T> GetNodeAt(int index)
    {
        if (Count == 0) throw new IndexOutOfRangeException("Collection is empty");
        if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
        RecyclingNode<T> ptr = _head;
        for (int i = 0; i < index; ++i) ptr = ptr.next;
        return ptr;
    }

    public RecyclingNode<T> FindNode(Func<T, bool> selector)
    {
        if (Count == 0) throw new IndexOutOfRangeException("Collection is empty");
        for (RecyclingNode<T> i = _head; i != null; i = i.next) if (selector(i.value)) return i;
        throw new IndexOutOfRangeException("Selector didn't match any nodes");
    }
    
    public RecyclingNode<T> FindNode(Func<RecyclingNode<T>, bool> selector)
    {
        if (Count == 0) throw new IndexOutOfRangeException("Collection is empty");
        for (RecyclingNode<T> i = _head; i != null; i = i.next) if (selector(i)) return i;
        throw new IndexOutOfRangeException("Selector didn't match any nodes");
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (RecyclingNode<T> i = _head; i != null; i = i.next) yield return i.value;
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
        RecyclingNode<T> n = _head;
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
        RecyclingNode<T> n = _head;
        for (int i = 0; i < _serializedRepr.Count; ++i)
        {
            n.value = _serializedRepr[i];
            n = n.next;
        }
    }

    #endregion
}

[Serializable]
public class RecyclingNode<T>
{
    public T value;

    [NonSerialized] private RecyclingNode<T> __next;
    public RecyclingNode<T> next { get => __next; internal set => __next = value; }

    private RecyclingNode()
    {
        id = instanceCount++;
        isAlive = false;
        next = null;
        value = default;
    }

    private static HashSet<RecyclingNode<T>> recycled = new HashSet<RecyclingNode<T>>();
    internal static RecyclingNode<T> New(T val)
    {
        //Get or instantiate
        RecyclingNode<T> n;
        if (recycled.Count > 0)
        {
            n = recycled.First();
            recycled.Remove(n);
        }
        else
        {
            n = new RecyclingNode<T>();
        }

        Debug.Assert(!n.isAlive, "Tried to retrieve recycled Node<"+typeof(T).Name+">#"+n.id+", but it was already alive! #"+n.id);
        n.isAlive = true;

        n.value = val;
        n.next = null;
        return n;
    }

    internal static void Recycle(RecyclingNode<T> n)
    {
        Debug.Assert(n.isAlive, "Tried to recycle Node<"+typeof(T).Name+">#"+n.id+", but it was already dead! #"+n.id);
        n.isAlive = false;

        recycled.Add(n);
    }

    private static int instanceCount = 0;
    private int id;
    private bool isAlive;

    public override int GetHashCode() => id;

    public override string ToString() => base.ToString() + "#"+id + (next != null ? "->"+next.id : ".");
}