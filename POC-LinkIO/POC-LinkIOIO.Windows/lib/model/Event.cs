using System;

using Newtonsoft.Json.Linq;
using System.IO;

namespace link.io.csharp.model
{
    public class Event
    {
        private String type;
        private JObject root;
        private string obj;
        private bool cSharpBinarySerializer;

        public Event(JObject jsonObj, bool cSharpBinarySerializer)
        {
            try
            {
                this.cSharpBinarySerializer = cSharpBinarySerializer;
                root = jsonObj;
                this.type = (String)jsonObj.SelectToken("type");
                this.obj = jsonObj.SelectToken("data").ToObject<string>();
            }
            catch {}
        }

        public T get<T>()
        {
            /*if(cSharpBinarySerializer)
                return (T)deserializeObject(obj);
            else*/
                return root.SelectToken("data").ToObject<T>();
        }

        public String getType()
        {
            return type;
        }

        /*private static object deserializeObject(string str)
        {
            byte[] bytes = Convert.FromBase64String(str);

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }*/
    }

}
