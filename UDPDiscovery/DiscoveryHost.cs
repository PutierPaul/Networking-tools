using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


[Serializable]
public class StringEvent : UnityEvent<string> { }

public class DiscoveryHost : MonoBehaviour
{
    /// <summary>
    /// Network thread.
    /// </summary>
    /// <remarks>
    /// All network operations will be executed in a separate
    /// thread to avoid blocking Unity application.
    /// This thread is closed inside StopDiscoveryListen().
    /// </remarks>
    Thread Thread;

    /// <summary>
    /// UDPClient sending and listening for messages over UDP.
    /// </summary>
    UdpClient UdpClient;

    /// <summary>
    /// UDP port to be used for discovery.
    /// </summary>
    /// <remarks>
    /// This value should always be identical to the client's in order to
    /// establish a communication.
    /// </remarks>
    [SerializeField]
    int Port = 38800;

    /// <summary>
    /// 
    /// </summary>
    [SerializeField]
    bool LaunchAtGameObjectStart = true;

    private void Start()
    {
        if (LaunchAtGameObjectStart)
        {
            StartHost();
        }
    }

    /// <summary>
    /// Start Network discovery.
    /// </summary>
    void StartHost()
    {
        Thread = new Thread(new ThreadStart(StartDiscoveryListen));
        Thread.IsBackground = true;

        Thread.Start();
    }

    /// <summary>
    /// Listen for client's discovery messages and send a response.
    /// </summary>
    /// <remarks>
    /// This function will loop until the thread is aborted.
    /// This allows to send responses to multiple clients until one
    /// of them connects.
    /// </remarks>
    void StartDiscoveryListen()
    {
        UdpClient = new UdpClient(Port);

        while (true)
        {
            // Receive client discovery message
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, Port);
            byte[] BroadcastBytes = UdpClient.Receive(ref endPoint);

            // Send discovery response. this message will only be
            // used by the client to get host's IP.
            byte[] toSend = Encoding.UTF8.GetBytes("Host ready.");
            UdpClient.Send(toSend, toSend.Length, endPoint);
        }
    }

    /// <summary>
    /// Stop listening for discovery messages.
    /// </summary>
    /// <remarks>
    /// Call this method once the connection with a client is established.
    /// </remarks>
    public void StopDiscoveryListen()
    {
        Thread.Abort();
        UdpClient.Close();
    }
}
