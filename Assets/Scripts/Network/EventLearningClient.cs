using System;
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

	public class EventLearningClient : SocketClient
	{
		
	}
}