﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CsharpSocketClient.models
{
    public class Message
    {

        private string _eventTime;
        private string? _chanel = null;
        private string? _message = null;

        private struct M
        {
            public string eventTime { get; set; }
            public string chanel { get; set; }
            public string data { get; set; }
        }

        public double eventTime { get { return double.Parse(_eventTime); } }
        public string chanel { get { return _chanel; } }
        public string message { get { return _message; } }

        public Message(string message)
        {
            this._normalize(message);
        }

        private void _normalize(string message)
        {
            try
            {
                M m = JsonSerializer.Deserialize<M>(message);
                this._eventTime = m.eventTime;
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
