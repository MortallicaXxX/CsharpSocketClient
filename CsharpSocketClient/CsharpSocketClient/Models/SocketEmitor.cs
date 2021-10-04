using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CsharpSocketClient.Models
{
    public class SocketEmitor
    {
        public string Ipv6 { get; set; }
        public IPHostEntry IPHostEntry { get; set; }
        public IPAddress IPAddress { get; set; }
        public IPEndPoint IPEndPoint { get; set; }
        public Socket Sender { get; set; }
        public bool IsConnected { get; set; }
        public SocketChanel Channel { get; set; }

        public SocketEmitor(string ipv6 = null)
        {
            Ipv6 = ipv6;
            Channel = new SocketChanel(this);
        }

        public SocketEmitor Subscribe(string ipv6 = null)
        {
            Ipv6 = ipv6;
            SendOn("subscribe", Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString());
            return this;
        }

        public void Infos()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine((IPAddress == null ? $"socketEmitor is linked with unscribe:{11111}" : $"socketEmitor is linked with {IPAddress}:{11111}"));
            Console.ResetColor();
        }

        public void Connect()
        {
            _connect();
        }

        public void SendOn(string chanelName, string message)
        {
            Send($"{{" +
                $"\"chanel\" : \"{chanelName}\"," +
                $"\"data\" : \"{message}\"" +
                $"}}");
        }

        public void Send(string message)
        {
            _setEndpoint();
            _setSender();
            _connect();

            try
            {
                if (IsConnected == false) throw new InvalidCastException("Socket n'est pas connecter au point d'accès.");
                _send(message);
                _print(_Onmessage());
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine(e);
            }

            _close();
        }

        public void Close()
        {
            try
            {
                if (IsConnected == false) throw new InvalidCastException("Socket n'est pas connecter au point d'accès.");
                _close();
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine(e);
            }
        }

        private byte[] _normalize(string message)
        {
            return Encoding.ASCII.GetBytes(message + "<EOF>");
        }

        private void _setEndpoint()
        {
            //if(this.ipv6 != null) Console.WriteLine(IPAddress.Parse(this.ipv6));
            IPHostEntry = Ipv6 == null ? Dns.GetHostEntry(Dns.GetHostName()) : Dns.GetHostEntry(IPAddress.Parse(Ipv6));
            IPAddress = Ipv6 == null ? IPHostEntry.AddressList[0] : IPAddress.Parse(Ipv6);
            IPEndPoint = new IPEndPoint(IPAddress, 11111);
        }

        private void _setSender()
        {
            Sender = new Socket(IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        private void _connect()
        {
            try
            {
                Sender.Connect(IPEndPoint);
                _print($"Socket connected to : {Sender.RemoteEndPoint}"); 
                _switchConnectionState();
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }

            catch (SocketException se)
            {

                Console.WriteLine("SocketException : {0}", se.ToString());
            }

            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }

        private void _send(string message)
        {
            Sender.Send(_normalize(message));
        }

        private string _Onmessage()
        {
            // Data buffer
            byte[] messageReceived = new byte[1024];
            int byteRecv = Sender.Receive(messageReceived);
            return Encoding.ASCII.GetString(messageReceived, 0, byteRecv);
        }

        private void _print(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{IPHostEntry.HostName} ");
            Console.ResetColor();
            Console.Write(message);
            Console.WriteLine("");
        }

        private void _execClient()
        {
            try
            {
                _connect();                        // connexion au endpoint
                _send("C'est un test les kheys");   // Envois d'un message
                _print(_Onmessage());         // impression dans la console du message de reçeption
                _close();                          // fermeture du socket
            }
            // Gestion des erreurs de sockets
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }

            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }

            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }

        private void _close()
        {
            Sender.Shutdown(SocketShutdown.Both);
            Sender.Close();
            _switchConnectionState();
        }

        private void _switchConnectionState()
        {
            IsConnected = IsConnected == false;
        }
    }
}
