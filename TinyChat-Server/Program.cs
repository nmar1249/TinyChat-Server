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
        }
        private string SetManagerName(IPAddress remoteIP, string name)
        {
            //finish this later
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
