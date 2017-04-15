using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Network
{
	public class CmdServer : NonBlockingTcpServer
	{
		private StringBuilder _sb;
		private Queue<string> _messages;
		// why does \n not work?
		private const char MessageDelimiter = ';';

		public CmdServer(bool localhost, int port, int clientLimit)
			: base(localhost, port, clientLimit)
		{
			_messages = new Queue<string>();
		}

		public override void Process()
		{
			while (true)
			{
				Debug.Log("Listening for a remote commander");
				// make sure we have a socket connected; this line BLOCKS,
				// so Process() needs to be run from a sep. thread.
				Socket clientSocket = _listener.AcceptSocket();
				string connectedpoint = clientSocket.RemoteEndPoint.ToString();
				Debug.Log("Remote commander accepted at: " + connectedpoint);
				NetworkStream stream = new NetworkStream(clientSocket);

				byte[] byteBuffer = new byte[128];
				_sb = new StringBuilder();

				while (IsConnected(clientSocket))
				{
					int numBytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length);
					if (numBytesRead == 0) continue;
					string gotten = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
					string[] lines = gotten.Split(MessageDelimiter);
					if (lines.Length == 0) continue;

					// take chunks until the second to last
					for (int i = 0; i < lines.Length - 1; i++)
					{
						_sb.Append(lines[i]);
						_messages.Enqueue(_sb.ToString());
						// TODO 3/29/17-12:38 do we need to send something back?
						byte[] okResponse = {0x20};
						stream.Write(okResponse, 0, okResponse.Length);
						_sb = new StringBuilder();
					}
					// leave the last piece to concatenate with the following chunks
					_sb.Append(lines[lines.Length - 1]);
				}
				clientSocket.Close();
				stream.Dispose();
				Debug.Log("Disconnected: " + connectedpoint);
			}
		}

		// method to check if the client is still connected
		private bool IsConnected(Socket socket)
		{
			try
			{
				return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
			}
			catch (SocketException) { return false; }
		}

		public string GetMessage()
		{
			return _messages.Count > 0 ? _messages.Dequeue() : "";
		}

		public string GetMessageOld()
		{
			if (_sb == null) return "";
			string message = _sb.ToString();
			_sb = new StringBuilder();
			return message;
		}
	}
}