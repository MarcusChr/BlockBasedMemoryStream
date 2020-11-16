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
    public class BlockBasedMemoryStream : Stream, IDisposable
    {
        private Node _head;
        private Node _tail;

        private int _bufferSize;

        public int BufferSize
        {
            get => _bufferSize;
        }

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
            return;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > (buffer.Length - offset)) throw new ArgumentOutOfRangeException($"{nameof(count)} was bigger than ({nameof(buffer)}.Length - {nameof(offset)})");
            if (offset > buffer.Length) throw new ArgumentOutOfRangeException("Offset was bigger than buffer");

            return _read(buffer, offset, count, removeReadData: true);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            _setLength(value);
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

        public byte[] ToArray()
        {
            return this.ToArray(false);
        }

        public byte[] ToArray(bool removeReadData = false)
        {
            byte[] buffer = new byte[this.Length];
            _read(buffer, 0, buffer.Length, removeReadData: removeReadData);
            return buffer;
        }

        public void Skip(int numberOfBytes)
        {
            this.Read(new byte[numberOfBytes], 0, numberOfBytes); //This might be a temporary solution - who knows?
        }

        private int _read(byte[] buffer, int offset, int count, bool removeReadData = true)
        {
            if (count > (buffer.Length - offset)) throw new ArgumentOutOfRangeException($"{nameof(count)} was bigger than ({nameof(buffer)}.Length - {nameof(offset)})");
            if (offset > buffer.Length) throw new ArgumentOutOfRangeException("Offset was bigger than buffer");
            if (count == 0 || buffer.Length == 0) return 0;

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
                        int bytesToCopyThisRound = (value.end - value.start);
                        int bytesLeftToCopy = (count - currentIndex);

                        if (bytesToCopyThisRound > bytesLeftToCopy)
                        {
                            bytesToCopyThisRound = bytesLeftToCopy;
                        }

                        Buffer.MemoryCopy((byte*)value.pointer + value.start, destPtr + currentIndex, count, bytesToCopyThisRound);
                        if (removeReadData)
                        {
                            current.Value.start += bytesToCopyThisRound;

                            if (current.Value.start >= current.Value.end)
                            {
                                _head = current.Next;

                                if (_head == null)
                                {
                                    this.Clear();
                                }
                            }
                        }
                        currentIndex += bytesToCopyThisRound;
                        current = current.Next;
                        ++i;
                    }
                }
            }

            return currentIndex;
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

        private void _setLength(long newLength)
        {
            int numberOfHops = (int)(newLength / _bufferSize);
            Node current = _head;

            int i = 0;
            while (i < numberOfHops && current != null)
            {
                current = current.Next;

                if (current == null)
                {
                    throw new ArgumentException("The new length is greater the current length, which is unsupported.");
                }

                ++i;
            }


            current.Value.end = (int)(newLength % _bufferSize);
            ValueHolder value = current.Value;

            if (value.start > value.end)
            {
                current.Value.start = value.end;
            }

            current.Next = null;
            _tail = current;
        }
    }
}
