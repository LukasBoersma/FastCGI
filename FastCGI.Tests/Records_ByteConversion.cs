using System;
using System.IO;
using FastCGI;
using NUnit.Framework;
using System.Collections.Generic;

namespace FastCGI.Tests
{
    [TestFixture]
    public class Records_ByteConversion
    {
        [Test]
        public void Records_ByteConversion_Int16()
        {
            // Tests for Record.ReadInt16 and Record.WriteInt16
            // Values should be encoded in big-endian mode.
            // Test some numbers and make sure that both read and write
            // produce the expected results.

            var bytesByValue = new Dictionary<UInt16,byte[]> {
                {0, new byte[] {0x00, 0x00}},
                {1, new byte[] {0x00, 0x01}},
                {2, new byte[] {0x00, 0x02}},
                {5, new byte[] {0x00, 0x05}},
                {128, new byte[] {0x00, 0x80}},
                {255, new byte[] {0x00, 0xff}},
                {256, new byte[] {0x01, 0x00}},
                {1024, new byte[] {0x04, 0x00}},
                {1025, new byte[] {0x04, 0x01}},
                {1234, new byte[] {0x04, 0xd2}},
                {4097, new byte[] {0x10, 0x01}},
                {65534, new byte[] {0xff, 0xfe}},
                {65535, new byte[] {0xff, 0xff}}
            };

            foreach (var kvp in bytesByValue)
            {
                var number = kvp.Key;
                var bytes = kvp.Value;
                var stream = new MemoryStream(2);

                // Write the number to a stream with WriteInt16.
                Record.WriteInt16(stream, number);

                Assert.AreEqual(2, stream.Length);

                // Read it back
                var result = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(result, 0, (int)stream.Length);

                // Make sure that:
                
                // a) The bytes are correctly encoded
                Assert.AreEqual(bytes, result);

                // b) ReadInt16 decodes the correct number from the bytes
                stream.Seek(0, SeekOrigin.Begin);
                var read = Record.ReadInt16(stream);
                Assert.AreEqual(number, read);
            }
        }

        [Test]
        public void Records_ByteConversion_VarLength()
        {
            // Tests for Record.ReadVarLength and Record.WriteVarLength
            // Values should be encoded in big-endian mode.
            // Test some numbers and make sure that both read and write
            // produce the expected results.

            var bytesByValue = new Dictionary<UInt32, byte[]> {
                {0, new byte[] {0x00}},
                {1, new byte[] {0x01}},
                {2, new byte[] {0x02}},
                {5, new byte[] {0x05}},
                {128, new byte[] { 0x80, 0x00, 0x00, 0x80}},
                {255, new byte[] { 0x80, 0x00, 0x00, 0xff}},
                {256, new byte[] { 0x80, 0x00, 0x01, 0x00}},
                {1024, new byte[] { 0x80, 0x00, 0x04, 0x00}},
                {1025, new byte[] { 0x80, 0x00, 0x04, 0x01}},
                {1234, new byte[] { 0x80, 0x00, 0x04, 0xd2}},
                {4097, new byte[] { 0x80, 0x00, 0x10, 0x01}},
                {65534, new byte[] { 0x80, 0x00, 0xff, 0xfe}},
                {65535, new byte[] { 0x80, 0x00, 0xff, 0xff}},
                {16777215, new byte[] { 0x80, 0xff, 0xff, 0xff}},
                {17829889, new byte[] { 0x81, 0x10, 0x10, 0x01}},
                {2147483647, new byte[] { 0xff, 0xff, 0xff, 0xff}},
            };

            foreach (var kvp in bytesByValue)
            {
                var number = kvp.Key;
                var expectedBytes = kvp.Value;
                var stream = new MemoryStream(2);

                // Write the number to a stream with WriteVarLength.
                Record.WriteVarLength(stream, number);

                Assert.AreEqual(expectedBytes.Length, stream.Length);

                // Read it back
                var result = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(result, 0, (int)stream.Length);

                // Make sure that:

                // a) The bytes are correctly encoded
                Assert.AreEqual(expectedBytes, result);

                // b) ReadInt16 decodes the correct number from the bytes
                stream.Seek(0, SeekOrigin.Begin);
                var read = Record.ReadVarLength(stream);
                Assert.AreEqual(number, read);
            }
        }

    }
}
