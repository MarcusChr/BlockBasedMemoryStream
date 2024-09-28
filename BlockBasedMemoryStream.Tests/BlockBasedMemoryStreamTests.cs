using System;
using System.IO;
using Xunit;

namespace BlockBasedMemoryStream.Tests
{
    public class BlockBasedMemoryStreamTests
    {
        private readonly Random _sharedRandom = new Random();

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void Write_numberOfBytes_lengthIsSameAsWritten(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            var bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            //Assert
            Assert.Equal(numberOfBytesToWrite, memoryBasedMemoryStream.Length);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void Write_WriteBytesMultipleTimes_DataEqualsDataWritten(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var numberOfRuns = 4;
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);

            byte[][] bytesToWrite = new byte[numberOfRuns][];
            byte[][] bytesRead = new byte[numberOfRuns][];

            for (var i = 0; i < bytesToWrite.Length; i++)
            {
                bytesToWrite[i] = new byte[numberOfBytesToWrite];
                bytesRead[i] = new byte[numberOfBytesToWrite];

                _sharedRandom.NextBytes(bytesToWrite[i]);
            }

            //Act
            for (int i = 0; i < bytesToWrite.Length; i++)
            {
                memoryBasedMemoryStream.Write(bytesToWrite[i], 0, bytesToWrite[i].Length);
                memoryBasedMemoryStream.Read(bytesRead[i], 0, bytesRead[i].Length);
            }

            //Assert
            for (int i = 0; i < bytesToWrite.Length; i++)
            {
                Assert.Equal(bytesToWrite[i], bytesRead[i]);
            }
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void Read_WriteBytes_DataEqualsDataWritten(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            byte[] bytesRead = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.Read(bytesRead, 0, bytesRead.Length);

            //Assert
            Assert.Equal(bytesToWrite, bytesRead);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void ToArray_WriteBytes_ToArrayEqualsDataWritten(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            byte[] bytesRead = memoryBasedMemoryStream.ToArray();

            //Assert
            Assert.Equal(bytesToWrite, bytesRead);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void Clear_WriteBytesThenClear_LengthEqualsZero(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.Clear();

            //Assert
            Assert.Equal(0, memoryBasedMemoryStream.Length);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void CopyTo_WriteDataThenCopyToNormalMemoryStream_TargetMemoryStreamEqualsWrittenData(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            var targetMemoryStream = new MemoryStream();

            byte[] bytesToWrite = new byte[numberOfBytesToWrite];

            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.CopyTo(targetMemoryStream);

            //Assert
            Assert.Equal(bytesToWrite, targetMemoryStream.ToArray());
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void SetLength_WritesBytesThenSetsLength_LengthEqualsTheSetLength(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            var targetLength = numberOfBytesToWrite / 2;

            byte[] bytesToWrite = new byte[numberOfBytesToWrite];

            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.SetLength(targetLength);

            //Assert
            Assert.Equal(targetLength, memoryBasedMemoryStream.Length);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void Seek_AttemptsToCallTheSeekMethod_ANotSupportedExceptionIsThrown(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);

            //Act
            var exceptionThrown = Assert.Throws<NotSupportedException>(() => memoryBasedMemoryStream.Seek(0, SeekOrigin.Begin));

            //Assert
            Assert.NotNull(exceptionThrown);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void Dispose_AttempsToWriteAfterDisposingStream_AnExceptionIsThrown(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.Dispose();
            var exceptionThrown = Assert.Throws<NullReferenceException>(() => memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length));

            //Assert
            Assert.NotNull(exceptionThrown);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void Skip_WritesBytesSkipsThenReadsTheBytes_TheReadBytesShouldEqualTheBytesReadExcludingTheSkippedBytes(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var numberOfBytesToSkip = 64;
                
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            var numberOfExpectedBytes = numberOfBytesToWrite - numberOfBytesToSkip;

            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            byte[] bytesRead = new byte[numberOfExpectedBytes];
            byte[] expectedBytes = new byte[numberOfExpectedBytes];

            _sharedRandom.NextBytes(bytesToWrite);
            Array.Copy(bytesToWrite, numberOfBytesToSkip, expectedBytes, 0, expectedBytes.Length);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.Skip(numberOfBytesToSkip);
            memoryBasedMemoryStream.Read(bytesRead, 0, bytesRead.Length);

            //Assert
            Assert.Equal(expectedBytes, bytesRead);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void ClearPool_WriteBytesClearPoolThenReadBytesAgain_TheReadBytesShouldEqualTheBytesWritten(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            byte[] bytesReadBuffer = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.ClearPool();
            memoryBasedMemoryStream.Read(bytesReadBuffer, 0, bytesReadBuffer.Length);

            //Assert
            Assert.Equal(bytesToWrite, bytesReadBuffer);
        }

        [Theory]
        [ClassData(typeof(BlockDataTest))]
        public void Flush_WriteBytesFlushThenReadBytesAgain_TheReadBytesShouldEqualTheBytesWritten(bool isUsingValueCaching, int poolSize, int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            byte[] bytesRead = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.Flush();
            memoryBasedMemoryStream.Read(bytesRead, 0, bytesRead.Length);

            //Assert
            Assert.Equal(bytesToWrite, bytesRead);
        }
    }
}