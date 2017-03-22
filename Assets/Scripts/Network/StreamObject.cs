using System.Net.Sockets;
using System.Text;

namespace Network
{
	public class StreamObject
	{
		// Client  socket.
		public Socket Socket;
		// Size of receive buffer.
		public int BufferSize;

		// Receive buffer.
		public byte[] Buffer;
		// Received data string.
		private StringBuilder _sb = new StringBuilder();

		public StreamObject(int bufferSize, Socket socket)
			:this(bufferSize)
		{
			Socket = socket;

		}
		public StreamObject(int bufferSize)
		{
			BufferSize = bufferSize;
			Buffer = new byte[BufferSize];
		}

		public override string ToString()
		{
			return _sb.ToString();
		}

		public bool HasContents()
		{
			return _sb.Length > 1;
		}

		public void Append(int length)
		{
			_sb.Append(Encoding.ASCII.GetString(Buffer, 0, length));
		}
	}
}