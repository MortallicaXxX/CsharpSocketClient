using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CsharpSocketClient.Models
{
    public class SocketMessage
    {
        public string Channel { get; set; }
        public string Message { get; set; }

        public SocketMessage(string json)
        {
            Normalize(json);
        }

        private void Normalize(string json)
        {
            try
            {
                var definition = new { chanel = "", data = "" };
                var res = JsonConvert.DeserializeAnonymousType(json, definition);
                Channel = res.chanel;
                Message = res.data;
            }
            catch (Exception e)
            {
                Message = json;
            }
        }
    }
}
