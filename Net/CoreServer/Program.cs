using System;

namespace CoreServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting server, press enter to exit.");
            AlpacaDiscovery.Responder server = new AlpacaDiscovery.Responder(6764);
            Console.ReadLine();
        }
    }
}