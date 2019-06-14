using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;

using VoxSimPlatform.Network;

public class FusionEventArgs : EventArgs {
	public string Content { get; set; }

	public FusionEventArgs(string content, bool macroEvent = false) {
		this.Content = content;
	}
}

public class FusionSocket : SocketConnection {
   	public event EventHandler FusionReceived;

	public void OnFusionReceived(object sender, EventArgs e) {
		if (FusionReceived != null) {
			FusionReceived(this, e);
		}
	}
     
    public FusionSocket() {
        IOClientType = typeof(FusionIOClient);
    }
           
	protected override void Loop() {
		while (IsConnected()) {
			NetworkStream stream = _client.GetStream();
			byte[] byteBuffer = new byte[IntSize];
			try {
				stream.Read(byteBuffer, 0, IntSize);
			}
			catch (Exception e) {
				Debug.LogError(e);
				Debug.LogError(e.Message);
			}

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
			if (message.StartsWith("P")) {
				if ((HowManyLeft() == 0) || (!_messages.Peek().StartsWith("P"))) {
					_messages.Enqueue(message);
				}
			}
			else {
				_messages.Enqueue(message);
			}

			//_messages.Enqueue (message);
//				Debug.Log (stream.DataAvailable);
		}

		//_client.Close();
	}
}