using System;

namespace EchoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Please provide a port number and option");
                return;
            }
            if (!int.TryParse(args[0], out int portNumber))
            {
                Console.WriteLine($"Invalid port number: '{args[0]}'");
                return; 
            }
            if (!bool.TryParse(args[1], out bool executeOnEpollThread))
            {
                Console.WriteLine($"Invalid boolean executeOnEpollThread option: '{args[1]}'");
                return; 
            }

            EpollEventLoop.Run(portNumber, executeOnEpollThread);
        }
    }
}