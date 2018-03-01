﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using UnityEngine;

namespace Network
{
	public class EventLearningEventArgs : EventArgs {
		public string Content { get; set; }

		public EventLearningEventArgs(string content, bool macroEvent = false)
		{
			this.Content = content;
		}
	}

	public class EventLearningClient
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

		private const int IntSize = sizeof(Int32);
		private TcpClient _client;
		private Thread _t;
		private Queue<string> _messages;
		private byte[] _ok = new byte[] { 0x20 };

	    public bool IsConnected()
	    {
	        return _client.Connected;
	    }

		public void Connect(string address, int port)
		{
			_messages = new Queue<string>();
			_client = new TcpClient();
			_client.Connect(address, port);
			_t  = new Thread(Loop);
			_t.Start();
		}

		private void Loop()
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

				//Debug.Log (len);

				byteBuffer = new byte[len];
				int numBytesRead = stream.Read(byteBuffer, 0, len);
				//Debug.Log (numBytesRead);

				string message = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
				_messages.Enqueue (message);
				//_messages.Enqueue (message);
//				Debug.Log (stream.DataAvailable);

			}
			_client.Close();
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
//		    stream.ReadTimeout = 4000;
            int numBytesRead = stream.Read(byteBuffer, 0, len);
//			string message = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
			string message = Encoding.UTF8.GetString(byteBuffer, 0, numBytesRead);
			//stream.Write(_ok, 0, 1);
			return message;
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
			    lock (_messages)
			    {
                    return _messages.Dequeue();
			    }
			}
			return "";
		}
	}
}