using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace LinkIOcsharp.model
{
    public class Event
    {
        private String type;
        private Boolean me;
        private Dictionary<String, Object> data;

        public Event(JObject jsonObj)
        {
            try
            {

                this.type = (String) jsonObj.SelectToken("type");
                data = new Dictionary<String, Object>();

                JObject ds = (JObject) jsonObj.SelectToken("data");
                foreach(KeyValuePair<String,JToken> j in ds)
                {
                    data.Add(j.Key, j.Value.ToObject<Object>());
                }
                
            }
            catch (Exception e)
            {
                //e.printStackTrace();
            }
        }

        public T get<T>(String s)
        {

            return (T) data[s];
        }

        public Boolean isMe()
        {
            return me;
        }

        public void setMe(Boolean me)
        {
            this.me = me;
        }

        public String getType()
        {
            return type;
        }

        public void setType(String type)
        {
            this.type = type;
        }
    }
}
