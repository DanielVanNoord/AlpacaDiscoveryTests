using System;
using System.Collections.Generic;
using System.Text;

namespace AlpacaDiscovery
{
    public static class Constants
    {
        public const string DiscoveryMessage = "alpacadiscovery";
        public const int DiscoveryPort = 32227;
        public const string ResponseString = "alpacaport";
        public const string MulticastGroup = "ff12::414c:5041:4341";

        public static byte[] Message
        {
            get
            {
                return Encoding.ASCII.GetBytes(DiscoveryMessage);
            }
        }
    }
}
