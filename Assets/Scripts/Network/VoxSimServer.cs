using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Network
{
	public class VoxSimServer
	{
		private const int nBuffer = 2048;
		// Thread signal.
		public ManualResetEvent allDone = new ManualResetEvent(false);
		private int _port;
		private string inputString;

		public VoxSimServer(int port)
		{
			_port = port;
			inputString = "";
		}

		public string GetMessage()
		{
			return inputString;
		}

		public bool SelfHandShake()
		{
			return true;
			// TODO: create a dummy client and send something, report on success
			// remember to set inputstring = "" to initialization
		}



		private IPEndPoint GetLocalEndPoint()
		{
			// Establish the local endpoint for the socket.
			// The DNS name of the computer
			// running the listener is "host.contoso.com".
			IPAddress ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
			return new IPEndPoint(ipAddress, _port);
		}

		public void StartListening() {
			// Data buffer for incoming data.
			byte[] bytes = new Byte[1024];

			// get an endpoint and create a vacant TCP/IP socket
			IPEndPoint localEndPoint = GetLocalEndPoint();
			Socket listener = new Socket(
				AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );

			// Bind the socket to the local endpoint and listen for incoming connections.
			try {
				listener.Bind(localEndPoint);
				listener.Listen(100);

				while (true) {
					// Set the event to nonsignaled state.
					allDone.Reset();

					// Start an asynchronous socket to listen for connections.
					Debug.Log(" is waiting for a connection...");
					listener.BeginAccept( AcceptCallback, listener );

					// Wait until a connection is made before continuing.
					allDone.WaitOne();
				}

			} catch (Exception e) {
				Debug.LogError(e.ToString());
			}
		}

		public void AcceptCallback(IAsyncResult ar) {
			// Signal the main thread to continue.
			allDone.Set();

			Socket listener = (Socket) ar.AsyncState;
			Socket handler = listener.EndAccept(ar);

			StreamObject stream = new StreamObject(nBuffer, handler);
			handler.BeginReceive(stream.Buffer, 0, nBuffer, 0, ReadCallback, stream);
		}

		public void ReadCallback(IAsyncResult ar) {
			String content = String.Empty;

			StreamObject stream = (StreamObject) ar.AsyncState;
			Socket handler = stream.Socket;

			// Read data from the client socket.
			int bytesRead = handler.EndReceive(ar);

			if (bytesRead > 0) {
				// There  might be more data, so store the data received so far.
				stream.Append(bytesRead);

				// Check for end-of-file tag. If it is not there, read
				// more data.
				content = stream.ToString();
				if (content.IndexOf("<EOF>") > -1) {
					// got EOF, return what's in the string builder of the stream
					inputString += content;
					Debug.Log(String.Format("Read {0} bytes from socket. \n Data : {1}",
						content.Length, content));
					// Echo the data back to the client.
					Send(handler, content);
				} else {
					// otherwise, continue listening
					handler.BeginReceive(stream.Buffer, 0, nBuffer, 0, ReadCallback, stream);
				}
			}
		}

		private void Send(Socket handler, String data) {
			// Convert the string data to byte data using ASCII encoding.
			byte[] byteData = Encoding.ASCII.GetBytes(data);

			// Begin sending the data to the remote device.
			handler.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, handler);
		}

		private void SendCallback(IAsyncResult ar) {
			try {
				Socket handler = (Socket) ar.AsyncState;

				// Complete sending the data to the remote device.
				int bytesSent = handler.EndSend(ar);
				Console.WriteLine("Sent {0} bytes to client.", bytesSent);

				handler.Shutdown(SocketShutdown.Both);
				handler.Close();

			} catch (Exception e) {
				Debug.LogError(e.ToString());
			}
		}
	}
}