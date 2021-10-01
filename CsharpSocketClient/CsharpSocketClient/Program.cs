using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;

namespace socketCsharp
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
            this.serverTask = new Task(this._execServer);
        }

        public socketChanels Chanels(){ return this.chanels; }

        /*
         * @{name}      Start
         * @{type}      public void
         * @{desc}      Permet de démarrer le serveur sur un thread.
         */
        public void Start()
        {
            this.serverTask.Start();
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
            Console.WriteLine((data.chanel == null ? $"General chanel receive : {data.message}" : $"{data.chanel} chanel recive : {data.message}"));
            Console.ResetColor();
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

        public void Connect()
        {
            this._connect();
        }

        public void SendOn(string chanelName, string message)
        {
            this.Send($"{{" +
                $"\"chanel\" : \"{chanelName}\"," +
                $"\"data\" : \"{message}\"" +
                $"}}");
        }

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
            if(this.ipv6 != null) Console.WriteLine(IPAddress.Parse(this.ipv6));
            this.ipHost = (this.ipv6 == null ? Dns.GetHostEntry(Dns.GetHostName()) : Dns.GetHostEntry(IPAddress.Parse(this.ipv6)));
            Console.WriteLine(ipHost.AddressList.Length);
            Console.WriteLine(ipHost.AddressList[0]);
            this.ipAddr = (this.ipv6 == null ? ipHost.AddressList[0] : IPAddress.Parse(this.ipv6));
            this.localEndPoint = new IPEndPoint(ipAddr, 11111);
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
            Console.WriteLine(message);
            Console.ResetColor();
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

        private static bool Test(dynamic cli , Message arg)
        {
            return cli.Reply("C'est ma reponse");
        }

        // Main Method
        static void Main(string[] args)
        {

            socketCaptor se = new socketCaptor(11111);
            Console.WriteLine(se.IP());     // affiche l'ip sur lequel tourne le serveur
            Console.WriteLine(se.Port());   // affiche le port sur lequel tourne le serveur

            se.On("Test", Test);            // création d'un chanel lié à un callback
            se.Chanels().Delete("Test");    // suppersion du chanel

            se.Start();                     // Démare le serveur Socket

            socketEmitor sc = new socketEmitor("fe80::45f4:89d7:78f4:68fb");
            sc.Send("Test Data");           // Envois d'un premier message
            sc.Send("Test Data2");          // Envois d'un deuxième message

            // Erreur sur l'appel d'un chanel non existant - serveur se bloque A REGLER
            //sc.SendOn("Test", "helloworld");// Envois d'un troisième message sur un chanel - Erreur puisque le chanel à été supprimer

            se.On("Test", Test);            // création d'un chanel lié à un callback
            sc.SendOn("Test", "helloworld");// Envois d'un troisième message sur un chanel - Succes puisque le chanel à été recréer

            Console.ReadKey();

        }
    }
}
