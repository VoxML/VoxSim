using System;

using VoxSimPlatform.Network;

public class CommanderEventArgs : EventArgs {
	public string Content { get; set; }

	public CommanderEventArgs(string content, bool macroEvent = false) {
		this.Content = content;
	}
}

public class CommanderSocket : SocketConnection {
	public event EventHandler ConnectionLost;

	public void OnConnectionLost(object sender, EventArgs e) {
		if (ConnectionLost != null) {
			ConnectionLost(this, e);
		}
	}

	public void Write(string content) {
		// Check to see if this NetworkStream is writable.
//			if (_client.GetStream().CanWrite) {
//
//				byte[] writeBuffer = Encoding.ASCII.GetBytes (content);
//				_client.GetStream().Write (writeBuffer, 0, writeBuffer.Length);
//				Debug.Log (string.Format("Written to this NetworkStream: {0}",writeBuffer.Length));  
//			} 
//			else {
//				Debug.Log ("Sorry.  You cannot write to this NetworkStream.");  
//			}
	}
}