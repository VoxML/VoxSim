using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace VoxSimPlatform {
    namespace Network {
    	public class SocketConnection {
            public CommunicationsBridge owner;
            public Type IOClientType;

    		public event EventHandler EventSequenceReceived;

    		public void OnEventSequenceReceived(object sender, EventArgs e) {
    			if (EventSequenceReceived != null) {
    				EventSequenceReceived(this, e);
    			}
    		}

    		public event EventHandler ConnectionLost;

    		public void OnConnectionLost(object sender, EventArgs e) {
    			if (ConnectionLost != null) {
    				ConnectionLost(this, e);
    			}
    		}

    		public event EventHandler ConnectionMade;

    		public void OnConnectionMade(object sender, EventArgs e) {
    			if (ConnectionMade != null) {
    				ConnectionMade(this, e);
    			}
    		}

    		protected const int IntSize = sizeof(Int32);
    		protected TcpClient _client;
    		protected Thread _t;
    		protected Queue<string> _messages;
    		protected byte[] _ok = new byte[] {0x20};

    		string _label;
            public string Label {
                get { return _label; }
                set { _label = value; }
            }
                
            string _address;
    		public string Address {
    			get { return _address; }
    			set { _address = value; }
    		}

    		int _port;
    		public int Port {
    			get { return _port; }
    			set { _port = value; }
    		}

    		public virtual bool IsConnected() {
    			return !((_client.Client.Poll(10, SelectMode.SelectRead) && (_client.Client.Available == 0)) ||
    			         !_client.Client.Connected);
    		}

    		public virtual void Connect(string address, int port) {
    			_address = address;
    			_port = port;
    			//Debug.Log (string.Format("{0}:{1}",address,port));

    			_messages = new Queue<string>();
    			_client = new TcpClient();

    			var result = _client.BeginConnect(address, port, null, null);
    			var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

    			if (!success) {
    				throw new Exception("Failed to connect.");
    				return;
    			}

    			_client.EndConnect(result);
    			//_client.Connect(address, port);
    			_t = new Thread(Loop);
    			_t.Start();
                Debug.Log(string.Format("VoxSim ({0}) is connected to {1} on port {2} with outbound port {3}",
                    ((IPEndPoint)_client.Client.LocalEndPoint).Address, ((IPEndPoint)_client.Client.RemoteEndPoint).Address,
                    ((IPEndPoint)_client.Client.RemoteEndPoint).Port, ((IPEndPoint)_client.Client.LocalEndPoint).Port));
    		}

    		protected virtual void Loop() {
    			while (IsConnected()) {
    				NetworkStream stream = _client.GetStream();
    				byte[] byteBuffer = new byte[IntSize];
    				try {
    					stream.Read(byteBuffer, 0, IntSize);
    				}
    				catch (Exception e) {
    					Debug.LogError(e.Message);
    				}

    				//				if (!BitConverter.IsLittleEndian)
    				//				{
    				//					Array.Reverse(byteBuffer);
    				//				}
    				int len = BitConverter.ToInt32(byteBuffer, 0);

    				byteBuffer = new byte[len];
    				int numBytesRead = stream.Read(byteBuffer, 0, len);
    				//Debug.Log (numBytesRead);

    				string message = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
    				_messages.Enqueue(message);
    				//_messages.Enqueue (message);
    			}

    			//_client.Close();
    		}

    		public virtual void Close() {
    			_t.Abort();
    			_client.GetStream().Close();
    			_client.Close();
    		}

    		protected virtual string GetMessage(int len) {
    			byte[] byteBuffer = new byte[len];
    			NetworkStream stream = _client.GetStream();
    //		    stream.ReadTimeout = 4000;
    			int numBytesRead = stream.Read(byteBuffer, 0, len);
    //			string message = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
    			string message = Encoding.UTF8.GetString(byteBuffer, 0, numBytesRead);
    			//stream.Write(_ok, 0, 1);
    			return message;
    		}

    		protected virtual int GetMessageLength() {
    			byte[] byteBuffer = new byte[IntSize];
    			NetworkStream stream = _client.GetStream();
    			stream.Read(byteBuffer, 0, IntSize);
    			if (BitConverter.IsLittleEndian) {
    				Array.Reverse(byteBuffer);
    			}

    			return BitConverter.ToInt32(byteBuffer, 0);
    		}

    		public virtual int HowManyLeft() {
    			return _messages.Count;
    		}

    		public virtual string GetMessage() {
    //			Debug.Log (_messages.Count);
    			if (_messages.Count > 0) {
    				lock (_messages) {
    					return _messages.Dequeue();
    				}
    			}

    			return "";
    		}
    	}
    }
}