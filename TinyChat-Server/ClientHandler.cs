using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Threading;

namespace TinyChat_Server
{

    public class ClientHandler
    {
        private Socket socket;
        private string clientName;
        private BackgroundWorker Receiver_bw;

        NetworkStream netStream;
        
        //retrieves the ip of the remote client
        public IPAddress IP
        {
            get
            {
                if(socket != null)
                {
                    return ((IPEndPoint)socket.RemoteEndPoint).Address;
                }
                else
                {
                    return IPAddress.None;
                }
            }
        }

        public int Port
        {
            get
            {
                if(socket != null)
                {
                    return ((IPEndPoint)socket.RemoteEndPoint).Port;
                }
                else
                {
                    return -1;
                }
            }
        }

        //returns connected state (true or false)
        public bool Connected
        {
            get
            {
                if(socket != null)
                {
                    return socket.Connected;
                }
                else
                {
                    return false;
                }
            }
        }

        public string ClientName
        {
            get { return clientName; }
            set { clientName = value; }
        }

        //constructor
        public ClientHandler(Socket clientSocket)
        {
            socket = clientSocket;
            netStream = new NetworkStream(socket);
            Receiver_bw = new BackgroundWorker();
            Receiver_bw.DoWork += new DoWorkEventHandler(Start);
        }

        //private methods
        private void Start(object sender, DoWorkEventArgs e)
        {
            //loop while socket is connected
            while(socket.Connected)
            {
                //get command type
                byte[] buff = new byte[4];
                int read = netStream.Read(buff, 0, 4);

                if (read == 0)
                    break;

                Type cmdType = (Type)(BitConverter.ToInt32(buff, 0));

                //get command target size (do this before getting the target in order to call the Read function)
                string cmdTarget = "";
                buff = new byte[4];
                read = netStream.Read(buff, 0, 4);

                if (read == 0)
                    break;

                int ipSize = BitConverter.ToInt32(buff, 0);

                //get the target
                buff = new byte[ipSize];
                read = netStream.Read(buff, 0, ipSize);

                if (read == 0)
                    break;

                cmdTarget = Encoding.ASCII.GetString(buff);

                //get the command body size
                string body = "";
                buff = new byte[4];
                read = netStream.Read(buff, 0, 4);

                if (read == 0)
                    break;

                int bodySize = BitConverter.ToInt32(buff, 0);

                //get command body data
                buff = new byte[bodySize];
                read = netStream.Read(buff, 0, bodySize);

                if (read == 0)
                    break;

                body = Encoding.Unicode.GetString(buff);

                //send command
                Command cmd = new Command(cmdType, IPAddress.Parse(cmdTarget), body);
                cmd.SenderIP = IP;

                if (cmd.CmdType == Type.ClientLogin)
                    cmd.SenderName = cmd.Body.Split(new char[] { ':' })[1];
                else
                    cmd.SenderName = clientName;

                OnCommandReceived(new CommandEventArgs(cmd));
            }
            OnDisconnected(new ClientEventArgs(socket));
            Disconnect();
        }

        private void Sender_bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null && ((bool)e.Result))
                OnCommandSent(new EventArgs());
            else
                OnCommandFailed(new EventArgs());

            ((BackgroundWorker)sender).Dispose();
            GC.Collect();
        }

        private void Sender_bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Command cmd = (Command)e.Argument;
            e.Result = SendCommandToClient(cmd);
        }

        Semaphore sem = new Semaphore(1, 1);

        private bool SendCommandToClient(Command cmd)
        {
            try
            {
                sem.WaitOne();

                //type
                byte[] buff = new byte[4];
                buff = BitConverter.GetBytes((int)cmd.CmdType);

                netStream.Write(buff, 0, 0);
                netStream.Flush();

                //sender IP
                byte[] senderIPBuff = Encoding.ASCII.GetBytes(cmd.SenderIP.ToString());
                buff = new byte[4];
                buff = BitConverter.GetBytes(senderIPBuff.Length);

                netStream.Write(buff, 0, 4);
                netStream.Flush();
                netStream.Write(senderIPBuff, 0, senderIPBuff.Length);
                netStream.Flush();

                //sender name
                byte[] senderNameBuff = Encoding.Unicode.GetBytes(cmd.SenderName.ToString());
                buff = new byte[4];
                buff = BitConverter.GetBytes(senderNameBuff.Length);
                netStream.Write(buff, 0, 4);
                netStream.Flush();
                netStream.Write(senderNameBuff, 0, senderNameBuff.Length);
                netStream.Flush();

                //target
                byte[] ipBuff = Encoding.ASCII.GetBytes(cmd.Target.ToString());
                buff = new byte[4];
                buff = BitConverter.GetBytes(ipBuff.Length);
                netStream.Write(buff, 0, 4);
                netStream.Flush();
                netStream.Write(ipBuff, 0, ipBuff.Length);
                netStream.Flush();

                //body
                if(cmd.Body == null || cmd.Body == "")
                {
                    cmd.Body = "\n";
                }

                byte[] bodyBuff = Encoding.Unicode.GetBytes(cmd.Body);
                buff = new byte[4];
                buff = BitConverter.GetBytes(bodyBuff.Length);
                netStream.Write(buff, 0, 4);
                netStream.Flush();
                netStream.Write(bodyBuff, 0, bodyBuff.Length);
                netStream.Flush();

                sem.Release();
                return true;
            }
            catch
            {
                sem.Release();
                return false;
            }
        }
        
        //public methods
        public void SendCommand(Command cmd)
        {
            if(socket != null && socket.Connected)
            {
                BackgroundWorker Sender_bw = new BackgroundWorker();
                Sender_bw.DoWork += new DoWorkEventHandler(Sender_bw_DoWork);
                Sender_bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Sender_bw_RunWorkerCompleted);
                Sender_bw.RunWorkerAsync(cmd);
            }
            else
            {
                OnCommandFailed(new EventArgs());
            }
        }
        //disconnects the current client manager from the remote client
        //returns true if disconnected, false if not
        public bool Disconnect()
        {
            if(socket != null && socket.Connected)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        //events
        public event CommandReceivedEventHandler CommandReceived;
        public event CommandSentEventHandler CommandSent;
        public event CommandSendingFailedEventHandler CommandFailed;
        public event DisconnectedEventHandler Disconnected;

        protected virtual void OnCommandReceived(CommandEventArgs e)
        {
            CommandReceived?.Invoke(this, e);
        }

        protected virtual void OnCommandSent(EventArgs e)
        {
            CommandSent?.Invoke(this, e);
        }

        protected virtual void OnCommandFailed(EventArgs e)
        {
            CommandFailed?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(ClientEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }
    }
}
