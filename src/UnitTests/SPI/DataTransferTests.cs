using NUnit.Framework;
using DuetControlServer.SPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using LinuxApi;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using DuetControlServer.Utility;
using DuetControlServer.SPI.Communication.Shared;
using DuetControlServer.SPI.Communication;
using System.Numerics;

namespace DuetControlServer.SPI.Tests
{
    [TestFixture()]
    public class InputGpioPinTest : IInputGpioPin, IDisposable
    {
        public InputGpioPinTest()
        {
            Value = true;
        }

        ~InputGpioPinTest() => DisposeInternal();

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        private bool disposed = false;

        private void DisposeInternal()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
        }

        public bool WaitForEvent(int timeout)
        {
            Value = !Value;
            return Value;    
        }

        public void FlushEvents()
        {

        }

        public bool Value { get; private set; }
        public event PinChangeDelegate? PinChanged;
        public Task StartMonitoring(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                
            }, cancellationToken);
        }

    }

    public sealed class SpiDeviceTest : ISpiDevice, IDisposable
    {
        /// <summary>
        /// Initialize an SPI device
        /// </summary>
        /// <param name="devNode">Path to the /dev node</param>
        /// <param name="speed">Transfer speed in Hz</param>
        /// <param name="transferMode">Transfer mode</param>
        public unsafe SpiDeviceTest(string devNode, int speed, int transferMode)
        {

        }

        /// <summary>
        /// Finalizer of this class
        /// </summary>
        ~SpiDeviceTest() => Dispose(false);

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Indicates if this instance has been disposed
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Dispose this instance internally
        /// </summary>
        /// <param name="disposing">Release managed resourcess</param>
        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
        }

        public unsafe void TransferFullDuplex(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
        {
            if (writeBuffer.Length != readBuffer.Length)
            {
                throw new ArgumentException($"Parameters '{nameof(writeBuffer)}' and '{nameof(readBuffer)}' must have the same length.");
            }

            if (readBuffer.Length == sizeof(uint))
            {
                ResponseCodeFromRRF(readBuffer);
            }
            else if (readBuffer.Length == sizeof(TransferHeader))
            {
                ResponseHeaderFromRRF(readBuffer);
            }
        }

        private static readonly Memory<byte> _rxHeaderBuffer = new byte[Marshal.SizeOf<TransferHeader>()];
        private static TransferHeader _rxHeader;

        public int ResponseCodeState { get; set; }
        public int ResponseHeaderState { get; set; }

        public int ResponseDataState { get; set; }

        public List<uint> ResponseCodeSequence { get; set; }
        public List<TransferHeader> ResponseHeaderSequence { get; set; }
        public List<Memory<byte>> ResponseDataSequence { get; set; }

        private unsafe void ResponseCodeFromRRF(Span<byte> readBuffer)
        {
            
            uint response =  ResponseCodeSequence[ResponseCodeState++];

            MemoryMarshal.Write(readBuffer, ref response);
        }

        private unsafe void ResponseHeaderFromRRF(Span<byte> readBuffer)
        {
            //_rxHeader.FormatCode = Consts.FormatCode;
            //_rxHeader.SequenceNumber = 0;
            //_rxHeader.NumPackets = 0;
            //_rxHeader.ProtocolVersion = Consts.ProtocolVersion;
            //_rxHeader.DataLength = 0;

            _rxHeader = ResponseHeaderSequence[ResponseHeaderState++];
            _rxHeader.DataLength = (ushort)ResponseDataSequence[ResponseDataState].Length;

            _rxHeader.ChecksumData32 = CRC32.Calculate(ResponseDataSequence[ResponseDataState++].Span);
            MemoryMarshal.Write(_rxHeaderBuffer.Span, ref _rxHeader);
            _rxHeader.ChecksumHeader32 = CRC32.Calculate(_rxHeaderBuffer[..12].Span);
            MemoryMarshal.Write(_rxHeaderBuffer.Span, ref _rxHeader);
            _rxHeaderBuffer.Span.CopyTo(readBuffer);
            _rxHeader = MemoryMarshal.Read<TransferHeader>(_rxHeaderBuffer.Span);
        }
    }

    public class DataTransferTests
    {
        [Test()]
        public unsafe void PerformFullTransferTestBadHeaderChecksum()
        {
            var transferReadyPin = new InputGpioPinTest();
            var spiDeviceTest = new SpiDeviceTest("/dev/spidev0.0", 1000000, 0);

            spiDeviceTest.ResponseCodeSequence = new List<uint>(new uint[] { 
                TransferResponse.BadHeaderChecksum,
                TransferResponse.Success
            });

            spiDeviceTest.ResponseHeaderSequence = new List<TransferHeader>(new TransferHeader[] {
                new TransferHeader() {
                    FormatCode = Consts.FormatCode,
                    SequenceNumber = 1,
                    NumPackets = 0,
                    ProtocolVersion = Consts.ProtocolVersion,
                    DataLength = 0
                },
                new TransferHeader() {
                    FormatCode = Consts.FormatCode,
                    SequenceNumber = 1,
                    NumPackets = 0,
                    ProtocolVersion = Consts.ProtocolVersion,
                    DataLength = 0
                }
            });

            spiDeviceTest.ResponseDataSequence = new List<Memory<byte>>(new Memory<byte>[]
            {
                new byte[0],
                new byte[0]
            });

            DataTransfer.Init(transferReadyPin, spiDeviceTest);

            Assert.AreEqual(spiDeviceTest.ResponseCodeState, 2);
            Assert.AreEqual(spiDeviceTest.ResponseHeaderState, 2);
            Assert.Pass();
        }

        [Test()]
        public unsafe void PerformFullTransferTestBadHeaderChecksumInDataExchange()
        {
            var transferReadyPin = new InputGpioPinTest();
            var spiDeviceTest = new SpiDeviceTest("/dev/spidev0.0", 1000000, 0);

            spiDeviceTest.ResponseCodeSequence = new List<uint>(new uint[] { 
                TransferResponse.Success, 
                TransferResponse.Success,
                TransferResponse.Success,
                TransferResponse.BadHeaderChecksum,
                TransferResponse.Success,
                TransferResponse.Success,
                TransferResponse.Success
            });

            spiDeviceTest.ResponseHeaderSequence = new List<TransferHeader>(new TransferHeader[] {
                new TransferHeader() {
                    FormatCode = Consts.FormatCode,
                    SequenceNumber = 1,
                    NumPackets = 0,
                    ProtocolVersion = Consts.ProtocolVersion,
                    DataLength = 0
                },
                new TransferHeader() {
                    FormatCode = Consts.FormatCode,
                    SequenceNumber = 2,
                    NumPackets = 0,
                    ProtocolVersion = Consts.ProtocolVersion,
                    DataLength = 0
                },
                new TransferHeader() {
                    FormatCode = Consts.FormatCode,
                    SequenceNumber = 3,
                    NumPackets = 0,
                    ProtocolVersion = Consts.ProtocolVersion,
                    DataLength = 0
                },
                new TransferHeader() {
                    FormatCode = Consts.FormatCode,
                    SequenceNumber = 3,
                    NumPackets = 0,
                    ProtocolVersion = Consts.ProtocolVersion,
                    DataLength = 0
                },
                new TransferHeader() {
                    FormatCode = Consts.FormatCode,
                    SequenceNumber = 4,
                    NumPackets = 0,
                    ProtocolVersion = Consts.ProtocolVersion,
                    DataLength = 0
                }
            });

            spiDeviceTest.ResponseDataSequence = new List<Memory<byte>>(new Memory<byte>[]
            {
                new byte[0],
                new byte[0],
                new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new byte[0],
                new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 }
            });

            DataTransfer.Init(transferReadyPin, spiDeviceTest);
            DataTransfer.PerformFullTransfer();
            DataTransfer.PerformFullTransfer();
            DataTransfer.PerformFullTransfer();

            Assert.AreEqual(spiDeviceTest.ResponseCodeState, 7);
            Assert.AreEqual(spiDeviceTest.ResponseHeaderState, 5);
            Assert.AreEqual(DataTransfer.HadReset(), false);
            Assert.Pass();
        }
    }
}