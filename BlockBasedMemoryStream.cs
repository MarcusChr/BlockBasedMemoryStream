using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace com.marcuslc.BlockBasedMemoryStream
{
    public class BlockBasedMemoryStream : Stream
    {
        private Node _head;
        private Node _tail;

        private int _bufferSize;

        public BlockBasedMemoryStream()
        {
            _init(ushort.MaxValue);
        }

        public BlockBasedMemoryStream(int bufferSize)
        {
            _init(bufferSize);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _getLength();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > (buffer.Length - offset)) throw new ArgumentOutOfRangeException($"{nameof(count)} was bigger than ({nameof(buffer)}.Length - {nameof(offset)})");
            if (offset > buffer.Length) throw new ArgumentOutOfRangeException("Offset was bigger than buffer");

            int numberOfHops = (count / _bufferSize) + 1;

            int currentIndex = 0;
            Node current = _head;
            unsafe
            {
                fixed (byte* destPtr = &buffer[offset])
                {
                    int i = 0;
                    while (i < numberOfHops && current != null)
                    {
                        ValueHolder value = current.Value;
                        int bytesToCopy = (value.end - value.start);
                        if(bytesToCopy > count)
                        {
                            bytesToCopy = count;
                        }

                        Buffer.MemoryCopy((byte*)value.pointer + value.start, destPtr + currentIndex, count, bytesToCopy);

                        currentIndex += bytesToCopy;
                        current = current.Next;
                        ++i;
                    }
                }
            }

            return currentIndex;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int bytesWritten = 0;
            int bytesLeftToWrite = count;
            while (bytesLeftToWrite > 0)
            {
                int spaceLeftInTail = (_bufferSize - _tail.Value.end);
                bool newNodeNeeded = (spaceLeftInTail < bytesLeftToWrite);
                int bytesToWriteThisRound = bytesLeftToWrite;

                if (newNodeNeeded)
                {
                    bytesToWriteThisRound = spaceLeftInTail;
                }

                unsafe
                {
                    fixed (void* sourcePtr = &buffer[offset + bytesWritten])
                    {
                        int valuePointerOffset = _tail.Value.start;
                        Buffer.MemoryCopy(sourcePtr, (byte*)_tail.Value.pointer + valuePointerOffset, _bufferSize, bytesToWriteThisRound);
                        bytesLeftToWrite -= bytesToWriteThisRound;
                        _tail.Value.end += bytesToWriteThisRound;
                        bytesWritten += bytesToWriteThisRound;
                    }
                }

                if (newNodeNeeded) _addNodeToTail();
            }
        }

        public void Clear()
        {
            _init(_bufferSize);
        }

        private Node _addNodeToTail()
        {
            Node nodeToAdd = new Node(_bufferSize);
            _tail.Next = nodeToAdd;
            _tail = nodeToAdd;

            return nodeToAdd;
        }

        private void _init(int bufferSize)
        {
            _bufferSize = bufferSize;
            Node nodeToAdd = new Node(_bufferSize);
            _tail = nodeToAdd;
            _head = nodeToAdd;
        }

        private long _getLength()
        {
            Node current = _head;
            long counter = 0;

            while (current != null)
            {
                counter += (current.Value.end - current.Value.start);
                current = current.Next;
            }

            return counter;
        }
    }
}
