using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using UnityEngine;

namespace Network
{
	public class SocketClient
	{
		public event EventHandler EventSequenceReceived;

		public void OnEventSequenceReceived(object sender, EventArgs e)
		{
			if (EventSequenceReceived != null)
			{
				EventSequenceReceived(this, e);
			}
		}

		public event EventHandler ConnectionLost;

		public void OnConnectionLost(object sender, EventArgs e)
		{
			if (ConnectionLost != null)
			{
				ConnectionLost(this, e);
			}
		}

		protected const int IntSize = sizeof(Int32);
		protected TcpClient _client;
		protected Thread _t;
		protected Queue<string> _messages;
		protected byte[] _ok = new byte[] { 0x20 };

	    public bool IsConnected()
	    {
	        return _client.Connected;
	    }

		public void Connect(string address, int port)
		{
			Debug.Log (string.Format("{0}:{1}",address,port));

			_messages = new Queue<string>();
			_client = new TcpClient();

			var result = _client.BeginConnect(address, port, null, null);
			var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

			if (!success)
			{
				throw new Exception("Failed to connect.");
				return;
			}
			_client.EndConnect(result);
			//_client.Connect(address, port);
			_t = new Thread (Loop);
			_t.Start ();
			Debug.Log ("I am connected to " + ((System.Net.IPEndPoint)_client.Client.RemoteEndPoint).Address.ToString () +
			" on port " + ((System.Net.IPEndPoint)_client.Client.RemoteEndPoint).Port.ToString ());
			Debug.Log ("I am connected from " + ((System.Net.IPEndPoint)_client.Client.LocalEndPoint).Address.ToString () +
			" on port " + ((System.Net.IPEndPoint)_client.Client.LocalEndPoint).Port.ToString ());
		}

		protected void Loop()
		{
			while (_client.Connected)
			{
				NetworkStream stream = _client.GetStream();
				byte[] byteBuffer = new byte[IntSize];
				stream.Read(byteBuffer, 0, IntSize);

				//				if (!BitConverter.IsLittleEndian)
				//				{
				//					Array.Reverse(byteBuffer);
				//				}
				int len = BitConverter.ToInt32(byteBuffer, 0);

				byteBuffer = new byte[len];
				int numBytesRead = stream.Read(byteBuffer, 0, len);
				//Debug.Log (numBytesRead);

				string message = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
				_messages.Enqueue (message);
				//_messages.Enqueue (message);

			}
			_client.Close();
		}

		public void Close()
		{
			_t.Abort();
			_client.Close();
		}

		protected string GetMessage(int len)
		{
            byte[] byteBuffer = new byte[len];
			NetworkStream stream = _client.GetStream();
//		    stream.ReadTimeout = 4000;
            int numBytesRead = stream.Read(byteBuffer, 0, len);
//			string message = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
			string message = Encoding.UTF8.GetString(byteBuffer, 0, numBytesRead);
			//stream.Write(_ok, 0, 1);
			return message;
		}

		protected int GetMessageLength()
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
//			Debug.Log (_messages.Count);
			if (_messages.Count > 0)
			{
			    lock (_messages)
			    {
                    return _messages.Dequeue();
			    }
			}
			return "";
		}
	}
}