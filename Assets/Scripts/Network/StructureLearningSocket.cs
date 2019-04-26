using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Network {
	public class StructureLearningEventArgs : EventArgs {
		public string Content { get; set; }

		public StructureLearningEventArgs(string content, bool macroEvent = false) {
			this.Content = content;
		}
	}

	public class StructureLearningSocket : SocketConnection {
	}
}