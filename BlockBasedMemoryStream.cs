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

        private Node[] _pool;
        private int _currentPoolPos;

        private int _blockSize;
        private bool _useLengthCaching;
        private long _cachedLength;

        /// <summary>
        /// Please use BlockBasedMemoryStream.BlockSize instead.
        /// <para/>Returns the set Buffer size.
        /// </summary>
        [ObsoleteAttribute("BlockBasedMemoryStream.BufferSize is being phased out due to confusing naming - please use BlockBasedMemoryStream.BlockSize instead.", false)]
        public int BufferSize
        {
            get => _blockSize;
        }

        /// <summary>
        /// Returns the set Buffer size.
        /// </summary>
        public int BlockSize
        {
            get => _blockSize;
        }

        /// <summary>
        /// Gets or set whether or to use length-caching. Turn it off to be absolutely sure of always getting the correct length, but at the cost of performance.
        /// </summary>
        public bool UseLengthCaching
        {
            get => _useLengthCaching;
            set => _useLengthCaching = value;
        }

        /// <summary>
        /// Gets or sets the size of the block pool. Once a block is released, it will either be placed into the pool, for it be reused, or it will be released. 
        /// Set the PoolSize to 0 to turn this feature off.
        /// Reusing blocks can be beneficial if You read and write huge amounts of data, however it will come at the cost of added memory consumption (BlockSize * PoolSize).
        /// <para/>Changing the PoolSize once already set is not recommended, as it will require copying the nodes from one array to another.
        /// </summary>
        public int PoolSize
        {
            get => _pool.Length;
            set => _setPoolSize(value);
        }

        /// <summary>
        /// Creates a memory stream based on a linked list with fixed size buffers.
        /// <para/>
        /// The default buffer-size is 65535 bytes (2^16 - 1).
        /// </summary>
        /// <param name="useLengthCaching"></param>
        /// <param name="poolSize"></param>
        public BlockBasedMemoryStream(bool useLengthCaching = true, int poolSize = 0)
        {
            _init(ushort.MaxValue, useLengthCaching, poolSize);
        }

        /// <summary>
        /// Creates a memory stream based on a linked list with custom fixed size buffers.
        /// </summary>
        /// <param name="bufferSize">Custom size of the buffers. The bigger the buffer-size is, the faster it is to add, although more memory will be wasted.</param>
        /// <param name="useLengthCaching"></param>
        /// <param name="poolSize"></param>
        public BlockBasedMemoryStream(int blockSize, bool useLengthCaching = true, int poolSize = 0)
        {
            _init(blockSize, useLengthCaching, poolSize);
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

        public override void CopyTo(Stream destination, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;
            do
            {
                bytesRead = this.Read(buffer, 0, buffer.Length);
                destination.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);
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
                int spaceLeftInTail = (_blockSize - _tail.Value.end);
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
                        int valuePointerOffset = _tail.Value.end;
                        Buffer.MemoryCopy(sourcePtr, (byte*)_tail.Value.pointer + valuePointerOffset, _blockSize, bytesToWriteThisRound);
                        bytesLeftToWrite -= bytesToWriteThisRound;
                        _tail.Value.end += bytesToWriteThisRound;
                        bytesWritten += bytesToWriteThisRound;
                    }
                }

                if (newNodeNeeded) _addNodeToTail();
            }

            _cachedLength += count;
        }

        /// <summary>
        /// Clears the Stream.
        /// </summary>
        public void Clear()
        {
            _init(_blockSize, _useLengthCaching, _pool.Length);
        }

        protected override void Dispose(bool disposing)
        {
            this.Clear();
            _head = null;
            _tail = null;
            UseLengthCaching = false;
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
                    while (currentIndex < count && current != null)
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
                                _setNewHead(current.Next);
                            }
                        }
                        currentIndex += bytesToCopyThisRound;
                        current = current.Next;
                        ++i;
                    }
                }
            }
            if (removeReadData)
            {
                if (_head == null)
                {
                    this.Clear();
                }
                else
                {
                    _cachedLength -= currentIndex;
                }
            }
            return currentIndex;
        }

        private void _setNewHead(Node newHead)
        {
            if (_pool.Length > _currentPoolPos + 1)
            {
                Node oldHead = _head;
                oldHead.Value.start = 0;
                oldHead.Value.end = 0;
                _pool[++_currentPoolPos] = oldHead;
            }
            _head = newHead;
        }

        private Node _addNodeToTail()
        {
            Node nodeToAdd;
            if (_currentPoolPos > 0)
            {
                nodeToAdd = _pool[_currentPoolPos];
                _pool[_currentPoolPos] = null;
                --_currentPoolPos;
            }
            else
            {
                nodeToAdd = new Node(_blockSize);
            }
            _tail.Next = nodeToAdd;
            _tail = nodeToAdd;

            return nodeToAdd;
        }

        private void _init(int blockSize, bool useLengthCaching, int poolSize)
        {
            _blockSize = blockSize;
            Node nodeToAdd = new Node(_blockSize);
            _tail = nodeToAdd;
            _head = nodeToAdd;

            _useLengthCaching = useLengthCaching;
            _cachedLength = 0;

            _pool = new Node[poolSize];
            _currentPoolPos = -1;
        }

        private long _getLength()
        {
            long counter = 0;
            if (!_useLengthCaching)
            {
                Node current = _head;
                while (current != null)
                {
                    counter += (current.Value.end - current.Value.start);
                    current = current.Next;
                }
            }
            else
            {
                counter = _cachedLength;
            }

            _cachedLength = counter;
            return counter;
        }

        private void _setLength(long newLength)
        {
            int numberOfHops = (int)(newLength / _blockSize);
            int newEndPos = (int)(newLength % _blockSize);
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
            _cachedLength = newLength;
        }

        private void _setPoolSize(int newSize)
        {
            Node[] newPool = new Node[newSize];
            int i = 0;
            while (i < _pool.Length && i < newPool.Length && i <= _currentPoolPos)
            {
                newPool[i] = _pool[i];
                ++i;
            }

            if (_currentPoolPos > newSize - 1)
            {
                _currentPoolPos = newSize - 1;
            }

            _pool = newPool;
        }
    }
}
