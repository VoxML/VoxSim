using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace VoxSimPlatform {
    namespace Network {
    	public abstract class NonBlockingTcpServer {
    		protected static TcpListener _listener;

    		private readonly bool _localhost;
    		private readonly int _port;
    		private List<Thread> threads;


    		public NonBlockingTcpServer(bool localhost, int port, int clientLimit) {
    			_localhost = localhost;
    			_port = port;
    			_listener = new TcpListener(GetLocalIpAddress(), _port);
    			_listener.Start();
    			Debug.Log("Server mounted, listening to port " + _port);

    			threads = new List<Thread>();
    			for (int i = 0; i < clientLimit; i++) {
    				Thread t = new Thread(Process);
    				threads.Add(t);
    				t.Start();
    			}
    		}

    		public void Close() {
    			foreach (Thread thread in threads) {
    				thread.Abort();
    			}

    			_listener.Stop();
    		}

    		private IPAddress GetLocalIpAddress() {
    			IPAddress ip = null;
    #if !UNITY_IOS
    			string hostName = _localhost ? "localhost" : Dns.GetHostName();
    			foreach (IPAddress ipAddress in Dns.GetHostEntry(hostName).AddressList) {
    				if (ipAddress.AddressFamily.ToString() == "InterNetwork") {
    					Debug.Log(ipAddress);
    					ip = ipAddress;
    				}
    			}
    #else
    			foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
    			{
    				if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
    				{
    					//Debug.Log(ni.Name);
    					foreach (UnicastIPAddressInformation ipInfo in ni.GetIPProperties().UnicastAddresses)
    					{
    						if (ipInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    						{
    							Debug.Log (ipInfo.Address.ToString());
    							ip = ipInfo.Address;
    						}
    					}
    				}  
    			}
    #endif
    			Debug.Log(ip);
    			return ip;
    			throw new NetworkInformationException();
    		}

    		// Method that does actual work with incoming stream
    		public abstract void Process();
    	}
    }
}