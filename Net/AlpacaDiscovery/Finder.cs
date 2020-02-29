// (c) 2019 Daniel Van Noord
// This code is licensed under MIT license (see License.txt for details)

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

//This namespace dual targets NetStandard2.0 and Net35, thus no async await
namespace AlpacaDiscovery
{
    public class Finder
    {
        private readonly Action<IPEndPoint> callbackFunction;

        /// <summary>
        /// A cache of all endpoints found by the server
        /// </summary>
        public List<IPEndPoint> CachedEndpoints
        {
            get;
        } = new List<IPEndPoint>();

        /// <summary>
        /// Creates a Alpaca Finder object that sends out a search request for Alpaca devices
        /// The results will only be in the cache
        /// This may require firewall access
        /// </summary>
        public Finder() : this(null)
        {
        }

        /// <summary>
        /// Creates a Alpaca Finder object that sends out a search request for Alpaca devices
        /// The results will be sent to the callback and stored in the cache
        /// Calling search and concatenating the results reduces the chance that a UDP packet is lost
        /// This may require firewall access
        /// This dual targets NetStandard 2.0 and NetFX 3.5 so no Async Await
        /// </summary>
        /// <param name="callback">A callback function to receive the endpoint result</param>
        public Finder(Action<IPEndPoint> callback)
        {
            callbackFunction = callback;

            // Try to send the discovery request message
            SendDiscoveryMessage();
        }

        /// <summary>
        /// Send out discovery message on each IPv4 broadcast address
        /// This dual targets NetStandard 2.0 and NetFX 3.5 so no Async Await
        /// Broadcasts on each adapters address as per Windows / Linux documentation 
        /// </summary>
        private void SearchIPv4()
        {
            var IPv4Client = new UdpClient();

            IPv4Client.EnableBroadcast = true;
            IPv4Client.MulticastLoopback = false;

            //0 tells OS to give us a free ethereal port
            IPv4Client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            IPv4Client.BeginReceive(ReceiveCallback, IPv4Client);

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                //Do not try and use non-operational adapters
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                    if (adapterProperties == null)
                        continue;
                    UnicastIPAddressInformationCollection uniCast = adapterProperties.UnicastAddresses;
                    if (uniCast.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation uni in uniCast)
                        {
                            if (uni.Address.AddressFamily != AddressFamily.InterNetwork)
                                continue;

                            // Local host addresses (127.*.*.*) may have a null mask in Net Framework. We do want to search these. The correct mask is 255.0.0.0.
                            IPv4Client.Send(Constants.Message, Constants.Message.Length, new IPEndPoint(GetBroadcastAddress(uni.Address, uni.IPv4Mask ?? IPAddress.Parse("255.0.0.0")), Constants.DiscoveryPort));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Send out discovery message on the IPv6 multicast group
        /// This dual targets NetStandard 2.0 and NetFX 3.5 so no Async Await
        /// </summary>
        private void SearchIPv6()
        {
            // Windows needs to bind a socket to each adapter explicitly
            if (PlatformDetection.IsWindows)
            {
                List<UdpClient> clients = new List<UdpClient>();

                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
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

                                try
                                {
                                    clients.Add(NewClient(uni.Address, 0));
                                }
                                catch (SocketException)
                                {

                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Linux seems to handle this correctly
                var client = NewClient(IPAddress.IPv6Any, 0);
            }
        }

        private UdpClient NewClient(IPAddress host, int index)
        {
            var client = new UdpClient(AddressFamily.InterNetworkV6);

            //0 tells OS to give us a free ethereal port
            client.Client.Bind(new IPEndPoint(host, index));

            client.BeginReceive(ReceiveCallback, client);

            client.Send(Constants.Message, Constants.Message.Length, new IPEndPoint(IPAddress.Parse(Constants.MulticastGroup), Constants.DiscoveryPort));

            return client;
        }

        /// <summary>
        /// Send out the IPv4 and IPv6 messages
        /// </summary>
        private void SendDiscoveryMessage()
        {
            SearchIPv4();
            SearchIPv6();        
        }

        // This turns the unicast address and the subnet into the broadcast address for that range
        // http://blogs.msdn.com/b/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx
        private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        // This dual targets NetStandard 2.0 and NetFX 3.5 so no Async Await
        // This callback is shared between IPv4 and IPv6
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                UdpClient udpClient = (UdpClient)ar.AsyncState;

                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Constants.DiscoveryPort);

                // Obtain the UDP message body and convert it to a string, with remote IP address attached as well
                string ReceiveString = Encoding.ASCII.GetString(udpClient.EndReceive(ar, ref endpoint));

                // Configure the UdpClient class to accept more messages, if they arrive
                udpClient.BeginReceive(ReceiveCallback, udpClient);

                //Do not report your own transmissions
                if (ReceiveString.Contains(Constants.ResponseString))
                {
                    JObject obj = JObject.Parse(ReceiveString);

                    var ep = new IPEndPoint(endpoint.Address, (int)obj[Constants.ResponseString]);
                    if (!CachedEndpoints.Contains(ep))
                    {
                        CachedEndpoints.Add(ep);
                    }

                    callbackFunction?.Invoke(ep);
                }
            }
            catch(Exception ex)
            {
                //Logging goes here
            }
        }

        /// <summary>
        /// Resends the search request
        /// </summary>
        public void Search()
        {
            SendDiscoveryMessage();
        }

        /// <summary>
        /// Clears the cached IP Endpoints in CachedEndpoints
        /// </summary>
        public void ClearCache()
        {
            CachedEndpoints.Clear();
        }
    }
}