using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class StringEvent : UnityEvent<string> { };

public class DiscoveryClient : MonoBehaviour
{
    /// <summary>
    /// Network thread.
    /// </summary>
    /// <remarks>
    /// All network operations will be executed in a separate
    /// thread to avoid blocking Unity application.
    /// This thread is closed once a server is found.
    /// </remarks>
    Thread Thread;

    /// <summary>
    /// UDPClient sending and receiving messages over UDP.
    /// </summary>
    UdpClient UdpClient;

    /// <summary>
    /// String containing the host's IP once it has been found.
    /// </summary>
    /// <remarks>
    /// This string should remain empty and will be replaced by the host's IP
    /// once a host is found.
    /// </remarks>
    string HostIP = "";


    [SerializeField]
    /// <summary>
    /// Events triggered when a host has been found.
    /// Callbacks are called with the host's IP address as parameter.
    /// </summary>
    /// <remarks>
    /// This event is used to return host's IP address.
    /// </remarks>
    StringEvent OnHostFound;

    [SerializeField]
    /// <summary>
    /// UDP port to be used for discovery.
    /// </summary>
    /// <remarks>
    /// This value should always be identical to the host's in order to
    /// establish a communication.
    /// </remarks>
    int Port = 38800;

    [SerializeField]
    /// <summary>
    /// Time interval beetween two broadcast messages. A broadcast message
    /// (message sent to all devices in the local network) will be sent every
    /// `BroadcastInterval` seconds.
    /// </summary>
    float BroadcastInterval = 3.0f;


    /// <summary>
    /// Start Network Discovery.
    /// </summary>
    public void FindHost()
    {
        Thread = new Thread(new ThreadStart(NetworkDiscovery));
        Thread.IsBackground = true;

        Thread.Start();
    }

    /// <summary>
    /// Get a list of IP addresses corresponding to the device currently
    /// running this script.
    /// </summary>
    /// <returns>
    /// Array of structs IPAddress containing all IP addresses held by
    /// this device.
    /// </returns>
    public static IPAddress[] GetSelfIPs()
    {
        string HostName = Dns.GetHostName();
        IPAddress[] IPs = Dns.GetHostAddresses(HostName);
        return IPs;
    }

    /// <summary>
    /// Sends broadcast messages and wait for a response.
    /// </summary>
    /// <remarks>
    /// Sends a broadcast message every BroadcastInterval seconds.
    /// Once a response is received, HostIP is filled and the function ends.
    /// </remarks>
    void NetworkDiscovery()
    {
        // Initialize the UdpClient
        UdpClient = new UdpClient(Port);
        UdpClient.Client.ReceiveTimeout = Convert.ToInt32(BroadcastInterval * 1000);

        // Broadcast address to send discovery messages
        IPEndPoint BroadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, Port);

        // This EndPoint object will be filled with Host's info when a response
        // is received
        IPEndPoint HostEndPoint = new IPEndPoint(IPAddress.Any, Port);

        // Get self IPs in order to ignore our own messages (coming from our IPs).
        IPAddress[] SelfIPs = GetSelfIPs();

        byte[] msg = Encoding.UTF8.GetBytes("Discovery Message");

        while (true)
        {
            // Send discovery message
            UdpClient.Send(msg, msg.Length, BroadcastEndPoint);

            try
            {
                byte[] bytes = UdpClient.Receive(ref HostEndPoint);
            }
            catch (SocketException)
            {
                // UdpClient.Receive() timed out
                // => no response has been received
                continue;
            }

            // Check if endpoint's IP is one of ours (if we received our own
            // message)
            if (!SelfIPs.Where(ip => ip.Equals(HostEndPoint.Address)).Any())
            {
                HostIP = HostEndPoint.Address.ToString();
                return;
            }
        }
    }

    // As it is not possible to invoke OnHostFound callbacks from a thread,
    // we set HostIP and invoke the callback list from Update()
    // (from the main thread)
    void Update()
    {
        if (!(HostIP.Length < 1))
        {
            // Abort the thread if it has not finished properly
            if (Thread.IsAlive)
            {
                Thread.Abort();
            }
            UdpClient.Close();
            OnHostFound.Invoke(HostIP);
        }
    }
}