using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectIO
{
    public interface ConnectIO
    {

        ConnectIO connectTo(String serverIP);
        ConnectIO withUser(String user);
        ConnectIO connect(Action<Object> listener);
        void createGroup();
        void joinGroup(String groupID);

        void onJoinGroup(Action<Object> listener);
        void onUserInGroupChanged(Action<Object> listener);
        void on(String eventName, Action<Object> listener);
        void off(String eventName);

        void send(String eventName, Object data, Boolean receiveAlso);
        void send(String eventName, Object data);
        void send(String eventName, Newtonsoft.Json.JsonToken data, Boolean receiveAlso);
        void send(String eventName, Newtonsoft.Json.JsonToken data);
        void getLatency(Action<Object> listener);


    }
}
