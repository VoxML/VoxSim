using System;

using VoxSimPlatform.Network;

public class EventLearningEventArgs : EventArgs {
	public string Content { get; set; }

	public EventLearningEventArgs(string content, bool macroEvent = false) {
		this.Content = content;
	}
}

public class EventLearningSocket : SocketConnection {
}