#if !UNITY_WEBGL
using UnityEngine;
using System;

using VoxSimPlatform.Global;
using VoxSimPlatform.Network;


namespace VoxSimPlatform
{
	namespace Examples
	{
		namespace SocketConnections
		{
			public class RelationExtractorEventArgs : EventArgs
			{
				public string Content { get; set; }

				public RelationExtractorEventArgs(string content, bool macroEvent = false)
				{
					this.Content = content;
				}
			}

			public class RelationExtractorSocket : SocketConnection
			{

				public EventHandler UpdateReceived;

				public void OnUpdateReceived(object sender, EventArgs e)
				{
					if (UpdateReceived != null)
					{
						UpdateReceived(this, e);
					}
				}

				public RelationExtractorSocket()
				{
					IOClientType = typeof(IOClients.RelationExtractorIOClient);
				}

				public void Write(byte[] content)
				{
					// Check to see if this NetworkStream is writable.
					if (_client.GetStream().CanWrite)
					{
						byte[] writeBuffer = content;
						if (!BitConverter.IsLittleEndian)
						{
							Array.Reverse(writeBuffer);
						}

						_client.GetStream().Write(writeBuffer, 0, writeBuffer.Length);
						Debug.Log(string.Format("Written to this NetworkStream: {0} ({1})", writeBuffer.Length,
							GlobalHelper.PrintByteArray(writeBuffer)));
					}
					else
					{
						Debug.Log("Sorry.  You cannot write to this NetworkStream.");
					}
				}
			}
		}
	}
} 
#endif