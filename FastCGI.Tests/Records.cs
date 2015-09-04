using System;
using System.Collections.Generic;
using System.Linq;
using FastCGI;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace FastCGI.Tests
{
    [TestFixture]
    class Records
    {
        [Test]
        public void Records_Creation()
        {
            // Tests for Record.Create* and Record.WriteToStream
            // Do some Record.Create* calls and make sure that both reading and writing
            // produces the expected results.

            var bytesFCGI_MAX_CONNS = Encoding.ASCII.GetBytes(Constants.FCGI_MAX_CONNS);
            var bytesFCGI_MAX_REQS = Encoding.ASCII.GetBytes(Constants.FCGI_MAX_REQS);
            var bytesFCGI_MPXS_CONNS = Encoding.ASCII.GetBytes(Constants.FCGI_MPXS_CONNS);

            var bytesByValue = new Dictionary<Record, byte[]>
            {
                {
                    Record.CreateStdout(new byte[0], 1),
                    new byte[] {
                        Constants.FCGI_VERSION_1, // Version byte
                        Constants.FCGI_STDOUT,    // Record type
                        0x00,                     // Request Id byte 1
                        0x01,                     // Request Id byte 2
                        0x00,                     // Content length byte 1
                        0x00,                     // Content length byte 2
                        0x00,                     // Padding length
                        0x00,                     // Reserved byte
                        // Zero content bytes, zero padding bytes
                    }
                },
                {
                    Record.CreateStdout(new byte[] { 0x01, 0x02, 0x03 }, 256),
                    new byte[] {
                        Constants.FCGI_VERSION_1, // Version byte
                        Constants.FCGI_STDOUT,    // Record type
                        0x01,                     // Request Id byte 1
                        0x00,                     // Request Id byte 2
                        0x00,                     // Content length byte 1
                        0x03,                     // Content length byte 2
                        0x00,                     // Padding length
                        0x00,                     // Reserved byte
                        0x01, 0x02, 0x03          // Content bytes
                    }
                },
                {
                    Record.CreateStdout(new byte[] { 0x00, 0x00, 0x00 }, 65535),
                    new byte[] {
                        Constants.FCGI_VERSION_1, // Version byte
                        Constants.FCGI_STDOUT,    // Record type
                        0xff,                     // Request Id byte 1
                        0xff,                     // Request Id byte 2
                        0x00,                     // Content length byte 1
                        0x03,                     // Content length byte 2
                        0x00,                     // Padding length
                        0x00,                     // Reserved byte
                        0x00, 0x00, 0x00          // Content bytes
                    }
                },
                {
                    Record.CreateEndRequest(1),
                    new byte[] {
                        Constants.FCGI_VERSION_1, // Version byte
                        Constants.FCGI_END_REQUEST,// Record type
                        0x00,                     // Request Id byte 1
                        0x01,                     // Request Id byte 2
                        0x00,                     // Content length byte 1
                        0x08,                     // Content length byte 2
                        0x00,                     // Padding length
                        0x00,                     // Reserved byte
                        0x00, 0x00, 0x00, 0x00,   // appStatus
                        Constants.FCGI_REQUEST_COMPLETE, // protocolStatus
                        0x00, 0x00, 0x00 // reserved bytes
                    }
                },
                {
                    Record.CreateGetValuesResult(123, 456, false),
                    new byte[] {
                        Constants.FCGI_VERSION_1, // Version byte
                        Constants.FCGI_GET_VALUES_RESULT,// Record type
                        0x00,                     // Request Id byte 1
                        0x00,                     // Request Id byte 2
                        0x00,                     // Content length byte 1
                        (byte) (                  // Content length byte 2
                            3*2 // namevalue headers are 3 times 2 bytes
                            + bytesFCGI_MAX_CONNS.Length // other lengths of the actual namevalue contents
                            + 0x03
                            + bytesFCGI_MAX_REQS.Length
                            + 0x03
                            + bytesFCGI_MPXS_CONNS.Length
                            + 0x01
                            ),
                        0x00,                     // Padding length
                        0x00,                     // Reserved byte
                    }
                    // Append name length, value length
                    .Concat(new byte[] { (byte)bytesFCGI_MAX_CONNS.Length, 0x03 })
                    // Append name
                    .Concat(bytesFCGI_MAX_CONNS)
                    // Append value
                    .Concat(Encoding.ASCII.GetBytes("123"))
                    // Append name length, value length
                    .Concat(new byte[] { (byte)bytesFCGI_MAX_REQS.Length, 0x03 })
                    // Append name
                    .Concat(bytesFCGI_MAX_REQS)
                    // Append value
                    .Concat(Encoding.ASCII.GetBytes("456"))
                    // Append name length, value length
                    .Concat(new byte[] { (byte)bytesFCGI_MPXS_CONNS.Length, 0x01 })
                    // Append name
                    .Concat(bytesFCGI_MPXS_CONNS)
                    // Append value
                    .Concat(Encoding.ASCII.GetBytes("0"))
                    .ToArray()
                },
            };

            foreach (var kvp in bytesByValue)
            {
                var record = kvp.Key;
                var expectedBytes = kvp.Value;
                var stream = new MemoryStream(expectedBytes.Length);

                // Write the record to a stream with WriteVarLength.
                record.WriteToStream(stream);

                Assert.AreEqual(expectedBytes.Length, stream.Length);

                // Read it back
                var result = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(result, 0, (int)stream.Length);

                // Make sure that:

                // a) The bytes are correctly encoded
                Assert.AreEqual(expectedBytes, result);

                // b) ReadRecord produces the exact same record we started with
                stream.Seek(0, SeekOrigin.Begin);
                var read = Record.ReadRecord(stream);
                Assert.AreEqual(record, read);
            }
        }

        [Test]
        public void Records_BrokenRecords()
        {
            // Create some broken records and make sure the right things happen
            Record result;

            // An empty stream should simply result in a null record
            var emptyStream = new MemoryStream(new byte[0]);
            result = Record.ReadRecord(emptyStream);
            Assert.IsNull(result);

            // A wrong version number bytes should throw a InvalidDataException
            var wrongVersion = new MemoryStream(new byte[1] { Constants.FCGI_VERSION_1 + 1 });
            Assert.Throws(typeof(InvalidDataException), () => { Record.ReadRecord(wrongVersion); });


            // So should a bunch of zeroes
            var zeroes = new MemoryStream(new byte[4] { 0, 0, 0, 0 });
            Assert.Throws(typeof(InvalidDataException), () => { Record.ReadRecord(zeroes); });

            // And a correct version number with an incomplete header
            var incomplete = new MemoryStream(new byte[4] { Constants.FCGI_VERSION_1, 0, 0, 0 });
            Assert.Throws(typeof(InvalidDataException), () => { Record.ReadRecord(incomplete); });


            // Writing should fail for content above 64KB
            var tooLong = new Record
            {
                Version = Constants.FCGI_VERSION_1,
                Type = Record.RecordType.Stderr,
                RequestId = 123,
                ContentLength = 127000,
                ContentData = new byte[127000]
            };
            Assert.Throws(typeof(InvalidOperationException), () => { tooLong.WriteToStream(new MemoryStream()); });
        }

        [Test]
        public void Records_Equals()
        {
            var recordA1 = new Record
            {
                Version = Constants.FCGI_VERSION_1,
                Type = Record.RecordType.Stderr,
                RequestId = 123,
                ContentLength = 1,
                ContentData = new byte[1] { 1 }
            };

            var recordA2 = new Record
            {
                Version = Constants.FCGI_VERSION_1,
                Type = Record.RecordType.Stderr,
                RequestId = 123,
                ContentLength = 1,
                ContentData = new byte[1] { 1 }
            };

            var recordB1 = new Record
            {
                Version = Constants.FCGI_VERSION_1,
                Type = Record.RecordType.BeginRequest,
                RequestId = 321,
                ContentLength = 1,
                ContentData = new byte[1] { 1 }
            };

            var recordB2 = new Record
            {
                Version = Constants.FCGI_VERSION_1,
                Type = Record.RecordType.BeginRequest,
                RequestId = 321,
                ContentLength = 1,
                ContentData = new byte[1] { 1 }
            };

            var recordC1 = new Record
            {
                Version = Constants.FCGI_VERSION_1,
                Type = Record.RecordType.BeginRequest,
                RequestId = 321,
                ContentLength = 1,
                ContentData = new byte[1] { 2 }
            };

            var recordC2 = new Record
            {
                Version = Constants.FCGI_VERSION_1,
                Type = Record.RecordType.BeginRequest,
                RequestId = 321,
                ContentLength = 1,
                ContentData = new byte[1] { 2 }
            };

            Assert.IsTrue(recordA1.Equals(recordA2));
            Assert.IsTrue(recordB1.Equals(recordB2));
            Assert.IsTrue(recordC1.Equals(recordC2));

            Assert.IsFalse(recordA1.Equals(recordB2));
            Assert.IsFalse(recordA1.Equals(recordB1));
            Assert.IsFalse(recordA1.Equals(recordC1));

            Assert.IsFalse(recordB1.Equals(recordA1));
            Assert.IsFalse(recordB1.Equals(recordA2));
            Assert.IsFalse(recordB1.Equals(recordC1));

            Assert.IsFalse(recordB1.Equals(null));

            Assert.IsFalse(recordB1.Equals(5));
        }

        [Test]
        public void Records_ToString()
        {
            var someRecord = new Record
            {
                Version = Constants.FCGI_VERSION_1,
                Type = Record.RecordType.Stderr,
                RequestId = 123,
                ContentLength = 15,
                ContentData = new byte[15]
            };

            string stringified = someRecord.ToString();
            Assert.AreEqual("{Record type: Stderr, requestId: 123}", stringified);
        }
    }
}
