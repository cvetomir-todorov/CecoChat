using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Check.Connections.Client
{
    public sealed class PreConnectedHttpHandler
    {
        private readonly EndPoint _endPoint;
        private Stream _stream;

        public PreConnectedHttpHandler(EndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public async Task PreConnect()
        {
            Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Unspecified);

            try
            {
                await socket.ConnectAsync(_endPoint);
                _stream = new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        public ValueTask<Stream> GetConnectedStream(SocketsHttpConnectionContext _, CancellationToken __ = default)
        {
            return new ValueTask<Stream>(_stream);
        }
    }
}
