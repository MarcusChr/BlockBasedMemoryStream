using System;
using System.Collections;
using System.Collections.Generic;

namespace BlockBasedMemoryStream.Tests
{
    public class BlockDataTest : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return CreateBlockData(true, 0, 64);
            yield return CreateBlockData(true, 1, 64);
            
            yield return CreateBlockData(false, 0, 64);
            yield return CreateBlockData(false, 1, 64);
            
            yield return CreateBlockData(true, 0, 128);
            yield return CreateBlockData(true, 1, 128);

            yield return CreateBlockData(true, 8, int.MaxValue / 64);
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private object[] CreateBlockData(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            return new object[]
            {
                isUsingValueCaching,
                poolSize,
                numberOfBytesToWrite
            };
        }
    }
}