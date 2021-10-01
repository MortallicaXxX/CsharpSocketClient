using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;

namespace socketCsharp
{

    class Message
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
                M m = JsonSerializer.Deserialize<M>(message) ;
                this._chanel = m.chanel;
                this._message = m.data;
            }
            catch(Exception e)
            {
                this._message = message;
            }
        }
    }

    class socketChanels
    {

        private List<Func<dynamic , Message , bool>> callBacks = new List<Func<dynamic , Message , bool>>();
        private List<string> chanelsNames = new List<string>();
        private dynamic root;

        public socketChanels(dynamic root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public List<string> Chanels { get { return chanelsNames; } }

        public void Exec(Message message)
        {
            try
            {
                if ((message.chanel != null) && (this.chanelsNames.Contains(message.chanel))) _exec(message);
                else throw new Exception("Ce chanel n'est pas définis");
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Add(string chanelName, Func<dynamic , Message , bool> callback)
        {
            this._add(chanelName,callback);
        }

        public bool Delete(string chanelName)
        {
            int x = this._getIdFromName(chanelName);
            if(x > -1)
            {
                this.callBacks.RemoveAt(x);
                this.chanelsNames.RemoveAt(x);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void _add(string chanelName , Func<dynamic , Message , bool> callback)
        {
            if(this._isChanelExist(chanelName) == false)
            {
                this.callBacks.Add(callback);
                this.chanelsNames.Add(chanelName);
            }
        }

        private bool _isChanelExist(string chanelName)
        {
            return this.chanelsNames.Contains(chanelName);
        }

        private int _getIdFromName(string name)
        {
            for (int i = 0; i < this.chanelsNames.Count; i++)
            {
                if (this.chanelsNames[i] == name) return i;
            }
            return -1;
        }

        private Func<dynamic , Message , bool> _getCallBackFromName(string name)
        {
            for(int i = 0; i < this.chanelsNames.Count; i++)
            {
                if (this.chanelsNames[i] == name) return this.callBacks[i];
            }
            return null;
        }

        private void _exec(Message message)
        {
            this.callBacks[this._getIdFromName(message.chanel)](this.root , message);
        }

    }

    class socketSubscription
    {

        private List<socketEmitor> _subcriptions = new List<socketEmitor>();

        public socketSubscription()
        {

        }

        public void Add(string ipv6)
        {
            this._subcriptions.Add(new socketEmitor(ipv6));
        }

        public void Remove(string subIp)
        {
            int x = _subByIpv6(subIp);
            if (x > -1)
            {
                this._subcriptions.RemoveAt(x);
            }
        }

        private int _subByIpv6(string subIpv6)
        {
            for(int i = 0; i < this._subcriptions.Count; i++)
            {
                if (subIpv6 == this._subcriptions[i].IP().ToString()) return i;
            }
            return -1;
        }

        public void SendToAll(socketCaptor cli , string message)
        {
            foreach (socketEmitor se in this._subcriptions) se.Send(message);
        }

        public void SendToAllOn(socketCaptor cli , string chanel , string message)
        {
            foreach (socketEmitor se in this._subcriptions) se.SendOn(chanel, message);
        }
    }

    // class corespondant au module capable de capter les message et de répondre
    class socketCaptor
    {
        private int port;                   //
        private IPHostEntry ipHost;         //
        private IPAddress ipAddr;           //
        private IPEndPoint localEndPoint;   //
        private Socket listener;            //

        private Task serverTask;            // Threading Task du serveur, pour qu'il ne bloque pas le processus
        private Socket clientSocket;        //

        private int nbrUsers;               // nombre d'utilisateur max présent dans la liste Socket.Listen()

        private socketChanels chanels;

        private socketSubscription _subscriptions = new socketSubscription();

        /* @{name}      socketEmitor
         * @{type}      public constructor
         * @{desc}      Constructeur de la class socketEmitor
         * @{params}
         *      int? {nbrUsers} peut être null . SI null, par défaut 10
         *      int? {port}     peut être null . SI null, par défaut 11111
         */
        public socketCaptor(int? nbrUsers = null, int? port = null)
        {
            this.nbrUsers = (nbrUsers != null ? (int)nbrUsers : 10);
            this.port = (port != null ? (int)port : 11111);
            this.chanels = new socketChanels(this);
            this._setEndpoint();
            this._setListener();
            this._setHandlers();
            this.serverTask = new Task(this._execServer);
        }

        public socketChanels Chanels(){ return this.chanels; }

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
        public socketCaptor Start()
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

        /*
         * @{name}      On
         * @{type}      public int
         */
        public void On(string chanel, Func<dynamic , Message , bool> callback)
        {
            this.chanels.Add(chanel, callback);
        }

        public bool Reply(string message)
        {
            this._send(message);
            this._close();
            return true;
        }

        public void Emit(string message)
        {
            this._subscriptions.SendToAll(this,message);
        }

        public void EmitOn(string chanel, string message)
        {
            this._subscriptions.SendToAllOn(this, chanel , message);
        }

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

                data += Encoding.ASCII.GetString(bytes,0, numByte);

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
        private void _print(Message data)
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


    }

    // class corespondant au module capable de d'envoyer un message et de traiter la réponse
    class socketEmitor
    {
        private string ipv6;
        private IPHostEntry ipHost;
        private IPAddress ipAddr;
        private IPEndPoint localEndPoint;
        private Socket sender;

        private bool isConnected = false;

        private socketChanels chanels;

        public socketEmitor(string? ipv6 = null) {
            this.ipv6 = ipv6;
            this.chanels = new socketChanels(this);
        }

        public IPAddress IP()
        {
            return this.ipAddr;
        }

        public socketEmitor Subscribe(string? ipv6 = null)
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
                Console.WriteLine(e);
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
                Console.WriteLine(e);
            }
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
            this.sender.Shutdown(SocketShutdown.Both);
            this.sender.Close();
            this._switchConnectionState();
        }

        private void _switchConnectionState()
        {
            this.isConnected = (this.isConnected == false ? true : false);
        }

    }

    class Program
    {

        /*Exemple de création statique d'une fonction utilisée comme callback à un chanel*/
        private static bool Test1(dynamic cli, Message arg)
        {
            return cli.Reply("C'est ma reponse");
        }

        static void Main(string[] args)
        {

            ////////////////////////
            ///////EXEMPLES/////////
            ////////////////////////


            ////////////////////////
            /////SOCKETCAPTOR///////
            ////////////////////////

            /*instancie et démarre le serveur sur une seule ligne | peut se faire sur deux ligne*/
            socketCaptor sc = new socketCaptor(11111).Start();

            /*Affiche les infos du serveur*/
            sc.Infos();

            ////////////////////////
            ////////CHANELS/////////
            ////////////////////////

            /*création statique d'un chanel lié à un callback*/
            sc.On("Test1", Test1);

            /*création dynamique d'un chanel lié à un callback*/
            sc.On("Test2", (dynamic cli, Message arg) =>
            {
                return cli.Reply("lol");
            });

            /*Exemple de suppression - /!\ CREER UNE ERREUR /!\*/
            //se.Chanels().Delete("Test");

            ////////////////////////
            /////SOCKETEMITOR///////
            ////////////////////////

            /*Instancie et Souscris l'émeteur à un serveur | peut se faire sur deux ligne*/
            socketEmitor se = new socketEmitor().Subscribe(/*Dois Contenir une adresse IPV6*/);

            /*Affiche les infos de l'émeteur*/
            se.Infos();

            ////////////////////////
            ////////MESSAGES////////
            ////////////////////////

            /*Envois d'un premier message sur le chanel général*/
            se.Send("Test Data");
            /*Envois d'un deuxième message sur le chanel général*/
            se.Send("Test Data2");
            /*Envois d'un message sur un chanel particulier*/
            se.SendOn("Test1", "helloworld");

            ////////////////////////
            //////////EMIT//////////
            ////////////////////////

            /*Envois d'une information à tout utilisateur inscrit sur le serveur*/
            sc.Emit("Hello Tout le monde");
            /*Envois d'une information à sur le chanel correspondant à chaque utilisateur inscrit sur le serveur*/
            sc.EmitOn("Test2","Salut tout le monde!");

            Console.ReadKey();

        }
    }
}
