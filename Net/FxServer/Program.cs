using System;

namespace FxServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting server, press enter to exit.");
            AlpacaDiscovery.Server server = new AlpacaDiscovery.Server(4567);
            Console.ReadLine();
        }
    }
}