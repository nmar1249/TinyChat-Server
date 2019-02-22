using System;
using System.Net;
using System.Net.Sockets;

namespace TinyChat_Server
{
    public delegate void CommandReceivedEventHandler(object sender, CommandEventArgs e);
    public delegate void CommandSentEventHandler(object sender, EventArgs e);
    public delegate void CommandSendingFailedEventHandler(object sender, EventArgs e);

    public delegate void DisconnectedEventHandler(object sender, ClientEventArgs e);

    public class CommandEventArgs : EventArgs
    {
        private Command cmd;

        public Command Cmd
        {
            get { return cmd; }
        }

        public CommandEventArgs(Command c)
        {
            cmd = c;
        }
    }

    public class ClientEventArgs : EventArgs
    {
        private Socket soc;

        public IPAddress IP
        {
            get { return ((IPEndPoint)soc.RemoteEndPoint).Address; }
        }

        public int Port
        {
            get { return ((IPEndPoint)soc.RemoteEndPoint).Port; }
        }

        public ClientEventArgs(Socket clientHandlerSocket)
        {
            soc = clientHandlerSocket;
        }
    }
}
