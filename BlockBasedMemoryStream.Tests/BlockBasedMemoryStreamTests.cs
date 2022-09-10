using NUnit.Framework;
using System;

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
    }
}