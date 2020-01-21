using System;

namespace EchoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please provide a port number");
                return;
            }
            if (!int.TryParse(args[0], out int portNumber))
            {
                Console.WriteLine($"Invalid port number: '{args[0]}'");
                return; 
            }

            SingleThreadedEpollEchoServer.Run(portNumber);
        }
    }
}