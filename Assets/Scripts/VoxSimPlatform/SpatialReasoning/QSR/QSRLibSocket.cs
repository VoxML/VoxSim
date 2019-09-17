using UnityEngine;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;

using VoxSimPlatform.Global;
using VoxSimPlatform.Network;

namespace VoxSimPlatform {
    namespace SpatialReasoning {
        namespace QSR {
            public class QSRLibEventArgs : EventArgs {
                public string Content { get; set; }

                public QSRLibEventArgs(string content) {
                    this.Content = content;
                }
            }

            public class QSRLibSocket : SocketConnection {
                public event EventHandler QSRReceived;

                public void OnQSRReceived(object sender, EventArgs e) {
                    if (QSRReceived != null) {
                        QSRReceived(this, e);
                    }
                }
                 
                public QSRLibSocket() {
                    IOClientType = typeof(QSRLibIOClient);
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
                            Helper.PrintByteArray(writeBuffer)));
                    }
                    else {
                        Debug.Log("Sorry.  You cannot write to this NetworkStream.");
                    }
                }

                public void SendQSRRequest(string input) {
                    string qsrType = input.Split(':')[1];   // rcc8:block2,block3
                    string[] objNames = input.Split(':')[2].Split(',');
                    Debug.Log(string.Format("QSR Input: {0}", string.Join(",", objNames)));
                    string worldTraceMsg = GetPosSize(objNames) + "\n" + GetPosSize(objNames);
                    Debug.Log(string.Format("QSR World Trace message: {0}", worldTraceMsg));
                    string messageToSend = qsrType + ":" + worldTraceMsg;
                    Debug.Log(string.Format("Message to send: {0}", messageToSend));

                    byte[] bytes = BitConverter.GetBytes(messageToSend.Length).Concat(Encoding.ASCII.GetBytes(messageToSend)).ToArray<byte>();
                    Write(bytes);
                }

                //private static readonly Socket ClientSocket = new Socket
                //        (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //private const int PORT = 8220;
                //private int frames = 0; 

                //Vector3 pos;
                //Vector3 size;
                //Vector3 pos1;
                //Vector3 size1;
                //Vector3 pos2;
                //Vector3 size2;
                //Vector3 pos3;
                //Vector3 size3;
                //string[] objs; 

                // needs padding because of the ...
                //static string msg = "aaaa";

                //void Start() {
                //    ConnectToServer();
                //    objs = ReceiveCommand().Split(',');
                //    GetPosSize(objs); 
                //}

                //void Update() {
                //    if (ClientSocket.Connected)
                //    {        
                //        GetPosSize(objs);
                //        Send();
                //        Receive();
                //    }

                //    //frames++;
                //    //if (frames >= 10 && frames % 10 == 0)
                //    //{
                //    //    if (ClientSocket.Connected)
                //    //    {
                //    //        GetPosSize(objs);
                //    //        Send();
                //    //        Receive();
                //    //    }
                //    //}
                //}

                private string GetPosSize(string[] objNames) {
                    string posSize = string.Empty;
                    Vector3 pos = Vector3.zero;
                    Vector3 size = Vector3.zero;
                    try {
                        int len = objNames.Length;
                        int i = 0; 
                        foreach(string obj in objNames) {
                            i++; 
                            //Debug.Log(obj); 
                            posSize += obj + " ";
                            pos = GameObject.Find(obj).GetComponent<Transform>().position;
                            size = GameObject.Find(obj).GetComponent<Transform>().localScale;
                            posSize += pos.x + " ";
                            posSize += pos.y + " ";
                            posSize += pos.z + " ";
                            posSize += size.x + " ";
                            posSize += size.y + " ";
                            if (i == len) {
                                posSize += size.z + "\n";
                            }
                            else {
                                posSize += size.z + ",";
                            }
                        }
                    }
                    catch (Exception ex) {
                        Debug.Log(ex.Message);
                    }

                    return posSize;
                }

                //private static void ConnectToServer() {
                //    int attempts = 0;

                //    while (!ClientSocket.Connected) {
                //        try {
                //            attempts++;
                //            Debug.Log("Connection attempt " + attempts);
                //            // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                //            ClientSocket.Connect(IPAddress.Loopback, PORT);
                //        }
                //        catch (SocketException) {
                //            Debug.Log("Socket exception"); 
                //        }
                //    }

                //    Debug.Log("Connected");
                //}

                //private static void Send() {
                //    //Debug.Log("below is pos message sent:");
                //    //Debug.Log(message); 
                //    SendString(msg);
                //}

                //private static void SendString(string text) {
                //    byte[] buffer = Encoding.ASCII.GetBytes(text);
                //    ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                //}

                //private static void Receive() {
                //    var buffer = new byte[2048];
                //    int received = ClientSocket.Receive(buffer, SocketFlags.None); 
                //    if (received == 0) return;
                //    var data = new byte[received];
                //    Array.Copy(buffer, data, received);
                //    string text = Encoding.UTF8.GetString(data);
                //    Debug.Log("received from server: " + text);
                //}

                //private static string ReceiveCommand() {
                //    var buffer = new byte[2048];
                //    int received = ClientSocket.Receive(buffer, SocketFlags.None);
                //    if (received == 0) return "";
                //    var data = new byte[received];
                //    Array.Copy(buffer, data, received);
                //    string text = Encoding.UTF8.GetString(data);
                //    return text; 
                //}


                //private static void PrintByteArray(byte[] bytes)
                //{
                //    var sb = new StringBuilder("new byte[] { ");
                //    foreach (var b in bytes)
                //    {
                //        sb.Append(b + ", ");
                //    }
                //    sb.Append("}");
                //    Debug.Log(sb.ToString());
                //}
            }
        }
    }
}
