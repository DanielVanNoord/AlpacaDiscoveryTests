// (c) 2019 Daniel Van Noord
// This code is licensed under MIT license (see License.txt for details)

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

//This namespace dual targets NetStandard2.0 and Net35, thus no async await
namespace AlpacaDiscovery
{
    public class Server
    {
        private readonly int port;

        public Server(int AlpacaPort, bool IPv4=true, bool IPv6=true)
        {
            port = AlpacaPort;

            if (!IPv4 && !IPv6)
            {
                throw new ArgumentException("You must search on one or more protocol types.");
            }

            if (IPv4)
            {
                InitIPv4();
            }

            if (IPv6)
            {
                InitIPv6();
            }
        }

        /// <summary>
        /// Create and listen on an IPv4 broadcast port
        /// </summary>
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

        /// <summary>
        /// Bind a UDP client to each network adapter and set the index and address for multicast
        /// </summary>
        private void InitIPv6()
        {
            // Windows needs to have the IP Address and index set for an IPv6 multicast socket
            if (PlatformDetection.IsWindows)
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                List<UdpClient> clients = new List<UdpClient>();

                foreach (var adapter in adapters)
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up)
                        continue;

                    if (adapter.Supports(NetworkInterfaceComponent.IPv6) && adapter.SupportsMulticast)
                    {
                        IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                        if (adapterProperties == null)
                            continue;

                        UnicastIPAddressInformationCollection uniCast = adapterProperties.UnicastAddresses;

                        if (uniCast.Count > 0)
                        {
                            foreach (UnicastIPAddressInformation uni in uniCast)
                            {
                                if (uni.Address.AddressFamily != AddressFamily.InterNetworkV6)
                                    continue;

                                clients.Add(NewClient(uni.Address, adapterProperties.GetIPv6Properties().Index));
                            }
                        }
                    }
                }
            }
            else
            {
                //Linux does not
                UdpClient client = NewClient(IPAddress.IPv6Any, 0);
            }         
        }

        private UdpClient NewClient(IPAddress host, int index)
        {
            UdpClient UDPClientV6 = new UdpClient(AddressFamily.InterNetworkV6);

            UDPClientV6.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            UDPClientV6.ExclusiveAddressUse = false;

            UDPClientV6.Client.Bind(new IPEndPoint(host, Constants.DiscoveryPort));

            UDPClientV6.JoinMulticastGroup(index, IPAddress.Parse(Constants.MulticastGroup));
            // This uses begin receive rather then async so it works on net 3.5
            UDPClientV6.BeginReceive(ReceiveCallback, UDPClientV6);

            return UDPClientV6;
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