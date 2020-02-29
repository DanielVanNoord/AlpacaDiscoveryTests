using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AlpacaDiscovery
{
    // Implementing this because of the need to support .Net 3.5 and .Net Standard
    internal static class PlatformDetection
    {
        internal static bool IsWindows
        {
            get
            {
                string windir = Environment.GetEnvironmentVariable("windir");
                if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
                {
                    return true;
                }
                return false;
            }
        }
    }
}
