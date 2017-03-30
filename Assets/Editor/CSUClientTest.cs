using Network;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class CSUClientTest {

	[Test]
	public void ClientTest() {
		CSUClient client = new CSUClient();
		client.Connect("localhost", 8887);
		int i = 0;
		while (i < 20)
		{
			Debug.Log(client.GetMessage());
			System.Threading.Thread.Sleep(1000);
			Debug.Log(i++);
		}
		client.Close();
	}
}
