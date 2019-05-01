using System;

using VoxSimPlatform.Network;

public class StructureLearningEventArgs : EventArgs {
	public string Content { get; set; }

	public StructureLearningEventArgs(string content, bool macroEvent = false) {
		this.Content = content;
	}
}

public class StructureLearningSocket : SocketConnection {
}
