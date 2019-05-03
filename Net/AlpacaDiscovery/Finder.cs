// (c) 2019 Daniel Van Noord
// This code is licensed under MIT license (see License.txt for details)

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
        private readonly UdpClient cachedClient;

        /// <summary>
        /// A cache of all endpoints found by the server
        /// </summary>
        public List<IPEndPoint> CachedEndpoints
        {
            get;
        } = new List<IPEndPoint>();

        /// <summary>
        /// Creates a Alpaca Finder object that sends out a search request for Alpaca devices
        /// The results will be sent to the callback and stored in the cache
        /// Calling search and concatenating the results reduces the chance that a UDP packet is lost
        /// This may require firewall access
        /// </summary>
        /// <param name="callback">A callback function to receive the endpoint result</param>
        public Finder(Action<IPEndPoint> callback)
        {
            callbackFunction = callback;

            cachedClient = new UdpClient();

            cachedClient.EnableBroadcast = true;
            cachedClient.MulticastLoopback = false;

            //0 tells OS to give us a free ethereal port
            cachedClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            cachedClient.BeginReceive(ReceiveCallback, cachedClient);

            // Try to send the discovery request message
            SendDiscoveryMessage(Encoding.ASCII.GetBytes(Constants.DiscoveryMessage));
        }

        /// <summary>
        /// Creates a Alpaca Finder object that sends out a search request for Alpaca devices
        /// The results will only be in the cache
        /// This may require firewall access
        /// </summary>
        public Finder()
        {
            cachedClient = new UdpClient();
            IPEndPoint BindEP = new IPEndPoint(IPAddress.Any, 0);

            cachedClient.EnableBroadcast = true;
            cachedClient.MulticastLoopback = false;

            cachedClient.Client.Bind(BindEP);

            cachedClient.BeginReceive(ReceiveCallback, cachedClient);

            // Try to send the discovery request message
            SendDiscoveryMessage(Encoding.ASCII.GetBytes(Constants.DiscoveryMessage));
        }

        /*
         * On my test systems I discovered that some computer network adapters / networking gear will not forward 255.255.255.255 broadcasts. 
         * This binds to each network adapter on the computer, determines if it will work and then
         * Sends an IP and Subnet correct broadcast for each address combination on that adapter
         * This may result in some addresses being duplicated. For example if there are multiple addresses assigned to the same
         * Server this will find them all.
         * Also ncap style loopbacks may duplicate ip address running on local host
         */
        private void SendDiscoveryMessage(byte[] message)
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                // Currently this only works for IPv4, skip any adapters that do not support it.
                if (!adapter.Supports(NetworkInterfaceComponent.IPv4))
                    continue;
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                if (adapterProperties == null)
                    continue;
                UnicastIPAddressInformationCollection uniCast = adapterProperties.UnicastAddresses;
                if (uniCast.Count > 0)
                {
                    foreach (UnicastIPAddressInformation uni in uniCast)
                    {
                        // Currently this only works for IPv4.
                        if (uni.Address.AddressFamily != AddressFamily.InterNetwork) 
                            continue;

                        // Local host addresses (127.*.*.*) may have a null mask in Net Framework. We do want to search these. The correct mask is 255.0.0.0.
                        cachedClient.Send(message, 
                                       message.Length, 
                                       new IPEndPoint(GetBroadcastAddress(uni.Address, uni.IPv4Mask ?? IPAddress.Parse("255.0.0.0")), Constants.DiscoveryPort)
                                       );
                    }
                }
            }
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

        private void ReceiveCallback(IAsyncResult ar)
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
                if (int.TryParse(ReceiveString.Split(':')[1], out int result))
                {
                    var ep = new IPEndPoint(endpoint.Address, result);
                    if (!CachedEndpoints.Contains(ep))
                    {
                        CachedEndpoints.Add(ep);
                    }

                    callbackFunction?.Invoke(ep);
                }
            }
        }

        /// <summary>
        /// Resends the search request
        /// </summary>
        public void Search()
        {
            SendDiscoveryMessage(Encoding.ASCII.GetBytes(Constants.DiscoveryMessage));
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