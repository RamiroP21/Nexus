using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    public interface IDistrictAuthorityTransport : IDisposable
    {
        bool IsConnected { get; }
        Task ConnectAsync(string host, int port, int timeoutMilliseconds, CancellationToken cancellationToken);
        bool TrySend(DistrictAuthorityWireMessage message);
        int Drain(Action<DistrictAuthorityWireMessage> receiver, int maxMessages);
        event Action<string> Faulted;
        void Disconnect();
    }

    // Localhost-only, bounded framed transport. Socket work runs off the Unity
    // thread; consumers decode queued payloads in Update().
    public sealed class DistrictAuthorityTcpTransport : IDistrictAuthorityTransport
    {
        public const int MaxFrameBytes = 131072;
        private const int QueueCapacity = 64;
        private readonly object gate = new object();
        private readonly ConcurrentQueue<byte[]> inbound = new ConcurrentQueue<byte[]>();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private TcpClient client;
        private NetworkStream stream;
        private BlockingCollection<byte[]> outbound;
        private Task reader;
        private Task writer;

        public bool IsConnected { get; private set; }
        public event Action<string> Faulted;

        public async Task ConnectAsync(string host, int port, int timeoutMilliseconds, CancellationToken cancellationToken)
        {
            if (!string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
                && !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("District authority transport only accepts loopback hosts.", nameof(host));
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            TcpClient next = new TcpClient();
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                int boundedTimeout = Math.Max(100, Math.Min(30000, timeoutMilliseconds));
                timeout.CancelAfter(boundedTimeout);
                Task connection = next.ConnectAsync(host, port);
                try
                {
                    Task completed = await Task.WhenAny(connection, Task.Delay(boundedTimeout, timeout.Token)).ConfigureAwait(false);
                    if (completed != connection) throw new TimeoutException("District authority connection timed out.");
                    await connection.ConfigureAwait(false);
                }
                catch { next.Dispose(); throw; }
            }
            lock (gate)
            {
                client = next;
                stream = next.GetStream();
                outbound = new BlockingCollection<byte[]>(QueueCapacity);
                IsConnected = true;
            }
            reader = Task.Run(ReadLoopAsync);
            writer = Task.Run(WriteLoopAsync);
        }

        public bool TrySend(DistrictAuthorityWireMessage message)
        {
            if (!IsConnected || message == null) return false;
            byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));
            if (payload.Length == 0 || payload.Length > MaxFrameBytes) return false;
            byte[] frame = new byte[payload.Length + 4];
            frame[0] = (byte)(payload.Length >> 24); frame[1] = (byte)(payload.Length >> 16);
            frame[2] = (byte)(payload.Length >> 8); frame[3] = (byte)payload.Length;
            Buffer.BlockCopy(payload, 0, frame, 4, payload.Length);
            try { return outbound != null && outbound.TryAdd(frame); }
            catch (InvalidOperationException) { return false; }
        }

        public void Disconnect()
        {
            BlockingCollection<byte[]> queue;
            lock (gate)
            {
                IsConnected = false;
                queue = outbound;
                outbound = null;
                try { stream?.Close(); } catch (IOException) { }
                try { client?.Close(); } catch (SocketException) { }
                stream = null; client = null;
            }
            if (queue != null) queue.Dispose();
        }

        public void Dispose()
        {
            try { lifetime.Cancel(); }
            catch (AggregateException) { }
            Disconnect();
            lifetime.Dispose();
        }

        private async Task ReadLoopAsync()
        {
            try
            {
                while (IsConnected && !lifetime.IsCancellationRequested)
                {
                    byte[] header = await ReadExactlyAsync(4, lifetime.Token).ConfigureAwait(false);
                    int length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
                    if (length <= 0 || length > MaxFrameBytes) throw new InvalidDataException("Invalid authority frame length.");
                    byte[] payload = await ReadExactlyAsync(length, lifetime.Token).ConfigureAwait(false);
                    if (inbound.Count >= QueueCapacity) throw new InvalidDataException("Authority inbound queue is full.");
                    inbound.Enqueue(payload);
                    // Dispatch is drained by the bridge's main-thread Update.
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Faulted?.Invoke(ex.Message); }
            finally { IsConnected = false; }
        }

        private async Task WriteLoopAsync()
        {
            try
            {
                BlockingCollection<byte[]> queue = outbound;
                foreach (byte[] frame in queue.GetConsumingEnumerable(lifetime.Token))
                    await stream.WriteAsync(frame, 0, frame.Length, lifetime.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Faulted?.Invoke(ex.Message); }
        }

        public int Drain(Action<DistrictAuthorityWireMessage> receiver, int maxMessages)
        {
            int count = 0;
            while (count < maxMessages && inbound.TryDequeue(out byte[] payload))
            {
                receiver(JsonUtility.FromJson<DistrictAuthorityWireMessage>(Encoding.UTF8.GetString(payload)));
                count++;
            }
            return count;
        }

        private async Task<byte[]> ReadExactlyAsync(int length, CancellationToken token)
        {
            byte[] result = new byte[length]; int offset = 0;
            while (offset < length)
            {
                int read = await stream.ReadAsync(result, offset, length - offset, token).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException();
                offset += read;
            }
            return result;
        }
    }
}
