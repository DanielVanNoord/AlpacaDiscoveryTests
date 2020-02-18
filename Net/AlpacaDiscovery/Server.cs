// (c) 2019 Daniel Van Noord
// This code is licensed under MIT license (see License.txt for details)

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

//This namespace dual targets NetStandard2.0 and Net35, thus no async await
namespace AlpacaDiscovery
{
    public class Server
    {
        private readonly int port;

        public Server(int AlpacaPort)
        {
            port = AlpacaPort;
            
            InitIPv4();
            InitIPv6();
        }

        private void InitIPv4()
        {
            UdpClient UDPClient = new UdpClient();

            UDPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            UDPClient.EnableBroadcast = true;
            UDPClient.MulticastLoopback = false;
            UDPClient.ExclusiveAddressUse = false;

            UDPClient.Client.Bind(new IPEndPoint(IPAddress.Any, Constants.DiscoveryPort));

            // This uses begin receive rather then async so it works on net 3.5
            UDPClient.BeginReceive(ReceiveCallback, UDPClient);
        }

        private void InitIPv6()
        {
            UdpClient UDPClientV6 = new UdpClient(AddressFamily.InterNetworkV6);

            UDPClientV6.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            UDPClientV6.ExclusiveAddressUse = false;

            UDPClientV6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, Constants.DiscoveryPort));

            UDPClientV6.JoinMulticastGroup(IPAddress.Parse(Constants.MulticastGroup));

            // This uses begin receive rather then async so it works on net 3.5
            UDPClientV6.BeginReceive(ReceiveCallback, UDPClientV6);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient udpClient = (UdpClient)ar.AsyncState;

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Constants.DiscoveryPort);

            // Obtain the UDP message body and convert it to a string, with remote IP address attached as well
            string ReceiveString = Encoding.ASCII.GetString(udpClient.EndReceive(ar, ref endpoint));

            if (ReceiveString.Contains(Constants.DiscoveryMessage))//Contains rather then equals because of invisible padding garbage
            {
                //For testing only
                Console.WriteLine(string.Format("Received a discovery packet from {0} at {1}", endpoint.Address, DateTime.Now));

                byte[] response = Encoding.ASCII.GetBytes(string.Format("{{\"alpacaport\": {0}}}", port));

                udpClient.Send(response, response.Length, endpoint);
            }

            // Configure the UdpClient class to accept more messages, if they arrive
            udpClient.BeginReceive(ReceiveCallback, udpClient);
        }
    }
}