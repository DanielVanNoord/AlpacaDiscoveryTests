using System;
using System.Net;

namespace FxDiscovery
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Searching...");

            AlpacaDiscovery.Finder find = new AlpacaDiscovery.Finder(AddressFound);

            Console.WriteLine("Press Enter to Exit");
            Console.ReadLine();
        }

        private static void AddressFound(IPEndPoint ep)
        {
            Console.WriteLine(string.Format("{0}:{1} {2}", ep.Address, ep.Port, DateTime.Now));
        }
    }
}