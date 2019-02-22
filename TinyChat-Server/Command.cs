using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
namespace TinyChat_Server
{
    public class Command
    {
        private Type cmdType;
        private IPAddress senderIP;
        private string senderName;
        private IPAddress target;
        private string body;

        public Type CmdType
        {
            get { return cmdType; }
            set { cmdType = value; }
        }

        public IPAddress SenderIP
        {
            get { return senderIP; }
            set { senderIP = value; }
        }
        
        public string SenderName
        {
            get { return senderName; }
            set { senderName = value; }
        }

        public IPAddress Target
        {
            get { return target; }
            set { target = value; }
        }

        public string Body
        {
            get { return body; }
            set { body = value; }
        }

        //constructors
        public Command(Type t, IPAddress targetIP, string b)
        {
            cmdType = t;
            target = targetIP;
            body = b;
        }

        public Command(Type t, IPAddress targetIP)
        {
            cmdType = t;
            target = targetIP;
            body = "";
        }
    }
}
