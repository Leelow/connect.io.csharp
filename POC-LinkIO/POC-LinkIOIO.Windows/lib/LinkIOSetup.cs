using Quobject.SocketIoClientDotNet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace link.io.csharp
{
    public class LinkIOSetup
    {
        private static LinkIOSetup instance;
        public static LinkIOSetup Instance {
            get
            {
                if (instance == null)
                    instance = new LinkIOSetup();
                return instance;
            }
        }

        private LinkIOImp linkIO;
        private string serverIP;
        private string mail;
        private string password;
		private string api_key;
        private bool cSharpBinarySerializer = false;

        private LinkIOSetup()
        {

        }

        public LinkIOSetup create()
        {
            linkIO = new LinkIOImp();
            serverIP = string.Empty;
            mail = string.Empty;
            password = string.Empty;
			api_key = string.Empty;
            cSharpBinarySerializer = false;
            return this;
        }

        public LinkIOSetup connectTo(String serverIP)
        {
            this.serverIP = serverIP;
            return this;
        }

        public LinkIOSetup withMail(String mail)
        {
            this.mail = mail;
            return this;
        }

        public LinkIOSetup withPassword(String password)
        {
            this.password = password;
            return this;
        }
		
		public LinkIOSetup withAPIKey(String api_key)
        {
            this.api_key = api_key;
            return this;
        }

        [Obsolete("Using C# binary serializer instead of default JSON serializer disable communication with the Java client.")]
        public LinkIOSetup useCSharpBinarySerializer(bool use)
        {
            cSharpBinarySerializer = use;
            return this;
        }

        public void connect(Action<LinkIO> listener)
        {
            linkIO.setServerIP(serverIP);
            linkIO.setMail(mail);
            linkIO.setUserPassword(password);
			linkIO.setAPIKey(api_key);
            linkIO.useCSharpBinarySerializer(cSharpBinarySerializer);
            linkIO.connect(listener);
        }
    }
}
