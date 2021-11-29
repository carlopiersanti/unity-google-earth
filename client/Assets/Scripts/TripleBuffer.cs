using FFmpeg;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FFmpeg
{
    public class TripleBuffer
    {
        private Tuple<Dictionary<string, rocktree_t.bulk_t>, SortedDictionary<string, rocktree_t.node_t>>[] buffer =
            new Tuple<Dictionary<string, rocktree_t.bulk_t>, SortedDictionary<string, rocktree_t.node_t>>[3];

        private int WriteIndex;
        private int ReadIndex;
        private int IdleIndex;

        private bool IsBufferAvailable;

        private object lockObject = new object();

        public TripleBuffer()
        {
            WriteIndex = 0;
            IdleIndex = 1;
            ReadIndex = 2;
            IsBufferAvailable = false;
        }

        public Tuple<Dictionary<string, rocktree_t.bulk_t>, SortedDictionary<string, rocktree_t.node_t>> GetWriteBuffer() => buffer[WriteIndex];

        public void SwapWriteBuffer(Tuple<Dictionary<string, rocktree_t.bulk_t>, SortedDictionary<string, rocktree_t.node_t>> buffer)
        {
            this.buffer[WriteIndex] = buffer;
            int tmp = WriteIndex;
            lock (lockObject)
            {
                WriteIndex = IdleIndex;
                IdleIndex = tmp;
                IsBufferAvailable = true;
            }
        }

        public Tuple<Dictionary<string, rocktree_t.bulk_t>, SortedDictionary<string, rocktree_t.node_t>> GetReadBuffer()
        {
            lock (lockObject)
            {
                if (!IsBufferAvailable)
                    return null;
                int tmp = ReadIndex;
                ReadIndex = IdleIndex;
                IdleIndex = tmp;
                IsBufferAvailable = false;
                return buffer[ReadIndex];
            }
        }
    }
}