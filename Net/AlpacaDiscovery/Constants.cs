﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AlpacaDiscovery
{
    public static class Constants
    {
        public const string DiscoveryMessage = "alpacadiscovery1";
        public const int DiscoveryPort = 32227;
        public const string ResponseString = "alpacaport";
        public const string MulticastGroup = "ff02::1";

        public static byte[] Message
        {
            get
            {
                return Encoding.ASCII.GetBytes(DiscoveryMessage);
            }
        }
    }
}
