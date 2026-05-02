using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Program
{
    class Node
    {
        public long Xl, Fl;
        public Node Prev, Next;
        public int Version;
    }

    // Простая реализация минимальной кучи для замены PriorityQueue
    class MinHeap
    {
        private List<(Node node, long fl, long xl, int version)> heap = new List<(Node, long, long, int)>();

        public int Count => heap.Count;

        public void Enqueue(Node node, long fl, long xl)
        {
            heap.Add((node, fl, xl, node.Version));
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (Compare(heap[parent], heap[i]) <= 0) break;
                Swap(i, parent);
                i = parent;
            }
        }

        public (Node node, bool isValid) Dequeue()
        {
            var (node, fl, xl, version) = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            if (heap.Count > 0)
            {
                int i = 0;
                while (true)
                {
                    int left = 2 * i + 1;
                    int right = 2 * i + 2;
                    int smallest = i;
                    if (left < heap.Count && Compare(heap[left], heap[smallest]) < 0) smallest = left;
                    if (right < heap.Count && Compare(heap[right], heap[smallest]) < 0) smallest = right;
                    if (smallest == i) break;
                    Swap(i, smallest);
                    i = smallest;
                }
            }
            
            bool isValid = (node.Version == version);
            return (node, isValid);
        }

        private int Compare((Node node, long fl, long xl, int version) a, (Node node, long fl, long xl, int version) b)
        {
            int cmp = a.fl.CompareTo(b.fl);
            if (cmp != 0) return cmp;
            return a.xl.CompareTo(b.xl);
        }

        private void Swap(int i, int j)
        {
            var tmp = heap[i];
            heap[i] = heap[j];
            heap[j] = tmp;
        }
    }

    static int pos;
    static byte[] buffer;

    static long ReadLong()
    {
        while (pos < buffer.Length && (buffer[pos] < '0' || buffer[pos] > '9') && buffer[pos] != '-') pos++;
        int sign = 1;
        if (pos < buffer.Length && buffer[pos] == '-') { sign = -1; pos++; }
        long result = 0;
        while (pos < buffer.Length && buffer[pos] >= '0' && buffer[pos] <= '9')
        {
            result = result * 10 + (buffer[pos] - '0');
            pos++;
        }
        return result * sign;
    }

    static long ReadDoubled()
    {
        while (pos < buffer.Length && (buffer[pos] < '0' || buffer[pos] > '9') && buffer[pos] != '-' && buffer[pos] != '.') pos++;
        int sign = 1;
        if (pos < buffer.Length && buffer[pos] == '-') { sign = -1; pos++; }
        long intPart = 0;
        while (pos < buffer.Length && buffer[pos] >= '0' && buffer[pos] <= '9')
        {
            intPart = intPart * 10 + (buffer[pos] - '0');
            pos++;
        }
        long fracHalf = 0;
        if (pos < buffer.Length && buffer[pos] == '.')
        {
            pos++;
            if (pos < buffer.Length && buffer[pos] >= '0' && buffer[pos] <= '9')
            {
                if (buffer[pos] == '5') fracHalf = 1;
                pos++;
                while (pos < buffer.Length && buffer[pos] >= '0' && buffer[pos] <= '9') pos++;
            }
        }
        return sign * (2 * intPart + fracHalf);
    }

    static void Main()
    {
        using (var ms = new MemoryStream())
        {
            Console.OpenStandardInput().CopyTo(ms);
            buffer = ms.ToArray();
        }

        long W = ReadLong();
        long H = ReadLong();
        int n = (int)ReadLong();

        long[] xd = new long[n];
        long[] yd = new long[n];
        for (int i = 0; i < n; i++)
        {
            xd[i] = ReadDoubled();
            yd[i] = ReadDoubled();
        }

        var diag = new Dictionary<long, List<(long xd, int idx)>>();
        for (int i = 0; i < n; i++)
        {
            long d = xd[i] - yd[i];
            if (!diag.TryGetValue(d, out var list))
            {
                list = new List<(long, int)>();
                diag[d] = list;
            }
            list.Add((xd[i], i));
        }
        var diagPtr = new Dictionary<long, int>();
        foreach (var kv in diag)
        {
            kv.Value.Sort((a, b) => a.xd.CompareTo(b.xd));
            diagPtr[kv.Key] = 0;
        }

        long Wd = 2 * W;
        var head = new Node { Xl = -1, Fl = -1, Version = 0 };
        var tail = new Node { Xl = Wd, Fl = -2, Version = 0 };
        var first = new Node { Xl = 0, Fl = 0, Version = 0 };
        head.Next = first; first.Prev = head;
        first.Next = tail; tail.Prev = first;

        var pq = new MinHeap();
        pq.Enqueue(first, 0L, 0L);

        long[] result = new long[n];

        for (int step = 0; step < n; step++)
        {
            Node node = null;
            while (pq.Count > 0)
            {
                var (nd, isValid) = pq.Dequeue();
                if (isValid) { node = nd; break; }
            }

            long xl = node.Xl, fl = node.Fl;
            long xr = node.Next.Xl;
            long d = xl - fl;

            var list = diag[d];
            int p = diagPtr[d]++;
            (long xdC, int idx) = list[p];
            long s_d = xdC - xl;
            result[idx] = s_d;

            long xMid = xl + 2 * s_d;

            Node prev = node.Prev, next = node.Next;
            node.Version++;  // Инвалидация версии вместо флага Removed

            Node leftNew = new Node { Xl = xl, Fl = fl + 2 * s_d, Version = 0 };
            Node rightNew = null;

            prev.Next = leftNew;
            leftNew.Prev = prev;
            if (xMid < xr)
            {
                rightNew = new Node { Xl = xMid, Fl = fl, Version = 0 };
                leftNew.Next = rightNew;
                rightNew.Prev = leftNew;
                rightNew.Next = next;
                next.Prev = rightNew;
            }
            else
            {
                leftNew.Next = next;
                next.Prev = leftNew;
            }

            Node leftSurvivor;
            if (prev.Fl == leftNew.Fl)
            {
                prev.Next = leftNew.Next;
                leftNew.Next.Prev = prev;
                leftNew.Version++;  // Инвалидация вместо Removed флага
                leftSurvivor = prev;
            }
            else
            {
                pq.Enqueue(leftNew, leftNew.Fl, leftNew.Xl);
                leftSurvivor = leftNew;
            }

            Node rightSurvivor;
            if (rightNew != null)
            {
                pq.Enqueue(rightNew, rightNew.Fl, rightNew.Xl);
                rightSurvivor = rightNew;
            }
            else
            {
                rightSurvivor = leftSurvivor;
            }

            if (rightSurvivor.Fl == next.Fl)
            {
                rightSurvivor.Next = next.Next;
                next.Next.Prev = rightSurvivor;
                next.Version++;  // Инвалидация вместо Removed флага
            }
        }

        var sb = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(result[i]);
        }
        sb.Append('\n');
        Console.Write(sb);
    }
}
