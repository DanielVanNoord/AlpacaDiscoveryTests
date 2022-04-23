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

        private readonly Dictionary<IPAddress, UdpClient> IPv4Clients = new Dictionary<IPAddress, UdpClient>();
        private readonly Dictionary<IPAddress, UdpClient> IPv6Clients = new Dictionary<IPAddress, UdpClient>();

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
        }

        /// <summary>
        /// Send out discovery message on each IPv4 broadcast address
        /// This dual targets NetStandard 2.0 and NetFX 3.5 so no Async Await
        /// Broadcasts on each adapters address as per Windows / Linux documentation
        /// </summary>
        private void SearchIPv4()
        {
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
                            {
                                continue;
                            }

                            if (uni.IPv4Mask == IPAddress.Parse("255.255.255.255"))
                            {
                                //No broadcast on single device endpoint
                                continue;
                            }

                            if (!IPv4Clients.ContainsKey(uni.Address))
                            {
                                IPv4Clients.Add(uni.Address, NewIPv4Client(uni.Address));
                            }

                            if (!IPv4Clients[uni.Address].Client.IsBound)
                            {
                                IPv4Clients.Remove(uni.Address);
                                continue;
                            }

                            // Local host addresses (127.*.*.*) may have a null mask in Net Framework. We do want to search these. The correct mask is 255.0.0.0.
                            IPv4Clients[uni.Address].Send(Constants.Message, Constants.Message.Length, new IPEndPoint(GetBroadcastAddress(uni.Address, uni.IPv4Mask ?? IPAddress.Parse("255.0.0.0")), Constants.DiscoveryPort));
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

                            //Only use LinkLocal or LocalHost addresses
                            if (!uni.Address.IsIPv6LinkLocal && uni.Address != IPAddress.Parse("::1"))
                                continue;

                            try
                            {
                                if (!IPv6Clients.ContainsKey(uni.Address))
                                {
                                    IPv6Clients.Add(uni.Address, NewIPv6Client(uni.Address));
                                }

                                if (!IPv6Clients[uni.Address].Client.IsBound)
                                {
                                    IPv6Clients.Remove(uni.Address);
                                    continue;
                                }

                                IPv6Clients[uni.Address].Send(Constants.Message, Constants.Message.Length, new IPEndPoint(IPAddress.Parse(Constants.MulticastGroup), Constants.DiscoveryPort));

                            }
                            catch (SocketException)
                            {
                            }
                        }
                    }
                }
            }
        }

        private UdpClient NewIPv4Client(IPAddress host)
        {
            var client = new UdpClient();

            client.EnableBroadcast = true;
            client.MulticastLoopback = false;

            if (PlatformDetection.IsWindows)
            {
                int SIO_UDP_CONNRESET = -1744830452;
                client.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }

            //0 tells OS to give us a free ephemeral port
            client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            client.BeginReceive(new AsyncCallback(ReceiveCallback), client);

            return client;
        }

        private UdpClient NewIPv6Client(IPAddress host)
        {
            var client = new UdpClient(AddressFamily.InterNetworkV6);

            //0 tells OS to give us a free ephemeral port
            client.Client.Bind(new IPEndPoint(host, 0));

            client.BeginReceive(new AsyncCallback(ReceiveCallback), client);

            return client;
        }

        /// <summary>
        /// Send out the IPv4 and IPv6 messages
        /// </summary>
        private void SendDiscoveryMessage(bool searchIPv4, bool searchIPv6)
        {
            if (searchIPv4) { SearchIPv4(); }

            if (searchIPv6) { SearchIPv6(); }
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
            UdpClient udpClient = null;
            try
            {
                udpClient = (UdpClient)ar.AsyncState;

                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Constants.DiscoveryPort);

                // Obtain the UDP message body and convert it to a string, with remote IP address attached as well
                string ReceiveString = Encoding.ASCII.GetString(udpClient.EndReceive(ar, ref endpoint));

                //Do not report your own transmissions
                if (ReceiveString.Contains(Constants.ResponseString))
                {
                    JObject obj = JObject.Parse(ReceiveString);

                    var ep = new IPEndPoint(endpoint.Address, (int)obj[Constants.ResponseString]);
                    if (!CachedEndpoints.Contains(ep))
                    {
                        CachedEndpoints.Add(ep);

                        callbackFunction?.Invoke(ep);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during callback: {ex.Message}");
            }
            finally
            {
                try
                {
                    // Configure the UdpClient class to accept more messages, if they arrive
                    udpClient?.BeginReceive(new AsyncCallback(ReceiveCallback), udpClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error restarting search: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Resends the search request for IPv4 and IPv6
        /// </summary>
        public void Search(bool IPv4 = true, bool IPv6 = true)
        {
            if (!IPv4 && !IPv6)
            {
                throw new ArgumentException("You must search on one or more protocol types.");
            }
            SendDiscoveryMessage(IPv4, IPv6);
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