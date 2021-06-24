using System;
using System.Collections.Generic;
using System.Text;

namespace AlpacaDiscovery
{
    public static class Constants
    {
        public const string DiscoveryMessage = "alpacadiscovery1";
        public const int DiscoveryPort = 32227;
        public const string ResponseString = "AlpacaPort";
        public const string MulticastGroup = "ff12::00a1:9aca";

        public static byte[] Message
        {
            get
            {
                return Encoding.ASCII.GetBytes(DiscoveryMessage);
            }
        }
    }
}
