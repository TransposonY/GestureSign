using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using Xunit;

namespace GestureSign.Tests.InterProcessCommunication
{
    public class NamedPipeProtocolTests
    {
        [Fact]
        public async Task NamedPipeRoundTripsGesturePointsAndDeviceState()
        {
            var points = new[]
            {
                new[]
                {
                    new[] { new Point(-4, 12), new Point(83, 97) },
                    new[] { new Point(0, 0) }
                }
            };
            var result = await SendAndReceive(IpcCommands.GotGesture, points);
            var actualPoints = Assert.IsType<Point[][][]>(result.Payload);
            Assert.Equal(IpcCommands.GotGesture, result.Command);
            Assert.Equal(points.Length, actualPoints.Length);
            Assert.Equal(points[0].Length, actualPoints[0].Length);
            Assert.Equal(points[0][0], actualPoints[0][0]);
            Assert.Equal(points[0][1], actualPoints[0][1]);

            var devices = Devices.TouchScreen | Devices.TouchPad;
            result = await SendAndReceive(IpcCommands.SynDeviceState, devices);
            Assert.Equal(IpcCommands.SynDeviceState, result.Command);
            Assert.Equal(devices, Assert.IsType<Devices>(result.Payload));
        }

        [Theory]
        [InlineData(new byte[] { 255 })]
        [InlineData(new byte[] { (byte)IpcCommands.GotGesture, (byte)'n', (byte)'u', (byte)'l', (byte)'l' })]
        [InlineData(new byte[] { (byte)IpcCommands.StartControlPanel, (byte)'1' })]
        public async Task RejectsUnknownCommandsAndMismatchedPayloads(byte[] frame)
        {
            string pipeName = NamedPipe.GetUserPipeName("GestureSign-ipc-invalid-" + Guid.NewGuid().ToString("N"));
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                var sender = Task.Run(async () =>
                {
                    using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                    {
                        await client.ConnectAsync(5000);
                        await client.WriteAsync(frame, 0, frame.Length);
                    }
                });
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    await server.WaitForConnectionAsync(timeout.Token);
                Assert.Throws<InvalidDataException>(() => NamedPipe.ReadMessages(server, out _));
                await sender;
            }
        }

        private static async Task<(IpcCommands Command, object Payload)> SendAndReceive(IpcCommands command, object payload)
        {
            string name = "GestureSign-ipc-roundtrip-" + Guid.NewGuid().ToString("N");
            using (var server = new NamedPipeServerStream(NamedPipe.GetUserPipeName(name), PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Task<bool> sender = NamedPipe.SendMessageAsync(command, name, payload, wait: false);
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    await server.WaitForConnectionAsync(timeout.Token);
                object result = NamedPipe.ReadMessages(server, out IpcCommands receivedCommand);
                Assert.True(await sender, "Sending the IPC message failed.");
                return (receivedCommand, result);
            }
        }
    }
}
