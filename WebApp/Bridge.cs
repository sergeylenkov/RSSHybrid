using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace RssHybrid
{
    public class BridgeParameters
    {
        public string Id { get; set; }
        public string Method { get; set; }       
        public Dictionary<string, string> Parameters { get; set; }
    }

    class Bridge
    {
        private Dictionary<string, Action<BridgeParameters>> actions;

        public Bridge()
        {
            actions = new Dictionary<string, Action<BridgeParameters>>();
        }

        public void AddAction(string method, Action<BridgeParameters> action)
        {
            actions.Add(method, action);
        }

        public void Call(string value)
        {            
            var result = JsonConvert.DeserializeObject<BridgeParameters>(value);
            
            actions[result.Method].Invoke(result);
        }
    }
}
