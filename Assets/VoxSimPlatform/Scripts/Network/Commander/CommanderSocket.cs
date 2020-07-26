using UnityEngine;
using System;
using System.Text;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Network {
        namespace Commander {
            public class CommanderEventArgs : EventArgs {
                public string Content { get; set; }

                public CommanderEventArgs(string content, bool macroEvent = false) {
                    this.Content = content;
                }
            }

            public class CommanderSocket : SocketConnection {

                public EventHandler UpdateReceived;

                public void OnUpdateReceived(object sender, EventArgs e) {
                    if (UpdateReceived != null) {
                        UpdateReceived(this, e);
                    }
                }

                public CommanderSocket() {
                    IOClientType = typeof(CommanderIOClient);
                }

                public void Write(byte[] content) {
                    // Check to see if this NetworkStream is writable.
                    if (_client.GetStream().CanWrite) {
                        byte[] writeBuffer = content;
                        if (!BitConverter.IsLittleEndian) {
                            Array.Reverse(writeBuffer);
                        }

                        _client.GetStream().Write(writeBuffer, 0, writeBuffer.Length);
                        Debug.Log(string.Format("Written to this NetworkStream: {0} ({1})", writeBuffer.Length,
                            GlobalHelper.PrintByteArray(writeBuffer)));
                    }
                    else {
                        Debug.Log("Sorry.  You cannot write to this NetworkStream.");
                    }
                }
            }
        }
    }
}