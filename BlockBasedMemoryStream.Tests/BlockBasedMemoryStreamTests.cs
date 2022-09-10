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
        public void Write_numberOfBytes_lengthIsSameAsWritten()
        {
            //Arrange
            const int numberOfBytesToWrite = 64;
            var memoryBasedMemoryStream = new BlockBasedMemoryStream(true, 8);
            byte[] bytesToWrite = new byte[numberOfBytesToWrite];
            _sharedRandom.NextBytes(bytesToWrite);

            //Act
            memoryBasedMemoryStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            //Assert
            Assert.That(memoryBasedMemoryStream.Length, Is.EqualTo(numberOfBytesToWrite));
        }
    }
}