using UnityEngine;
using System.Threading;

using NUnit.Framework;
using VoxSimPlatform.Network;

public class FusionSocketTest {
	[Test]
	public void SocketTest() {
		FusionSocket socket = new FusionSocket();
		socket.Connect("localhost", 8887);
		int i = 0;
		while (i < 20) {
			Debug.Log(socket.GetMessage());
			Thread.Sleep(1000);
			Debug.Log(i++);
		}

		socket.Close();
	}
}