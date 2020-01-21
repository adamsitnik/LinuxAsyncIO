using System;
using System.Buffers.Binary;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace EchoServer
{
    internal static class SingleThreadedEpollEchoServer
    {
        internal static unsafe void Run(int portNumber)
        {
            int socketFileDescriptor = socket(AF_INET, SOCK_STREAM, 0);
            if (socketFileDescriptor < 0)
                Environment.FailFast($"Failed to create socket, socket returned {socketFileDescriptor}");

            sockaddr_in socketAddress = new sockaddr_in
            {
                sin_family = AF_INET,
                sin_port = htons((ushort) portNumber),
                sin_addr = INADDR_ANY
            };
            int bindResult = bind(socketFileDescriptor, (sockaddr*) &socketAddress, sizeof(sockaddr_in));
            if (bindResult < 0)
                Environment.FailFast($"Failed to bind, bind returned {bindResult}");

            int listenResult = listen(socketFileDescriptor, BACKLOG);
            if (listenResult < 0)
                Environment.FailFast($"Failed to start listening, listen returned {listenResult}");
            
            int epollFileDescriptor = epoll_create(MAX_EVENTS);
            if (epollFileDescriptor < 0)
                Environment.FailFast($"Failed to create epoll, epoll_create returned {epollFileDescriptor}");

            epoll_event epollAddSocketEvent = new epoll_event
            {
                events = EPOLLIN,
                data = new epoll_data_t
                {
                    fd = socketFileDescriptor
                }
            };
            if (epoll_ctl(epollFileDescriptor, EPOLL_CTL_ADD, socketFileDescriptor, &epollAddSocketEvent) == -1)
                Environment.FailFast("Failed to add new socket file descriptor to epoll");

            epoll_event[] epollEvents = new epoll_event[MAX_EVENTS];
            byte[] messageBuffer = new byte[MAX_MESSAGE_LENGTH];
            
            fixed (epoll_event* pinnedEvents = epollEvents)
            fixed (byte* pinnedMessageBuffer = messageBuffer)
            {
                while (true)
                {
                    int eventsCount = epoll_wait(epollFileDescriptor, pinnedEvents, MAX_EVENTS, -1);
                    if (eventsCount == -1)
                        Environment.FailFast("epoll_wait returned -1");

                    for (int i = 0; i < eventsCount; i++)
                    {
                        int currentSocketFileDescriptor = epollEvents[i].data.fd;
                        if (currentSocketFileDescriptor == socketFileDescriptor)
                        {
                            sockaddr_in clientAddress;
                            socklen_t clientAddressSize = sizeof(sockaddr_in);
                            int acceptResult = accept4(socketFileDescriptor, (sockaddr*) &clientAddress, &clientAddressSize, SOCK_NONBLOCK);
                            if (acceptResult == -1)
                                Environment.FailFast($"accept4 returned {acceptResult}");

                            epollAddSocketEvent.events = EPOLLIN | EPOLLET;
                            epollAddSocketEvent.data.fd = acceptResult;
                            
                            if (epoll_ctl(epollFileDescriptor, EPOLL_CTL_ADD, acceptResult, &epollAddSocketEvent) == -1)
                                Environment.FailFast("Failed to add socket to epoll");
                        }
                        else
                        {
                            ssize_t bytesRead = recv(currentSocketFileDescriptor, pinnedMessageBuffer, MAX_MESSAGE_LENGTH, 0);
                            send(currentSocketFileDescriptor, pinnedMessageBuffer, (size_t)bytesRead, 0);
                        }
                    }
                }
            }
        }

        private static unsafe in_addr INADDR_ANY
        {
            get
            {
                var address = new in_addr();
                if (BitConverter.IsLittleEndian)
                    BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(address.s_addr, 4), 0);
                else
                    BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(address.s_addr, 4), 0);
                return address;
            }
        }

        private const int BACKLOG = 128;
        private const int MAX_EVENTS = 10;
        private const int MAX_MESSAGE_LENGTH = 1024;
    }
}