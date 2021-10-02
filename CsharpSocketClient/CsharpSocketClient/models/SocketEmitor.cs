using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace CsharpSocketClient.models
{
    class SocketEmitor
    {
        private string ipv6;
        private IPHostEntry ipHost;
        private IPAddress ipAddr;
        private IPEndPoint localEndPoint;
        private Socket sender;

        private bool isConnected = false;

        private SocketChanels chanels;

        public SocketEmitor(string? ipv6 = null)
        {
            this.ipv6 = ipv6;
            this.chanels = new SocketChanels(this);
        }

        public IPAddress IP()
        {
            return this.ipAddr;
        }

        public SocketEmitor Subscribe(string? ipv6 = null)
        {
            this.ipv6 = ipv6;
            this.SendOn("subscribe", Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString());
            return this;
        }

        /*
         * @{name}      Infos
         * @{type}      public void
         * @{desc}      
         */
        public void Infos()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine((this.ipAddr == null ? $"socketEmitor is linked with unscribe:{11111}" : $"socketEmitor is linked with {this.ipAddr}:{11111}"));
            Console.ResetColor();
        }

        /*
         * @{name}      Connect
         * @{type}      public void
         * @{desc}      
         */
        public void Connect()
        {
            this._connect();
        }

        /*
         * @{name}      SendOn
         * @{type}      public void
         * @{desc}      
         */
        public void SendOn(string chanelName, string message)
        {
            this.Send($"{{" +
                $"\"chanel\" : \"{chanelName}\"," +
                $"\"data\" : \"{message}\"" +
                $"}}");
        }

        /*
         * @{name}      Send
         * @{type}      public void
         * @{desc}      
         */
        public void Send(string message)
        {
            this._setEndpoint();
            this._setSender();
            this._connect();

            try
            {
                if (this.isConnected == false) throw new InvalidCastException("Socket n'est pas connecter au point d'accès.");
                this._send(message);
                this._print(this._Onmessage());
            }
            catch (InvalidCastException e)
            {
                this._OnError("InvalidCastException", e.Message);
            }

            this._close();
        }

        /*
         * @{name}      Close
         * @{type}      public void
         * @{desc}      
         */
        public void Close()
        {
            try
            {
                if (this.isConnected == false) throw new InvalidCastException("Socket n'est pas connecter au point d'accès.");
                this._close();
            }
            catch (InvalidCastException e)
            {
                this._OnError("InvalidCastException", e.Message);
            }
        }

        public void OnError(string? type, string message)
        {
            this._OnError(type, message);
        }

        /*
         * @{name}      _normalize
         * @{type}      private byte[]
         * @{return}    byte[]
         * @{desc}      Processus de normalisation d'un string en tableau de byte
         */
        private byte[] _normalize(string message)
        {
            return Encoding.ASCII.GetBytes(message + "<EOF>");
        }

        /*
         * @{name}      _setEndpoint
         * @{type}      private void
         * @{desc}      Établissez le point de terminaison distant pour le socket. Cet exemple utilise le port 11111 sur l’ordinateur local.
         *              Modifie en interne les valeurs de ipHost , ipAddr , localEndPoint
         */
        private void _setEndpoint()
        {
            //if(this.ipv6 != null) Console.WriteLine(IPAddress.Parse(this.ipv6));
            this.ipHost = (this.ipv6 == null ? Dns.GetHostEntry(Dns.GetHostName()) : Dns.GetHostEntry(IPAddress.Parse(this.ipv6)));
            this.ipAddr = (this.ipv6 == null ? ipHost.AddressList[0] : IPAddress.Parse(this.ipv6));
            this.localEndPoint = new IPEndPoint(this.ipAddr, 11111);
        }

        /*
         * @{name}      _setListener
         * @{type}      private void
         * @{desc}      Création d’un socket TCP/IP à l’aide du constructeur de la classe socket.
         */
        private void _setSender()
        {
            this.sender = new Socket(this.ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        /*
         * @{name}      _connect
         * @{type}      private void
         * @{desc}      Connection du Socket au point de terminaison distant à l’aide de la méthode Connect()
         */
        private void _connect()
        {
            try
            {
                this.sender.Connect(this.localEndPoint);
                this._print("Socket connected to : " + this.sender.RemoteEndPoint.ToString()); // Nous imprimons des informations EndPoint à laquel nous sommes connectés
                this._switchConnectionState();
            }
            catch (ArgumentNullException ane)
            {
                this._OnError("ArgumentNullException", ane.Message);
            }

            catch (SocketException se)
            {
                this._OnError("SocketException", se.Message);
            }

            catch (Exception e)
            {
                this._OnError("Exception", e.Message);
            }
        }

        /*
         * @{name}      Send
         * @{type}      private void
         * @{desc}      Envois d'un message au serveur
         */
        private void _send(string message)
        {
            int byteSent = this.sender.Send(this._normalize(message));
        }

        private string _Onmessage()
        {
            // Data buffer
            byte[] messageReceived = new byte[1024];
            int byteRecv = this.sender.Receive(messageReceived);
            return Encoding.ASCII.GetString(messageReceived, 0, byteRecv);
        }

        private void _print(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{this.ipHost.HostName} ");
            Console.ResetColor();
            Console.Write(message);
            Console.WriteLine("");
        }

        /*
         * @{name}      _execClient
         * @{type}      private void
         * @{desc}      Établissez le point de terminaison distant pour le socket. Cet exemple utilise le port 11111 sur l’ordinateur local.
         *              Modifie en interne les valeurs de ipHost , ipAddr , localEndPoint
         */
        private void _execClient()
        {
            try
            {
                this._connect();                        // connexion au endpoint
                this._send("C'est un test les kheys");   // Envois d'un message
                this._print(this._Onmessage());         // impression dans la console du message de reçeption
                this._close();                          // fermeture du socket
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

        /*
         * @{name}      _close
         * @{type}      private void
         * @{desc}      Close Socket à l’aide de la méthode Close()
         */
        private void _close()
        {
            try
            {
                this.sender.Shutdown(SocketShutdown.Both);
                this.sender.Close();
                this._switchConnectionState();
            }
            catch (SocketException e)
            {
                this._OnError("SocketException" , e.Message);
            }
        }

        private void _switchConnectionState()
        {
            this.isConnected = (this.isConnected == false ? true : false);
        }

        private void _OnError(string? type , string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{this.ipHost.HostName}-");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{type}");
            Console.ResetColor();
            Console.Write($" {message}");
            Console.WriteLine("");
        }

    }
}
