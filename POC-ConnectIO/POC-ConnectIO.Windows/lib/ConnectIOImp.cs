using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Socket.io C# implementation
using Quobject.SocketIoClientDotNet.Client;

// Json C# implementation
using Newtonsoft.Json.Linq;

using ConnectIO.lib.exception;
using Newtonsoft.Json;
using POC_ConnectIO.lib;

namespace ConnectIO.lib
{
    public class ConnectIOImp : ConnectIO
    {
        public static ConnectIO Instance = new ConnectIOImp();

        private Socket socket;
        private String serverIP;
        private String user;
        private Action<Object> joinGroupListener;
        private Action<List<String>> userInGroupChangedListener;
        private Dictionary<String, Action<Object>> eventListeners;

        private ConnectIOImp() {
            eventListeners = new Dictionary<String, Action<Object>>();
        }

        public static ConnectIO create()
        {
            return new ConnectIOImp();
        }

        public ConnectIO connectTo(String serverIP)
        {
            this.serverIP = serverIP;
            return this;
        }

        public ConnectIO withUser(String user)
        {
            this.user = user;
            return this;
        }

        public ConnectIO connect(Action<Object> listener)
        {

            //Console.WriteLine("Connecting to http://" + serverIP + "?user=" + user);

            IO.Options opts = new IO.Options();
            Dictionary<String, String> query = new Dictionary<String, String>();
            query.Add("user", user);
            opts.Query = query;
            opts.AutoConnect = false;

            socket = IO.Socket("http://" + serverIP, opts);

            socket.On("joinedGroup", (e) =>
            {
                if (joinGroupListener != null)
                    joinGroupListener.Invoke((string)e);
            });

            socket.On("users", (e) =>
            {
                if (userInGroupChangedListener != null)
                    userInGroupChangedListener.Invoke(((JArray) e).ToObject<List<String>>());
            });

            socket.On(Socket.EVENT_CONNECT, () =>
            {
                listener.Invoke(null);

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

        public void createGroup()
        {
            checkConnect();
            socket.Emit("createGroup");
        }

        public void joinGroup(String groupID)
        {
            checkConnect();
            socket.Emit("joinGroup", groupID);
        }

        public void onJoinGroup(Action<Object> listener)
        {
            joinGroupListener = listener;
        }

        public void onUserInGroupChanged(Action<Object> listener)
        {
            userInGroupChangedListener = listener;
        }

        public void on(String eventName, Action<Object> listener)
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

        public void send(string eventName, JsonToken data, Boolean receiveAlso)
        {
            throw new NotImplementedException();
        }

        public void send(string eventName, JsonToken data)
        {
            throw new NotImplementedException();
        }

        public void getLatency(Action<Object> listener)
        {
            checkConnect();
            socket.Emit("ping");
        }

        private void checkConnect()
        {
            if (socket == null)
                throw new NotConnectedException("ConnectIO: please call connect() before.");
            else if (false) //!socket.connected()
                throw new NotConnectedException("ConnectIO: socket disconnected.");
        }

    }
}
