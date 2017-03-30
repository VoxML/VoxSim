using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Network
{
	public class CSUClient
	{
		private const int IntSize = sizeof(Int32);
		private TcpClient _client;
		private Thread _t;
		private Queue<string> _messages;

		public void Connect(string address, int port)
		{
			_messages = new Queue<string>();
			_client = new TcpClient();
			_client.Connect(address, port);
			_t  = new Thread(loop);
			_t.Start();
		}

		private void loop()
		{
			while (_client.Connected)
			{
				int len = GetMessageLength();
				_messages.Enqueue(GetMessage(len));
			}
		}

		public void Close()
		{
			_t.Abort();
			_client.Close();
		}

		private string GetMessage(int len)
		{
            byte[] byteBuffer = new byte[len];
			NetworkStream stream = _client.GetStream();
            int numBytesRead = stream.Read(byteBuffer, 0, len);
            return Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
		}

		private int GetMessageLength()
		{
            byte[] byteBuffer = new byte[IntSize];
			NetworkStream stream = _client.GetStream();
			stream.Read(byteBuffer, 0, IntSize);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(byteBuffer);
			}
			return BitConverter.ToInt32(byteBuffer, 0);
		}

		public int HowManyLeft()
		{
			return _messages.Count;
		}

		public string GetMessage()
		{
			if (_messages.Count > 0)
			{
                return _messages.Dequeue();
			}
			return "";
		}
	}
}