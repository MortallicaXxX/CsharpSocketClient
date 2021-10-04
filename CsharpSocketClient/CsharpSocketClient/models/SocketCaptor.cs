﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using CsharpSocketClient.models;

namespace CsharpSocketClient.models
{
    class SocketCaptor
    {
        private int port;                   //
        private IPHostEntry ipHost;         //
        private IPAddress ipAddr;           //
        private IPEndPoint localEndPoint;   //
        private Socket listener;            //

        private Task serverTask;            // Threading Task du serveur, pour qu'il ne bloque pas le processus
        private Socket clientSocket;        //

        private int nbrUsers;               // nombre d'utilisateur max présent dans la liste Socket.Listen()

        private SocketChanels chanels;

        private SocketSubscription _subscriptions = new SocketSubscription();

        ////////////////////////
        /////////PUBLIC/////////
        ////////////////////////

        /* @{name}      socketEmitor
         * @{type}      public constructor
         * @{desc}      Constructeur de la class socketEmitor
         * @{params}
         *      int? {nbrUsers} peut être null . SI null, par défaut 10
         *      int? {port}     peut être null . SI null, par défaut 11111
         */
        public SocketCaptor(int? nbrUsers = null, int? port = null)
        {
            this.nbrUsers = (nbrUsers != null ? (int)nbrUsers : 10);
            this.port = (port != null ? (int)port : 11111);
            this.chanels = new SocketChanels(this);
            this._setEndpoint();
            this._setListener();
            this._setHandlers();
            this.serverTask = new Task(this._execServer);
        }

        public SocketChanels Chanels() { return this.chanels; }

        /*
         * @{name}      Infos
         * @{type}      public void
         * @{desc}      
         */
        public void Infos()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"socketCaptor server is running on {this.IP()}:{this.Port()}");
            Console.ResetColor();
        }

        /*
         * @{name}      Start
         * @{type}      public void
         * @{desc}      Permet de démarrer le serveur sur un thread.
         */
        public SocketCaptor Start()
        {
            this.serverTask.Start();
            return this;
        }

        /*
          * @{name}      IP
          * @{type}      public IPAddress
          */
        public IPAddress IP()
        {
            return this.ipAddr;
        }

        /*
          * @{name}      Port
          * @{type}      private void
          */
        public int Port()
        {
            return this.port;
        }

        public void Subscribe(string ipv6)
        {
            this._subscriptions.Add(ipv6);
        }

        public void Unsubscribe(string ipv6)
        {
            this._subscriptions.Remove(ipv6);
        }

        public void OnError(string? type , dynamic error)
        {
            this._OnError(type, error);
        }

        /*
         * @{name}      On
         * @{type}      public int
         */
        public void On(string chanel, Func<dynamic, Message, bool> callback)
        {
            this.chanels.Add(chanel, callback);
        }

        public bool Reply(string message)
        {
            this._send($"{{" +
                $"\"chanel\" : \"reply\"," +
                $"\"data\" : \"{message}\"" +
                $"}}");
            this._close();
            return true;
        }

        public void Emit(string message)
        {
            this._subscriptions.SendToAll(this, message);
        }

        public void EmitOn(string chanel, string message)
        {
            this._subscriptions.SendToAllOn(this, chanel, message);
        }

        ////////////////////////
        /////////Private////////
        ////////////////////////

        /*
         * @{name}      _setEndpoint
         * @{type}      private void
         * @{desc}      Définition du "endpoint" de l'émeteur. Cet exemple utilise le port 11111 sur l’ordinateur local.
         *              Modifie en interne les valeurs de ipHost , ipAddr , localEndPoint
         */
        private void _setEndpoint()
        {
            this.ipHost = Dns.GetHostEntry(Dns.GetHostName());
            this.ipAddr = ipHost.AddressList[0];
            this.localEndPoint = new IPEndPoint(ipAddr, this.port);
        }

        /*
         * @{name}      _setListener
         * @{type}      private void
         */
        private void _setListener()
        {
            this.listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        private void _setHandlers()
        {
            Func<dynamic, Message, bool> Subscribe = (dynamic cli, Message arg) =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write($"{this.ipHost.HostName}-");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write((arg.chanel == null ? $"general" : $"{arg.chanel}"));
                Console.ResetColor();
                Console.Write($" : {arg.message}");
                Console.WriteLine();

                cli.Subscribe(arg.message);
                return cli.Reply("Souscris");
            };

            Func<dynamic, Message, bool> Unsubscribe = (dynamic cli, Message arg) =>
            {
                Console.WriteLine($"Delete d'abonement {arg.message}");
                cli.Unsubscribe(arg.message);
                return cli.Reply("Désouscris");
            };

            this.On("subscribe", Subscribe);            // création d'un chanel lié à un callback
            this.On("unsubscribe", Unsubscribe);            // création d'un chanel lié à un callback
        }

        /*
         * @{name}      _send
         * @{type}      private void
         * @{desc}      Envois d'un message au client à l’aide de la méthode Send()
         */
        private void _send(string message)
        {
            this.clientSocket.Send(Encoding.ASCII.GetBytes(message));
        }

        /*
         * @{name}      _result
         * @{type}      private void
         * @{desc}      WriteLine du résultat de la donnée reçue
         */
        private Message _Onmessage()
        {
            // Data buffer
            byte[] bytes = new Byte[1024];
            string data = null;

            while (true)
            {

                int numByte = this.clientSocket.Receive(bytes);

                data += Encoding.ASCII.GetString(bytes, 0, numByte);

                if (data.IndexOf("<EOF>") > -1)
                    break;
            }

            return new Message(data.Split("<EOF>")[0]);
        }

        /*
         * @{name}      _print
         * @{type}      private void
         * @{desc}      Affichage en bleu d'un message dans la console.
         */
        private async void _print(Message data)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"{this.ipHost.HostName}-");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write((data.chanel == null ? $"general" : $"{data.chanel}"));
            Console.ResetColor();
            Console.Write($" : {data.message}");
            Console.WriteLine();
            if (data.chanel != null) this.chanels.Exec(data);
            else
            {
                this._send("true");
                this._close();
            }
        }

        /*
         * @{name}      _execServer
         * @{type}      private void
         */
        private void _execServer()
        {
            try
            {
                // En utilisant la méthode Bind(), nous associons une adresse réseau au socket serveur.
                // Tout le client qui se connectera à ce socket serveur doit connaître cette adresse réseau.
                this.listener.Bind(this.localEndPoint);

                // En utilisant la méthode Listen(), nous créons la liste des clients qui voudront se connecter au serveur.
                this.listener.Listen(this.nbrUsers);

                this._accept();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void _accept()
        {
            while (true)
            {
                // Attente de la connexion entrante. À l’aide de la méthode Accept(), le serveur acceptera la connexion du client.
                this.clientSocket = this.listener.Accept();

                // Affichage du message entrant
                this._print(this._Onmessage());
            }
        }

        /*
         * @{name}      _close
         * @{type}      private void
         * @{desc}      Fermez le socket client à l’aide de la méthode Close().
         *              Après la fermeture, nous pouvons utiliser le socket fermé pour une nouvelle connexion client
         */
        private void _close()
        {
            this.clientSocket.Shutdown(SocketShutdown.Both);
            this.clientSocket.Close();
        }

        private void _OnError(string? type, dynamic error)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{this.ipHost.HostName}-");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{type}");
            Console.ResetColor();
            Console.Write($" {error.Message}");
            Console.WriteLine("");
        }

    }
}