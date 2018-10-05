using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace RssHybrid
{
    class Bridge
    {
        private Dictionary<string, Action<string>> actions;

        public Bridge()
        {
            actions = new Dictionary<string, Action<string>>();
        }

        public void AddAction(string method, Action<string> action)
        {
            actions.Add(method, action);
        }

        public void Call(string value)
        {
            JsonObject result = JsonValue.Parse(value).GetObject();

            string id = result.GetNamedString("id");
            string method = result.GetNamedString("method");

            actions[method].Invoke(id);
        }
    }
}
