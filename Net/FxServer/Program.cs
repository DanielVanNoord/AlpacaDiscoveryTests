using System;

namespace FxServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting server, press enter to exit.");
            AlpacaDiscovery.Responder server = new AlpacaDiscovery.Responder(4567);
            Console.ReadLine();
        }
    }
}