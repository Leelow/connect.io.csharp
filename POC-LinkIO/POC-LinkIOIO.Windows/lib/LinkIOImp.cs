using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Socket.io C# implementation
using Quobject.SocketIoClientDotNet.Client;

// Json C# implementation
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using LinkIOcsharp.model;
using LinkIOcsharp.exception;

namespace LinkIOcsharp
{
    public class LinkIOImp : LinkIO
    {
        public static LinkIO Instance = new LinkIOImp();

        private Socket socket;
        private String serverIP;
        private String user;
        private Action<List<User>> userInRoomChangedListener;
        private Dictionary<String, Action<Event>> eventListeners;
        private bool connected = false;

        private LinkIOImp() {
            eventListeners = new Dictionary<String, Action<Event>>();
        }

        public static LinkIO create()
        {
            return new LinkIOImp();
        }

        public LinkIO connectTo(String serverIP)
        {
            this.serverIP = serverIP;
            return this;
        }

        public LinkIO withUser(String user)
        {
            this.user = user;
            return this;
        }

        public LinkIO connect(Action listener)
        {

            //Console.WriteLine("Connecting to http://" + serverIP + "?user=" + user);

            IO.Options opts = new IO.Options();
            Dictionary<String, String> query = new Dictionary<String, String>();
            query.Add("user", user);
            opts.Query = query;
            opts.AutoConnect = false;

            socket = IO.Socket("http://" + serverIP, opts);

            socket.On("users", (e) =>
            {
                if (userInRoomChangedListener != null)
                    userInRoomChangedListener.Invoke(((JArray) e).ToObject<List<User>>());
            });

            socket.On(Socket.EVENT_CONNECT, () =>
            {
                connected = true;
                listener.Invoke();
            });

            socket.On(Socket.EVENT_DISCONNECT, () =>
            {
                connected = false;
            });

            socket.On("event", (Object o) =>
            {
                JObject evt = (JObject) o;
                String eventName = (String) evt.SelectToken("type");
                if (eventListeners.ContainsKey(eventName))
                {
                    eventListeners[eventName].Invoke(new Event(evt));
                }                        


            });

            socket.Connect();

            return this;
        }

        public void createRoom(Action<String> callback)
        {
            checkConnect();
            socket.Emit("createRoom", (id) =>
            {
                callback.Invoke(id as String);
            }, null);
        }

        public void joinRoom(String roomID, Action<String, List<User>> callback)
        {
            checkConnect();
            socket.Emit("joinRoom", (id, users) =>
            {
                callback.Invoke(id as String, ((JArray)users).ToObject<List<User>>());
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
                data = data
            });

            socket.Emit("event", o);
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
                data = data,
                idList = ids
            });

            socket.Emit("eventToList", o);
        }

        public void send(string eventName, object data, List<User> receivers)
        {
            send(eventName, data, receivers, false);
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
                throw new NotConnectedException("LinkIO: please call connect() before.");
            else if (false) //!socket.connected()
                throw new NotConnectedException("LinkIO: socket disconnected.");
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
    }
}
