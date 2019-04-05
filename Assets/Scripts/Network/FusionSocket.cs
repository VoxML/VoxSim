using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using UnityEngine;

namespace Network
{
	public class FusionEventArgs : EventArgs {
		public string Content { get; set; }

		public FusionEventArgs(string content, bool macroEvent = false)
		{
			this.Content = content;
		}
	}

    public class FusionSocket : SocketConnection
	{
		public event EventHandler FusionReceived;

		public void OnFusionReceived(object sender, EventArgs e)
		{
			if (FusionReceived != null)
			{
				FusionReceived(this, e);
			}
		}

		protected override void Loop()
		{
            while (IsConnected())
			{
				NetworkStream stream = _client.GetStream();
				byte[] byteBuffer = new byte[IntSize];
                try
                {
                    stream.Read(byteBuffer, 0, IntSize);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
					break;
                }
				
				int len = BitConverter.ToInt32(byteBuffer, 0);
				

				byteBuffer = new byte[len];
				int numBytesRead = stream.Read(byteBuffer, 0, len);

				string message = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
				if (message.StartsWith ("P")) {
					if ((HowManyLeft() != 0) && (!_messages.Peek().StartsWith ("P"))) {
						_messages.Enqueue (message);
					}
				}
				else {
					_messages.Enqueue (message);
				}
			}

			Close();
		}

	}
}