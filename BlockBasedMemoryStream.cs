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

        /// <summary>
        /// Returns the set Buffer size.
        /// </summary>
        public int BufferSize
        {
            get => _bufferSize;
        }

        /// <summary>
        /// Creates a memory stream based on a linked list with fixed size buffers.
        /// <para/>
        /// The default buffer-size is 65535 bytes (2^16 - 1).
        /// </summary>
        public BlockBasedMemoryStream()
        {
            _init(ushort.MaxValue);
        }

        /// <summary>
        /// Creates a memory stream based on a linked list with custom fixed size buffers.
        /// </summary>
        /// <param name="bufferSize">Custom size of the buffers. The bigger the buffer-size is, the faster it is to add, although more memory will be wasted.</param>
        public BlockBasedMemoryStream(int bufferSize)
        {
            _init(bufferSize);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        /// <summary>
        /// Gets the size of the BlockBasedMemoryStream. Be aware, a full loop-through the list will be necessary. 
        /// </summary>
        public override long Length => _getLength();

        /// <summary>
        /// Not supported. Will throw a NotSupportedException.
        /// </summary>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// This method does nothing, has no effect.
        /// </summary>
        public override void Flush()
        {
            return;
        }

        /// <summary>
        /// Reads from the BlockBasedMemoryStream.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset in the destination buffer.</param>
        /// <param name="count">The amount of bytes to read. Must be less or equal than the size of the buffer-parameter minus the offset.</param>
        /// <returns>Returns the amount of bytes read. </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _read(buffer, offset, count, removeReadData: true);
        }

        /// <summary>
        /// Not supported. You cannot seek in this Stream. Will throw a NotSupportedException.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the length of the stream. The new length cannot be greater than the current length of the Stream.
        /// </summary>
        /// <param name="value">The new length</param>
        public override void SetLength(long value)
        {
            _setLength(value);
        }

        /// <summary>
        /// Writes to the Stream.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">An offset on where to begin copying from buffer.</param>
        /// <param name="count">The maximum amount of bytes to copy.</param>
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
                        int valuePointerOffset = _tail.Value.end;//_tail.Value.start;
                        Buffer.MemoryCopy(sourcePtr, (byte*)_tail.Value.pointer + valuePointerOffset, _bufferSize, bytesToWriteThisRound);
                        bytesLeftToWrite -= bytesToWriteThisRound;
                        _tail.Value.end += bytesToWriteThisRound;
                        bytesWritten += bytesToWriteThisRound;
                    }
                }

                if (newNodeNeeded) _addNodeToTail();
            }
        }

        /// <summary>
        /// Clears the Stream.
        /// </summary>
        public void Clear()
        {
            _init(_bufferSize);
        }

        /// <summary>
        /// Returns a byte array with the content of the stream.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return this.ToArray(false);
        }

        /// <summary>
        /// Returns a byte array with the content of the stream.
        /// </summary>
        /// <param name="removeReadData">Whether or not to remove the returned data from the inner-stream. Default is false.</param>
        /// <returns></returns>
        public byte[] ToArray(bool removeReadData = false)
        {
            byte[] buffer = new byte[this.Length];
            _read(buffer, 0, buffer.Length, removeReadData: removeReadData);
            return buffer;
        }

        /// <summary>
        /// Skips a number of bytes.
        /// </summary>
        /// <param name="numberOfBytes">The number of bytes to skip.</param>
        public void Skip(int numberOfBytes)
        {
            this.Read(new byte[numberOfBytes], 0, numberOfBytes);
        }

        private int _read(byte[] buffer, int offset, int count, bool removeReadData = true)
        {
            if (count > (buffer.Length - offset)) throw new ArgumentOutOfRangeException($"{nameof(count)} was bigger than ({nameof(buffer)}.Length - {nameof(offset)})");
            if (offset > buffer.Length) throw new ArgumentOutOfRangeException("Offset was bigger than buffer");
            if (count == 0 || buffer.Length == 0) return 0;

            int currentIndex = 0;
            Node current = _head;
            unsafe
            {
                fixed (byte* destPtr = &buffer[offset])
                {
                    int i = 0;
                    while (currentIndex <= count && current != null)
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
            int newEndPos = (int)(newLength % _bufferSize);
            Node current = _head;
            ArgumentException exceptionToThrow = new ArgumentException("The new length is greater the current length, which is unsupported.");

            int i = 0;
            while (i < numberOfHops && current != null)
            {
                current = current.Next;

                if (current == null)
                {
                    throw exceptionToThrow;
                }

                ++i;
            }
            if (newEndPos > current.Value.end)
            {
                throw exceptionToThrow;
            }

            current.Value.end = newEndPos;
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
