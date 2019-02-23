using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;

namespace TinyChat_Server
{
    class Program
    {
        private List<ClientHandler> clients;
        private BackgroundWorker Listener_bw;
        private Socket listenerSocket;
        private IPAddress serverIP;
        private int serverPort;

        static void Main(string[] args)
        {
            Program domain = new Program();
            domain.clients = new List<ClientHandler>();

            //arguments are the ip and port you wish to connect to, default is any on port 8000
            if(args.Length == 0)
            {
                domain.serverPort = 8000;
                domain.serverIP = IPAddress.Any;
            }

            if(args.Length == 1)
            {
                domain.serverPort = 8000;
                domain.serverIP = IPAddress.Parse(args[0]);
            }

            if(args.Length == 2)
            {
                domain.serverPort = int.Parse(args[1]);
                domain.serverIP = IPAddress.Parse(args[0]);
            }

            //init listener and start the server
            domain.Listener_bw = new BackgroundWorker();
            domain.Listener_bw.WorkerSupportsCancellation = true;
            domain.Listener_bw.DoWork += new DoWorkEventHandler(domain.Start);
            domain.Listener_bw.RunWorkerAsync();

            Console.WriteLine("Server active on port {0}{1}{2}. Press ENTER to shut down server. \n", domain.serverIP.ToString(), ":", domain.serverPort.ToString());

            Console.ReadLine();

            domain.DisconnectServer();
        }

        private void Start(object sender, DoWorkEventArgs e)
        {
            //finish later
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenerSocket.Bind(new IPEndPoint(serverIP, serverPort));
            listenerSocket.Listen(200);

            while (true)
                CreateClientHandler(listenerSocket.Accept());
        }

        private void CreateClientHandler(Socket socket)
        {
            ClientHandler newHandler = new ClientHandler(socket);
            newHandler.CommandReceived += new CommandReceivedEventHandler(CommandReceived);
            newHandler.Disconnected += new DisconnectedEventHandler(ClientDC);
            AbnormalCheck(newHandler);
            clients.Add(newHandler);
            UpdateConsole("Connected.", newHandler.IP, newHandler.Port);

        }
        private void AbnormalCheck(ClientHandler handler)
        {
            if (RemoveClient(handler.IP))
                UpdateConsole("Disconnected:", handler.IP, handler.Port);
        }
        void ClientDC(object sender, ClientEventArgs e)
        {
            if (RemoveClient(e.IP))
                UpdateConsole("Disconnected: ", e.IP, e.Port);
        }
        private bool RemoveClient(IPAddress ip)
        {
            lock(this)
            {
                int i = ClientIndex(ip);
                if(i != -1)
                {
                    string n = clients[i].ClientName;
                    clients.RemoveAt(i);

                    Command c = new Command(Type.ClientLogoff, IPAddress.Broadcast);
                    c.SenderName = n;
                    c.SenderIP = ip;
                    BroadcastCommand(c);
                    return true;
                }
                return false;
            }
        }
        private void CommandReceived(object sender, CommandEventArgs e)
        {
            if (e.Cmd.CmdType == Type.ClientLogin)
                SetHandlerName(e.Cmd.SenderIP, e.Cmd.Body);

            if (e.Cmd.CmdType == Type.IsNameExists)
            {
                bool isExists = DoesNameExist(e.Cmd.SenderIP, e.Cmd.Body);
                Existance(e.Cmd.SenderIP, isExists);
                return;
            }
            else if (e.Cmd.CmdType == Type.SendClientList)
            {
                SendClientList(e.Cmd.SenderIP);
                return;
            }

            if (e.Cmd.Target.Equals(IPAddress.Broadcast))
                BroadcastCommand(e.Cmd);
            else
                SendCommandToTarget(e.Cmd);
        }
        private void Existance(IPAddress targetIP, bool isExists)
        {
            Command cExists = new Command(Type.IsNameExists, targetIP, isExists.ToString());
            cExists.SenderIP = serverIP;
            cExists.SenderName = "SERVER";
            SendCommandToTarget(cExists);
        }
        //sends the list of clients to a newly connected client
        private void SendClientList(IPAddress newIP)
        {
            foreach(ClientHandler handler in clients)
            {
                if(handler.Connected && !handler.IP.Equals(newIP))
                {
                    Command c = new Command(Type.SendClientList, newIP);
                    c.Body = handler.IP.ToString() + ":" + handler.ClientName;
                    c.SenderIP = serverIP;
                    c.SenderName = "SERVER";
                    SendCommandToTarget(c);
                }
            }
        }
        //gets the client index of specified IP; returns -1 if not found
        private int ClientIndex(IPAddress ip)
        {
            int i = -1;
            foreach(ClientHandler handler in clients)
            {
                i++;
                if (handler.IP.Equals(ip))
                    return i;
            }
            return -1;
        }
        private string SetHandlerName(IPAddress remoteIP, string name)
        {
            int i = ClientIndex(remoteIP);
            if(i != -1)
            {
                string n = name.Split(new char[] { ':' })[1];
                clients[i].ClientName = n;
                return n;
            }
            return "";
        }
        private bool DoesNameExist(IPAddress remoteIP, string name)
        {
            foreach (ClientHandler handler in clients)
                if (handler.ClientName == name && !handler.IP.Equals(remoteIP))
                    return true;

            return false;
        }
        private void BroadcastCommand(Command cmd)
        {
            foreach(ClientHandler handler in clients)
                if(!handler.IP.Equals(cmd.SenderIP))
                {
                    handler.SendCommand(cmd);
                }
        }
        private void SendCommandToTarget(Command cmd)
        {
            foreach(ClientHandler handler in clients)
                if(handler.IP.Equals(cmd.Target))
                {
                    handler.SendCommand(cmd);
                    return;
                }
        }
        private void UpdateConsole(string status, IPAddress IP, int port)
        {
            Console.WriteLine("Client {0}{1}{2} has been {3} ( {4}|{5} )", IP.ToString(), ":", port.ToString(), status, DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
        }

        //disconnects all the clients from the server
        public void DisconnectServer()
        {
            if (clients != null)
            {
                foreach (ClientHandler handler in clients)
                    handler.Disconnect();

                Listener_bw.CancelAsync();
                Listener_bw.Dispose();
                listenerSocket.Close();
                GC.Collect();
            }
        }
    }
  
}
