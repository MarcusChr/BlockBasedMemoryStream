using NUnit.Framework;
using System;
using System.IO;

namespace com.marcuslc.BlockBasedMemoryStream.Tests
{
    public class Tests
    {
        private Random _sharedRandom;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _sharedRandom = new Random();
        }

        [Test]
        public void Write_numberOfBytes_lengthIsSameAsWritten([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            //Assert
            Assert.That(memoryBasedMemoryStream.Length, Is.EqualTo(numberOfBytesToWrite));
        }

        [Test]
        public void Write_WriteBytesMultipleTimes_DataEqualsDataWritten([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
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
                memoryBasedMemoryStream.Read(bytesRead[i]);
            }

            //Assert
            for (int i = 0; i < bytesToWrite.Length; i++)
            {
                Assert.That(bytesRead[i], Is.EquivalentTo(bytesToWrite[i]));
            }
        }

        [Test]
        public void Read_WriteBytes_DataEqualsDataWritten([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            byte[] bytesRead = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.Read(bytesRead);

            //Assert
            Assert.That(bytesRead, Is.EquivalentTo(bytesToWrite));
        }

        [Test]
        public void ToArray_WriteBytes_ToArrayEqualsDataWritten([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            byte[] bytesRead = memoryBasedMemoryStream.ToArray();

            //Assert
            Assert.That(bytesRead, Is.EquivalentTo(bytesToWrite));
        }

        [Test]
        public void Clear_WriteBytesThenClear_LengthEqualsZero([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.Clear();

            //Assert
            Assert.That(memoryBasedMemoryStream.Length, Is.EqualTo(0));
        }

        [Test]
        public void CopyTo_WriteDataThenCopyToNormalMemoryStream_TargetMemoryStreamEqualsWrittenData([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
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
            Assert.That(targetMemoryStream.ToArray(), Is.EquivalentTo(bytesToWrite));
        }

        [Test]
        public void SetLength_WritesBytesThenSetsLength_LengthEqualsTheSetLength([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
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
            Assert.That(memoryBasedMemoryStream.Length, Is.EqualTo(targetLength));
        }

        [Test]
        public void Seek_AttemptsToCallTheSeekMethod_ANotSupportedExceptionIsThrown([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);

            //Act
            var exceptionThrown = Assert.Throws<NotSupportedException>(() => memoryBasedMemoryStream.Seek(0, SeekOrigin.Begin));

            //Assert
            Assert.That(exceptionThrown, Is.Not.Null);
        }

        [Test] //Another exception should be thrown. NullReferenceException is thrown because 'Dispose()' sets an internal field variable to null, which is then accessed when trying to write to the stream.
        public void Dispose_AttempsToWriteAfterDisposingStream_AnExceptionIsThrown([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite);
            memoryBasedMemoryStream.Dispose();
            var exceptionThrown = Assert.Throws<NullReferenceException>(() => memoryBasedMemoryStream.Write(bytesToWrite));

            //Assert
            Assert.That(exceptionThrown, Is.Not.Null);
        }

        [Test]
        public void Skip_WritesBytesSkipsThenReadsTheBytes_TheReadBytesShouldEqualTheBytesReadExcludingTheSkippedBytes([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite, [Values(16, 32)] int numberOfBytesToSkip)
        {
            //Arrange
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
            Assert.That(bytesRead, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void ClearPool_WriteBytesClearPoolThenReadBytesAgain_TheReadBytesShouldEqualTheBytesWritten([Values] bool isUsingValueCaching, [Values(0, 8)] int poolSize, [Values(64, ushort.MaxValue * 8)] int numberOfBytesToWrite)
        {
            //Arrange
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(isUsingValueCaching, poolSize);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            byte[] bytesRead = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            memoryBasedMemoryStream.ClearPool();
            memoryBasedMemoryStream.Read(bytesRead);

            //Assert
            Assert.That(bytesRead, Is.EquivalentTo(bytesToWrite));
        }
    }
}