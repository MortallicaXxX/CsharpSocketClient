using System;
using System.Collections.Generic;
using System.Text;

namespace CsharpSocketClient.Models
{
    public class SocketSubscription
    {
        public List<SocketEmitor> Subcriptions { get; set; }

        public SocketSubscription()
        {
            Subcriptions = new List<SocketEmitor>();
        }

        public void Add(string ipv6)
        {
            Subcriptions.Add(new SocketEmitor(ipv6));
        }

        public void Remove(string subIp)
        {
            int x = SubByIpv6(subIp);
            if (x > -1)
            {
                Subcriptions.RemoveAt(x);
            }
        }

        private int SubByIpv6(string subIpv6)
        {
            for (int i = 0; i < Subcriptions.Count; i++)
            {
                if (subIpv6 == Subcriptions[i].IPAddress.ToString()) return i;
            }
            return -1;
        }

        public void SendToAll(SocketCaptor cli, string message)
        {
            foreach (SocketEmitor se in Subcriptions) se.Send(message);
        }

        public void SendToAllOn(SocketCaptor cli, string chanel, string message)
        {
            foreach (SocketEmitor se in Subcriptions) se.SendOn(chanel, message);
        }
    }
}
