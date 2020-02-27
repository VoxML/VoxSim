using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ZenFulcrum.EmbeddedBrowser;

using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;



namespace VoxSimPlatform {
    namespace Network {
        public class GetSocket {
            private static Socket ConnectSocket(string server, int port) {
                Socket s = null;
                IPHostEntry hostEntry = null;

                // Get host related information.
                hostEntry = Dns.GetHostEntry(server);

                // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
                // an exception that occurs when the host IP Address is not compatible with the address family
                // (typical in the IPv6 case).
                foreach (IPAddress address in hostEntry.AddressList) {
                    IPEndPoint ipe = new IPEndPoint(address, port);
                    Socket tempSocket =
                        new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);

                    if (tempSocket.Connected) {
                        s = tempSocket;
                        break;
                    }
                    else {
                        continue;
                    }
                }
                return s;
            }

            // This method requests the home page content for the specified server.
            private static Socket SocketSendReceive(string server, int port, Socket s = null, string request = null, bool receive = false) {
                if (s == null) {
                    s = ConnectSocket(server, port);
                }

                if (request != null) {
                    byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                    // Create a socket connection with the specified server and port
                    // Send request to the server.
                    int result = s.Send(bytesSent, bytesSent.Length, 0);
                    Debug.LogWarning(request + "   " + s.Handle + "    " + s.IsBound + "  result: " + result + "  connected: " + s.RemoteEndPoint);

                }
                else {
                    // Initialize the connection
                    string id = Random.Range(0, 999999).ToString(); // This allows us to re-register on a new port without, like, doing it manually.
                    string[] requests = { "(register :name DIANA" + id +")", // Particular number needs to change while facilitator is open
                    "(subscribe :content (request &key :content (record-gene-data . * )))",
                    "(subscribe :content (request &key :content (cluster-analysis . * )))",
                    "(tell :content (module-status ready))",
                    //"(subscribe :content (tell &key :content (utterance . *)))",
                    "(subscribe :content (tell &key :content (spoken . *)))"// New one as of 2/27/2020
                    //(tell :content (spoken :who sys :what "Sorry, I didn't catch that. Can you rephrase please?"))
                };

                    foreach (string requestLoop in requests) {
                        request = requestLoop;
                        byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                        byte[] bytesReceived = new byte[1024];

                        // Create a socket connection with the specified server and port.

                        // Send request to the server.
                        s.Send(bytesSent, bytesSent.Length, 0);
                    }
                }
                return s;
            }

            public static Socket Main(Socket s = null, string request = null, bool receive = false) {
                string host;
                int port;

                //if (args.Length == 0)
                //    // If no server name is passed as argument to this program, 
                //    // use the current host name as the default.
                //    host = Dns.GetHostName();
                //else
                //    host = args[0];
                //Debug.LogWarning(host + "    " + port);
                host = "localhost";
                port = 6200;
                return SocketSendReceive(host, port, s, request, receive); // where s gets passed
                                                                           //Debug.LogWarning(result);
            }
        }

    }
}