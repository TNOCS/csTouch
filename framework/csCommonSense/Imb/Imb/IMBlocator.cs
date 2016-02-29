using System;
using System.Net;
using System.Net.Sockets;
using IMB3.ByteBuffers;

namespace IMB3
{
    public class IMBlocator
    {
        private static string server;

        private const int MaxUDPCommandBufferSize = 512-62;
        private const string ProtocolSep = @"://";
        
        public static bool DecodeServerURI(string aServerURI, ref string aServer, ref int aPort)
        {
            // uri:  protocol://server[:port][/path]
            if (aServerURI != "")
            {
                // remove protocol
                int i = aServerURI.IndexOf(ProtocolSep);
                if (i >= 0)
                {
                    aServer = aServerURI.Substring(i + ProtocolSep.Length);
                    // remove optional path
                    i = aServer.IndexOf('/');
                    if (i >= 0)
                        aServer = aServer.Substring(0, i);
                    // separate optional port from server
                    i = aServer.IndexOf(':');
                    if (i >= 0)
                    {
                        aPort = Convert.ToInt32(aServer.Substring(i + 1).Trim());
                        aServer = aServer.Substring(0, i);
                    }
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public static string LocateServerURI(AddressFamily aAddressFamily = AddressFamily.InterNetwork, int aPort=4000, int aTimeout=1000)
        {
            server = "";

            Socket socket = new Socket(aAddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.ReceiveTimeout = aTimeout; // ms
                EndPoint receiveEP = null;
                byte[] receiveBuffer = new byte[MaxUDPCommandBufferSize];
                int receivedBytes = 0;
                // build buffer with locator command
                TByteBuffer buffer = new TByteBuffer();
                buffer.Prepare(TConnectionPlatform.MagicBytes);
                buffer.Prepare((Int32)TConnectionPlatform.TCommands.icHUBLocate);
                buffer.Prepare((Int32)0);
                buffer.PrepareApply();
                buffer.QWrite(TConnectionPlatform.MagicBytes);
                buffer.QWrite((Int32)TConnectionPlatform.TCommands.icHUBLocate);
                buffer.QWrite((Int32)0);
                // do ipv4 and ipv6 stuff
                switch (aAddressFamily)
                {
                    case AddressFamily.InterNetworkV6:
                        // bind to socket to enable receiving response
                        socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                        socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, 0);
                        socket.SendTo(buffer.Buffer, new IPEndPoint(IPAddress.Parse("FF02::1"), aPort));
                        receiveEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                        break;
                    default: // ipv4
                        // bind to socket to enable receiving response
                        socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                        socket.EnableBroadcast = true;
                        socket.SendTo(buffer.Buffer, new IPEndPoint(IPAddress.Broadcast, aPort));
                        receiveEP = new IPEndPoint(IPAddress.Any, 0);
                        break;
                }
                try
                {
                    receivedBytes = socket.ReceiveFrom(receiveBuffer, ref receiveEP);
                    if (receivedBytes > 0)
                    {
                        // decode locator message
                        TByteBuffer receivedBuffer = new TByteBuffer(receiveBuffer, receivedBytes);
                        receivedBuffer.SkipReading(TConnectionPlatform.MagicBytes.Length);
                        Int32 command;
                        receivedBuffer.Read(out command);
                        if (command == (Int32)TConnectionPlatform.TCommands.icHUBFound)
                        {
                            string payload;
                            receivedBuffer.Read(out payload);
                            if (payload != "")
                            {
                                Int32 checkSum;
                                receivedBuffer.Read(out checkSum);
                                if (checkSum == TConnectionPlatform.CheckStringMagic)
                                    server = payload;
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            finally
            {
                socket.Close();
            }
            return server;
        }
    }
}
