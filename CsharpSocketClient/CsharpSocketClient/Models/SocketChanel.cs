using System;
using System.Collections.Generic;
using System.Text;

namespace CsharpSocketClient.Models
{
    public class SocketChanel
    {
        public List<Func<dynamic, SocketMessage, bool>> CallBacks { get; set; }
        public List<string> Channels { get; set; }
        public dynamic Root { get; set; }

        public SocketChanel(dynamic root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Channels = new List<string>();
            CallBacks = new List<Func<dynamic, SocketMessage, bool>>();
        }

        public void Exec(SocketMessage message)
        {
            try
            {
                if ((message.Channel != null) && (Channels.Contains(message.Channel))) CallBacks[GetIdFromName(message.Channel)](Root, message);
                else throw new Exception("Ce chanel n'est pas définis");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Add(string chanelName, Func<dynamic, SocketMessage, bool> callback)
        {
            if (ChanelExists(chanelName) == false)
            {
                CallBacks.Add(callback);
                Channels.Add(chanelName);
            }
        }

        public bool Delete(string chanelName)
        {
            int x = GetIdFromName(chanelName);
            if (x > -1)
            {
                CallBacks.RemoveAt(x);
                Channels.RemoveAt(x);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ChanelExists(string chanelName)
        {
            return Channels.Contains(chanelName);
        }

        private int GetIdFromName(string name)
        {
            for (int i = 0; i < Channels.Count; i++)
            {
                if (Channels[i] == name) return i;
            }
            return -1;
        }

        private Func<dynamic, SocketMessage, bool> GetCallBackFromName(string name)
        {
            for (int i = 0; i < Channels.Count; i++)
            {
                if (Channels[i] == name) return CallBacks[i];
            }
            return null;
        }
    }
}
