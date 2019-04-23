using Network;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class FusionSocketTest {

	[Test]
	public void SocketTest() {
        FusionSocket socket = new FusionSocket();
		socket.Connect("localhost", 8887);
		int i = 0;
		while (i < 20)
		{
			Debug.Log(socket.GetMessage());
			System.Threading.Thread.Sleep(1000);
			Debug.Log(i++);
		}
		socket.Close();
	}
}
