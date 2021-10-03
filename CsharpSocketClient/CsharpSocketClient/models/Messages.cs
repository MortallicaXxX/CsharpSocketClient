using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CsharpSocketClient.models
{
    public class Message
    {

        private string? _chanel = null;
        private string? _message = null;

        private struct M
        {
            public string chanel { get; set; }
            public string data { get; set; }
        }

        public string message { get { return _message; } }
        public string chanel { get { return _chanel; } }

        public Message(string message)
        {
            this._normalize(message);
        }

        private void _normalize(string message)
        {
            try
            {
                M m = JsonSerializer.Deserialize<M>(message);
                this._chanel = m.chanel;
                this._message = m.data;
            }
            catch (Exception e)
            {
                this._message = message;
            }
        }
    }
}
