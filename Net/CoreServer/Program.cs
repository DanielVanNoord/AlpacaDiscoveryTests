using System;

namespace CoreServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting server, press enter to exit.");
            AlpacaDiscovery.Server server = new AlpacaDiscovery.Server(6764);
            Console.ReadLine();
        }
    }
}