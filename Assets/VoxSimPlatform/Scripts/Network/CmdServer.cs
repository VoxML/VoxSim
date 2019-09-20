using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace VoxSimPlatform {
    namespace Network {
    	public class CmdServer : NonBlockingTcpServer {
    		private StringBuilder _sb;

    		private Queue<string> _messages;

    		// why does \n not work?
    		private const char MessageDelimiter = '\0';

    		public CmdServer(bool localhost, int port, int clientLimit)
    			: base(localhost, port, clientLimit) {
    			_messages = new Queue<string>();
    		}

    		public override void Process() {
    			while (true) {
    				// make sure we have a socket connected
    				Socket clientSocket = _listener.AcceptSocket();
    				if (!clientSocket.Connected) continue;
    				// then check if the stream is readable
    				NetworkStream stream = new NetworkStream(clientSocket);
    				if (!stream.CanRead) continue;

    				byte[] okResponse = Encoding.ASCII.GetBytes("OK");

    				byte[] byteBuffer = new byte[128];
    				_sb = new StringBuilder();

    				try {
    					do {
    						int numBytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length);
    						string gotten = Encoding.ASCII.GetString(byteBuffer, 0, numBytesRead);
    						string[] lines = gotten.Split(MessageDelimiter);
    						if (lines.Length == 0) continue;

    						// take chunks until the second to last
    						for (int i = 0; i < lines.Length - 1; i++) {
    							_sb.Append(lines[i]);
    							_messages.Enqueue(_sb.ToString());
    							// TODO 3/29/17-12:38 do we need to send something back?
    //                        stream.Write(okResponse, 0, okResponse.Length);
    							_sb = new StringBuilder();
    						}

    						// leave the last piece to concatenate with the following chunks
    						_sb.Append(lines[lines.Length - 1]);
    //					} while (stream.DataAvailable);
    					} while (true);
    				}
    				finally {
    					clientSocket.Close();
    					stream.Dispose();
    				}
    			}
    		}

    		public string GetMessage() {
    			return _messages.Count > 0 ? _messages.Dequeue() : "";
    		}

    		public string GetMessageOld() {
    			if (_sb == null) return "";
    			string message = _sb.ToString();
    			_sb = new StringBuilder();
    			return message;
    		}
    	}
    }
}