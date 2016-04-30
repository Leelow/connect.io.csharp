using link.io.csharp.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace link.io.csharp
{
    public interface LinkIO
    {
        /// <summary>
        /// Create a new room with a random ID and join it
        /// </summary>
        /// <param name="callback">Called with the room ID when joined the new room</param>
        void createRoom(Action<String> callback);

        /// <summary>
        /// Join an existing room by specifying the room ID. If the room doesn't exist, it will create a new room with the given ID
        /// </summary>
        /// <param name="roomID">Room ID</param>
        /// <param name="callback">Called when joined the room with the room ID and a list of <seealso cref="User"/> currently in this room</param>
        void joinRoom(String roomID, Action<String, List<User>> callback);

        /// <summary>
        /// Retrieve all <seealso cref="User"/> as a list
        /// </summary>
        /// <param name="callback">Called with a list of <seealso cref="User"/> in the current room</param>
        void getAllUsersInCurrentRoom(Action<List<User>> callback);

        /// <summary>
        /// Set an event handler that is called when an <seealso cref="User"/> join or leave the current room
        /// </summary>
        /// <param name="listener">Called when an <seealso cref="User"/> join or leave the current room with a list of all <seealso cref="User"/> in it.</param>
        void onUserInRoomChanged(Action<List<User>> listener);

        /// <summary>
        /// Add a new event handler for the specified event name
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="listener">Called when the event '<paramref name="eventName"/>' is received"/></param>
        void on(String eventName, Action<Event> listener);

        /// <summary>
        /// Remove an event handler
        /// </summary>
        /// <param name="eventName">Name of the associated event handler</param>
        void off(String eventName);

        /// <summary>
        /// Broadcast an event to all users in the current room
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="data"></param>
        /// <param name="receiveAlso"></param>
        void send(String eventName, Object data, Boolean receiveAlso);

        /// <summary>
        /// Broadcast an event to all users in the current room
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="data"></param>
        void send(String eventName, Object data);

        /// <summary>
        /// Send an event to a specific list of users
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="data"></param>
        /// <param name="receivers"></param>
        /// <param name="receiveAlso"></param>
        void send(String eventName, Object data, List<User> receivers, Boolean receiveAlso);

        /// <summary>
        /// Send an event to a specific list of users
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="data"></param>
        /// <param name="receivers"></param>
        /// <param name="receiveAlso"></param>
        void send(String eventName, Object data, List<User> receivers);

        LinkIOFile sendFile(String eventName, Stream stream, String fileName, double validity);
        LinkIOFile sendFile(String eventName, Stream stream, String fileName, double validity, List<User> receivers);
        void sendFile(String eventName, LinkIOFile file);
        void sendFile(String eventName, LinkIOFile file, List<User> receivers);

        /// <summary>
        /// Get latency with the server
        /// </summary>
        /// <param name="listener"></param>
        void getLatency(Action<Double> listener);

        /// <summary>
        /// Check if this client is currently connected or not.
        /// </summary>
        /// <returns></returns>
        bool isConnected();

        /// <summary>
        /// Disconnect from current server
        /// </summary>
        void disconnect();


        User getCurrentUser();
    }
}
