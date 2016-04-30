using System;
using System.Collections.Generic;

// Socket.io C# implementation
using Quobject.SocketIoClientDotNet.Client;

// Json C# implementation
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using link.io.csharp.model;
using link.io.csharp.exception;
using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;TODO
using System.Threading;
using System.Threading.Tasks;
using Quobject.Collections.Immutable;
using Quobject.EngineIoClientDotNet.Client.Transports;

namespace link.io.csharp
{
    public class LinkIOImp : LinkIO
    {
        private static int CHUNK_SIZE = 1024 * 512;
        private Socket socket;
        private String serverIP;
        private string mail = string.Empty;
        private string password = string.Empty;
		private string api_key = string.Empty;
        private Action<List<User>> userInRoomChangedListener;
        private Dictionary<String, Action<Event>> eventListeners;
        private bool connected = false;
        private bool cSharpBinarySerializer = false;
        private User currentUser;

        internal LinkIOImp()
        {
            eventListeners = new Dictionary<String, Action<Event>>();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            };
        }

        public LinkIO connect(Action<LinkIO> listener)
        {
            IO.Options opts = new IO.Options();
            Dictionary<String, String> query = new Dictionary<String, String>();
            if(mail != "")
				query.Add("mail", mail);

            if (password != "")
                query.Add("password", password);
			
			query.Add("api_key", api_key);
			
            opts.Query = query;
            opts.AutoConnect = false;
            opts.Transports = ImmutableList.Create<string>(WebSocket.NAME, Polling.NAME);

            socket = IO.Socket("http://" + serverIP, opts);

            socket.On("users", (e) =>
            {
                if (userInRoomChangedListener != null)
                {
                    Task.Run(() =>
                    {
                        userInRoomChangedListener.Invoke(((JArray)e).ToObject<List<User>>());
                    });
                }
            });

            socket.On("info", (Object o) =>
            {
                JObject evt = (JObject)o;

                currentUser = new User()
                {
                    Name = (String)evt.SelectToken("name"),
                    FirstName = (String)evt.SelectToken("fname"),
                    Mail = (String)evt.SelectToken("mail"),
                    ID = (String)evt.SelectToken("id"),
                    Role = (String)evt.SelectToken("role")
                };
            });
            
            socket.On(Socket.EVENT_CONNECT, () =>
            {
                Task.Run(() =>
                {
                    connected = true;
                    listener.Invoke(this);
                });
            });

            socket.On(Socket.EVENT_DISCONNECT, () =>
            {
                connected = false;
            });

            socket.On("event", (Object o) =>
            {
                JObject evt = (JObject)o;
                String eventName = (String)evt.SelectToken("type");
                if (eventListeners.ContainsKey(eventName))
                {
                    Task.Run(() =>
                    {
                        eventListeners[eventName].Invoke(new Event(evt, cSharpBinarySerializer));
                    });
                }


            });
            
            socket.Connect();

            return this;
        }

        internal void useCSharpBinarySerializer(bool use)
        {
            cSharpBinarySerializer = use;
        }

        public void createRoom(Action<String> callback)
        {
            checkConnect();
            socket.Emit("createRoom", (id) =>
            {
                Task.Run(() =>
                {
                    callback.Invoke(id as String);
                });
            }, null);
        }

        public void joinRoom(String roomID, Action<String, List<User>> callback)
        {
            checkConnect();
            socket.Emit("joinRoom", (id, users) =>
            {
                Task.Run(() =>
                {
                    callback.Invoke(id as String, ((JArray)users).ToObject<List<User>>());
                });
            }, roomID);
        }

        public void onUserInRoomChanged(Action<List<User>> listener)
        {
            userInRoomChangedListener = listener;
        }

        public void on(String eventName, Action<Event> listener)
        {
            eventListeners.Add(eventName, listener);
        }

        public void off(String eventName)
        {
            eventListeners.Remove(eventName);
        }

        public void send(String eventName, Object data, Boolean receiveAlso)
        {
            JObject o = JObject.FromObject(new
            {
                me = receiveAlso,
                type = eventName,
                data = /*cSharpBinarySerializer ? serializeObject(data) :TODO*/ data
            });

            Task.Run(() => { socket.Emit("event", o); });
            
        }

        public void send(String eventName, Object data)
        {
            send(eventName, data, false);
        }

        public void send(string eventName, object data, List<User> receivers, bool receiveAlso)
        {
            List<String> ids = new List<string>();
            foreach (var user in receivers)
            {
                ids.Add(user.ID);
            }


            JObject o = JObject.FromObject(new
            {
                me = receiveAlso,
                type = eventName,
                data = /*cSharpBinarySerializer ? serializeObject(data) :TODO*/ data,
                idList = ids
            });

            Task.Run(() => { socket.Emit("eventToList", o); });
        }

        public void send(string eventName, object data, List<User> receivers)
        {
            send(eventName, data, receivers, false);
        }

        public void send(String eventName, Object data, string id)
        {
            List<String> ids = new List<string>();
            ids.Add(id);


            JObject o = JObject.FromObject(new
            {
                me = false,
                type = eventName,
                data = /*cSharpBinarySerializer ? serializeObject(data) :TODO*/ data,
                idList = ids
            });

            Task.Run(() => { socket.Emit("eventToList", o); });
        }

        public LinkIOFile sendFile(string eventName, Stream stream, String fileName, double validity)
        {
            LinkIOFile file = new LinkIOFile();

            int length = (int)stream.Length;
            int nbChunk = (int)Math.Ceiling((double)length / CHUNK_SIZE);
            int chunkID = 1;

            socket.Emit("upload.start", new AckImpl((fileID) =>
            {
                file.FileID = fileID.ToString();
                byte[] buffer = new byte[CHUNK_SIZE];

                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    //Console.WriteLine("Sending " + chunkID + "/" + nbChunk);
                    socket.Emit("upload.chunk", buffer);

                    chunkID++;
                }

                socket.Emit("upload.end");
                //Console.WriteLine("Done.");
                
            }), fileName, eventName, nbChunk, validity);

            return file;
        }

        public LinkIOFile sendFile(string eventName, Stream stream, String fileName, double validity, List<User> receivers)
        {
            throw new NotImplementedException();
        }

        public void sendFile(string eventName, LinkIOFile file)
        {
            throw new NotImplementedException();
        }

        public void sendFile(string eventName, LinkIOFile file, List<User> receivers)
        {
            throw new NotImplementedException();
        }

        public void getLatency(Action<Double> listener)
        {
            checkConnect();

            var from = DateTime.UtcNow;
            socket.Emit("ping", () =>
            {
                Double ping = Math.Round((DateTime.UtcNow - from).TotalSeconds, 3) * 1000;
                listener.Invoke(ping);
            }, null);
        }

        private void checkConnect()
        {
            if (socket == null)
                throw new NotConnectedException("ConnectIO: please call connect() before.");
            else if (false) //!socket.connected()
                throw new NotConnectedException("ConnectIO: socket disconnected.");
        }

        public void getAllUsersInCurrentRoom(Action<List<User>> callback)
        {
            checkConnect();
            socket.Emit("getAllUsers", (users) => {
                callback.Invoke(((JArray)users).ToObject<List<User>>());
            });
        }

        public bool isConnected()
        {
            return connected;
        }

        public void disconnect()
        {
            if (socket != null)
                socket.Disconnect();
        }

        public User getCurrentUser()
        {
            return currentUser;
        }

        /*public static string serializeObject(object o)
        {
            if (!o.GetType().IsSerializable)
            {
                return null;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, o);
                return Convert.ToBase64String(stream.ToArray());
            }
        }TODO*/

        internal void setServerIP(string ip)
        {
            this.serverIP = ip;
        }

        internal void setMail(string mail)
        {
            this.mail = mail;
        }

        internal void setUserPassword(string password)
        {
            this.password = password;
        }
		
		internal void setAPIKey(string api_key)
        {
            this.api_key = api_key;
        }
    }
}
