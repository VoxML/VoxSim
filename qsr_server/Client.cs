using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

public class Client : MonoBehaviour
{

    private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    private const int PORT = 8220;

    Vector3 pos1;
    Vector3 size1;
    Vector3 pos2;
    Vector3 size2;

    static string message;

    void Start()
    {
        ConnectToServer();
        Receive();
    }

    void Update()
    {
        GetPosSize();
        Send(); 
    }

    private void GetPosSize() 
    {
        try
        {
            pos1 = GameObject.Find("knife").GetComponent<Transform>().position;
            size1 = GameObject.Find("knife").GetComponent<Transform>().localScale;
            pos2 = GameObject.Find("cup").GetComponent<Transform>().position;
            size2 = GameObject.Find("cup").GetComponent<Transform>().localScale;
            Debug.Log(pos1);
            Debug.Log(pos2);
            message += "knife ";
            message += pos1.x + " ";
            message += pos1.y + " ";
            message += pos1.z + " ";
            message += size1.x + " ";
            message += size1.y + " ";
            message += size1.z + ",";

            message += "cup ";
            message += pos2.x + " ";
            message += pos2.y + " ";
            message += pos2.z + " ";
            message += size2.x + " ";
            message += size2.y + " ";
            message += size2.z + "\n";
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }


    private static void ConnectToServer()
    {
        int attempts = 0;

        while (!ClientSocket.Connected)
        {
            try
            {
                attempts++;
                Debug.Log("Connection attempt " + attempts);
                // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                ClientSocket.Connect(IPAddress.Loopback, PORT);
            }
            catch (SocketException)
            {
                Debug.Log("Socket exception"); 
            }
        }

        Debug.Log("Connected");
    }


    private static void Send()
    {
        Debug.Log("Send a request: the pos of knife and cup");
        Debug.Log(message); 

        SendString(message);
    }

    private static void SendString(string text)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(text);
        ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
    }

    private static void Receive()
    {
        var buffer = new byte[2048];
        int received = ClientSocket.Receive(buffer, SocketFlags.None);
        if (received == 0) return;
        var data = new byte[received];
        Array.Copy(buffer, data, received);
        string text = Encoding.ASCII.GetString(data);
        Console.WriteLine("received from server:" + text);
    }



}
