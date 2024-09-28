using System;
using System.IO;

namespace BlockBasedMemoryStream
{
    public class BlockBasedMemoryStream : Stream
    {
        private Node _head;
        private Node _tail;

        private Node[] _pool;
        private int _currentPoolPos;

        private long _cachedLength;

        /// <summary>
        /// Returns the set size of the blocks.
        /// </summary>
        public int BlockSize { get; private set; }

        /// <summary>
        /// Gets or set whether or to use length-caching. Turn it off to be absolutely sure of always getting the correct length, but at the cost of performance.
        /// </summary>
        public bool UseLengthCaching { get; set; }

        /// <summary>
        /// Gets or sets the size of the block pool. Once a block is released, it will either be placed into the pool, for it be reused, or it will be released. 
        /// Set the PoolSize to 0 to turn this feature off.
        /// Reusing blocks can be beneficial if You read and write huge amounts of data, however it will come at the cost of added memory consumption (BlockSize * PoolSize).
        /// <para/>Changing the PoolSize once already set is not recommended, as it will require copying the nodes from one array to another.
        /// </summary>
        public int PoolSize
        {
            get => _pool.Length;
            set => InternalSetPoolSize(value);
        }

        /// <summary>
        /// Creates a memory stream based on a linked list with fixed size buffers.
        /// <para/>
        /// The default buffer-size is 65535 bytes (2^16 - 1).
        /// </summary>
        /// <param name="useLengthCaching">Whether or not to use cached length. Using cached length is on by default and is faster.</param>
        /// <param name="poolSize">The size of the pool of blocks to reuse once once released. Using a higher pool size can increase performance, but at the cost of increased memory consumption.</param>
        public BlockBasedMemoryStream(bool useLengthCaching = true, int poolSize = 0)
        {
            InternalInit(ushort.MaxValue, useLengthCaching, poolSize);
        }

        /// <summary>
        /// Creates a memory stream based on a linked list with custom fixed size buffers.
        /// </summary>
        /// <param name="blockSize">Custom size of the buffers. The bigger the buffer-size is, the faster it is to add, although more memory will be wasted.</param>
        /// <param name="useLengthCaching">Whether or not to use cached length. Using cached length is on by default and is faster.</param>
        /// <param name="poolSize">The size of the pool of blocks to reuse once released. Using a higher pool size can increase performance, but at the cost of increased memory consumption.</param>
        public BlockBasedMemoryStream(int blockSize, bool useLengthCaching = true, int poolSize = 0)
        {
            InternalInit(blockSize, useLengthCaching, poolSize);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        /// <summary>
        /// Gets the size of the BlockBasedMemoryStream. Be aware, a full loop-through the list will be necessary. 
        /// </summary>
        public override long Length => InternalGetLength();

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
            // No-op
        }

        /// <summary>
        /// Reads from the BlockBasedMemoryStream.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset in the destination buffer.</param>
        /// <param name="count">The amount of bytes to read. Must be less or equal than the size of the buffer-parameter minus the offset.</param>
        /// <returns>Returns the amount of bytes read. </returns>
        public override int Read(byte[] buffer, int offset, int count)
            => InternalRead(buffer, offset, count, removeReadData: true);

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
            InternalSetLength(value);
        }

        /// <summary>
        /// Writes to the Stream.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">An offset on where to begin copying from buffer.</param>
        /// <param name="count">The maximum amount of bytes to copy.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var bytesWritten = 0;
            var bytesLeftToWrite = count;
            while (bytesLeftToWrite > 0)
            {
                var spaceLeftInTail = BlockSize - _tail.Value.end;
                var newNodeNeeded = (spaceLeftInTail < bytesLeftToWrite);
                var bytesToWriteThisRound = bytesLeftToWrite;

                if (newNodeNeeded)
                {
                    bytesToWriteThisRound = spaceLeftInTail;
                }

                unsafe
                {
                    fixed (void* sourcePtr = &buffer[offset + bytesWritten])
                    {
                        int valuePointerOffset = _tail.Value.end;
                        Buffer.MemoryCopy(sourcePtr, (byte*)_tail.Value.pointer + valuePointerOffset, BlockSize,
                            bytesToWriteThisRound);
                        bytesLeftToWrite -= bytesToWriteThisRound;
                        _tail.Value.end += bytesToWriteThisRound;
                        bytesWritten += bytesToWriteThisRound;
                    }
                }

                if (newNodeNeeded)
                {
                    _ = AddNodeToTail();
                }
            }

            _cachedLength += count;
        }

        /// <summary>
        /// Clears the Stream.
        /// </summary>
        public void Clear()
        {
            InternalInit(BlockSize, UseLengthCaching, _pool.Length);
        }

        /// <summary>
        /// Clears the pool of reuseable blocks. This method is useful for when You are done reading from the Stream, but want to keep the instance alive. 
        /// <para/><b>Note:</b> Calling this method will not prevent you from reading/writing in the future, it will just clear the current pool.
        /// </summary>
        public void ClearPool()
        {
            for (; _currentPoolPos > -1; _currentPoolPos--)
            {
                _pool[_currentPoolPos] = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Clear();
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
            return ToArray(false);
        }

        /// <summary>
        /// Returns a byte array with the content of the stream.
        /// </summary>
        /// <param name="removeReadData">Whether or not to remove the returned data from the inner-stream. Default is false.</param>
        /// <returns></returns>
        public byte[] ToArray(bool removeReadData)
        {
            byte[] buffer = new byte[Length];
            InternalRead(buffer, 0, buffer.Length, removeReadData: removeReadData);
            return buffer;
        }

        /// <summary>
        /// Skips a number of bytes.
        /// </summary>
        /// <param name="numberOfBytes">The number of bytes to skip.</param>
        public void Skip(int numberOfBytes)
            => _ = Read(new byte[numberOfBytes], 0, numberOfBytes);

        private int InternalRead(byte[] buffer, int offset, int count, bool removeReadData = true)
        {
            if (count > (buffer.Length - offset))
                throw new ArgumentOutOfRangeException(
                    $"{nameof(count)} was bigger than ({nameof(buffer)}.Length - {nameof(offset)})");
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset was bigger than buffer");
            if (count == 0 || buffer.Length == 0) return 0;

            int currentIndex = 0;
            Node current = _head;
            unsafe
            {
                fixed (byte* destPtr = &buffer[offset])
                {
                    while (currentIndex < count && current != null)
                    {
                        ValueBlock value = current.Value;
                        int bytesToCopyThisRound = (value.end - value.start);
                        int bytesLeftToCopy = (count - currentIndex);

                        if (bytesToCopyThisRound > bytesLeftToCopy)
                        {
                            bytesToCopyThisRound = bytesLeftToCopy;
                        }

                        Buffer.MemoryCopy((byte*)value.pointer + value.start, destPtr + currentIndex, count,
                            bytesToCopyThisRound);

                        var previousNode = current;
                        current = current.Next;

                        if (removeReadData)
                        {
                            previousNode.Value.start += bytesToCopyThisRound;

                            if (previousNode.Value.start >= previousNode.Value.end)
                            {
                                SetNewHead(previousNode.Next);
                            }
                        }

                        currentIndex += bytesToCopyThisRound;
                    }
                }
            }

            if (removeReadData)
            {
                if (_head == null)
                {
                    _resetHeadAndTail();
                }
                else
                {
                    _cachedLength -= currentIndex;
                }
            }

            return currentIndex;
        }

        private void SetNewHead(Node newHead)
        {
            Node oldHead = _head;
            _head = newHead;
            if (_pool.Length > _currentPoolPos + 1)
            {
                oldHead.Value.start = 0;
                oldHead.Value.end = 0;
                oldHead.Next = null;
                _pool[++_currentPoolPos] = oldHead;
            }
        }

        private Node AddNodeToTail()
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
                nodeToAdd = new Node(BlockSize);
            }

            _tail.Next = nodeToAdd;
            _tail = nodeToAdd;

            return nodeToAdd;
        }

        private void InternalInit(int blockSize, bool useLengthCaching, int poolSize)
        {
            BlockSize = blockSize;

            _resetHeadAndTail();

            UseLengthCaching = useLengthCaching;

            _pool = new Node[poolSize];
            _currentPoolPos = -1;
        }

        private void _resetHeadAndTail()
        {
            var nodeToAdd = new Node(BlockSize);
            _tail = nodeToAdd;
            _head = nodeToAdd;

            _cachedLength = 0;
        }

        private long InternalGetLength()
        {
            long counter = 0;
            if (!UseLengthCaching)
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

        private void InternalSetLength(long newLength)
        {
            var numberOfHops = (int)(newLength / BlockSize);
            var newEndPos = (int)(newLength % BlockSize);
            var current = _head;
            ArgumentException exceptionToThrow =
                new ArgumentException("The new length is greater the current length, which is unsupported.");

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
            ValueBlock value = current.Value;

            if (value.start > value.end)
            {
                current.Value.start = value.end;
            }

            current.Next = null;
            _tail = current;
            _cachedLength = newLength;
        }

        private void InternalSetPoolSize(int newSize)
        {
            var newPool = new Node[newSize];
            var i = 0;
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