using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Read/write like a queue.
/// Performance of a linked list.
/// GC-friendliness of a circular buffer.
/// </summary>
/// <typeparam name="T">Type stored</typeparam>
public class RecyclingLinkedQueue<T> : IEnumerable<T>
{
    public Node Head { get; private set; }
    public Node Tail { get; private set; }
    public int Count { get; private set; }

    public class Node
    {
        public T value;
        public Node next { get; internal set; }

        private static List<Node> recycled = new List<Node>();
        internal static Node New(T val)
        {
            //Get or instantiate
            Node n;
            if (recycled.Count > 0)
            {
                n = recycled[recycled.Count - 1];
                recycled.RemoveAt(recycled.Count-1);
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
        if(Tail != null) Tail.next = newTail;
        if(Count==0) { Head = newTail; Tail = newTail; }
        Tail = newTail;
        ++Count;
    }

    public void Insert(int index, T val)
    {
        if (index < 0 || index > Count) throw new IndexOutOfRangeException();

        Node @new = Node.New(val);
        if (Count != 0)
        {
            Node before = GetNodeAt(index - 1);
            Node after = before.next;
            if(before != null) before.next = @new;
            @new.next = after;
            ++Count;
        }
        else
        {
            Head = @new;
            Tail = @new;
            ++Count;
        }
    }

    //WARNING: Not necessarily safe! Doesn't verify that node belongs to this data structure.
    public void Insert(Node before, T val)
    {
        Node @new = Node.New(val);
        
        Node after = before.next;
        if(before != null) before.next = @new;
        @new.next = after;
        ++Count;
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
        Node.Recycle(Head);
        Head = next;

        --Count;
    }

    public void Clear()
    {
        for (Node i = Head; i != null; i = i.next) Node.Recycle(i);
        Head = null;
        Tail = null;
        Count = 0;
    }

    private Node GetNodeAt(int index)
    {
        if (index < 0 || index >= Count) return null;
        Node ptr = Head;
        for (int i = 0; i < index; ++i) ptr = ptr.next;
        return ptr;
    }
    
    public Node FindNode(Func<T, bool> selector)
    {
        for (Node i = Head; i != null; i = i.next) if(selector(i.value)) return i;
        return null;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (Node i = Head; i != null; i = i.next) yield return i.value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
