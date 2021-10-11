using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

using CsharpSocketClient.models;

namespace CsharpSocketClient.models
{
    class SocketCaptor
    {
        private int port;                   //
        private IPHostEntry ipHost;         //
        private IPAddress ipAddr;           //
        private IPEndPoint localEndPoint;   //

        private Socket socketListener;              //
        private TcpListener tcpListener;            //

        private Task serverTask_socket;             // Threading Task du serveur, pour qu'il ne bloque pas le processus
        private Task serverTask_tcp;                // Threading Task du serveur, pour qu'il ne bloque pas le processus
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
            this.port = (port != null ? (int)port : 8080);
            this.chanels = new SocketChanels(this);
            this._setEndpoint();
            this._setSocketListener();
            this._setTCPListener();
            this._setHandlers();
            this.serverTask_socket = new Task(this._execSocketServer);
            this.serverTask_tcp = new Task(this._execTcpServer);
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
            Debug.WriteLine($"socketCaptor server is running on {this.IP()}:{this.Port()}");
            Console.ResetColor();
        }

        /*
         * @{name}      Start
         * @{type}      public void
         * @{desc}      Permet de démarrer le serveur sur un thread.
         */
        public SocketCaptor Start()
        {
            this.serverTask_socket.Start();
            this.serverTask_tcp.Start();
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
            this.ipAddr = ipHost.AddressList[1];
            this.localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
        }

        /*
         * @{name}      _setListener
         * @{type}      private void
         */
        private void _setSocketListener()
        {
            this.socketListener = new Socket(IPAddress.Parse("127.0.0.1").AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        private void _setTCPListener()
        {
            this.tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 3000);
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

                Debug.Write($"{this.ipHost.HostName}-");
                Debug.Write((arg.chanel == null ? $"general" : $"{arg.chanel}"));
                Debug.Write($" : {arg.message}");
                Debug.WriteLine("");

                cli.Subscribe(arg.message);
                return cli.Reply("Souscris");
            };

            Func<dynamic, Message, bool> Unsubscribe = (dynamic cli, Message arg) =>
            {
                Console.WriteLine($"Delete d'abonement {arg.message}");
                Debug.WriteLine($"Delete d'abonement {arg.message}");
                cli.Unsubscribe(arg.message);
                return cli.Reply("Désouscris");
            };

            Func<dynamic, Message, bool> Ping = (dynamic cli, Message arg) =>
            {
                return cli.Reply($"ping - {Encoding.UTF8.GetByteCount(arg.message).ToString()} byte recive in {(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds - arg.eventTime} ms");
            };

            this.On("subscribe", Subscribe);
            this.On("unsubscribe", Unsubscribe);
            this.On("ping", Ping);
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
        private Message _OnIncome()
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
        private async void _treat(Message data)
        {
            if (data.chanel != null) this.chanels.Exec(data);
            else
            {
                this._send("true");
                this._close();
            }
        }

        private void _execTcpServer()
        {
            try
            {
                this.tcpListener.Start();
                TcpClient client = this.tcpListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                Console.WriteLine("Connect");

                while (true)
                {
                    while (!stream.DataAvailable) ;
                    while (client.Available < 3) ; // match against "get"

                    byte[] bytes = new byte[client.Available];
                    stream.Read(bytes, 0, client.Available);
                    string s = Encoding.UTF8.GetString(bytes);

                    if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                    {
                        Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                        // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                        // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                        // 3. Compute SHA-1 and Base64 hash of the new value
                        // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                        string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                        string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                        byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                        string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                        // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                        byte[] response = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 101 Switching Protocols\r\n" +
                            "Connection: Upgrade\r\n" +
                            "Upgrade: websocket\r\n" +
                            "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                        stream.Write(response, 0, response.Length);
                    }
                    else
                    {
                        bool fin = (bytes[0] & 0b10000000) != 0,
                            mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                        int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                            msglen = bytes[1] - 128, // & 0111 1111
                            offset = 2;

                        if (msglen == 126)
                        {
                            // was ToUInt16(bytes, offset) but the result is incorrect
                            msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                            offset = 4;
                        }
                        else if (msglen == 127)
                        {
                            Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                            // i don't really know the byte order, please edit this
                            // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                            // offset = 10;
                        }

                        if (msglen == 0)
                            Console.WriteLine("msglen == 0");
                        else if (mask)
                        {
                            byte[] decoded = new byte[msglen];
                            byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                            offset += 4;

                            for (int i = 0; i < msglen; ++i)
                                decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                            string text = Encoding.UTF8.GetString(decoded);
                            this._OnMessage(new Message(text));
                        }
                        else
                            Console.WriteLine("mask bit not set");

                        Console.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /*
         * @{name}      _execServer
         * @{type}      private void
         */
        private void _execSocketServer()
        {
            try
            {
                // En utilisant la méthode Bind(), nous associons une adresse réseau au socket serveur.
                // Tout le client qui se connectera à ce socket serveur doit connaître cette adresse réseau.
                this.socketListener.Bind(this.localEndPoint);

                // En utilisant la méthode Listen(), nous créons la liste des clients qui voudront se connecter au serveur.
                this.socketListener.Listen(this.nbrUsers);
                this._accept();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void _accept()
        {
            Console.WriteLine("Waiting for connexion...");
            while (true)
            {
                // Attente de la connexion entrante. À l’aide de la méthode Accept(), le serveur acceptera la connexion du client.
                this.clientSocket = this.socketListener.Accept();
                Message income = this._OnIncome();
                // Affichage du message entrant
                this._treat(income);
                this._OnMessage(income);
                Console.WriteLine("Waiting for connexion...");
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

        private void _OnMessage(Message arg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"{this.ipHost.HostName}-");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write((arg.chanel == null ? $"general" : $"{arg.chanel}"));
            Console.ResetColor();
            Console.Write($" : {arg.message}");
            Console.WriteLine();

            Debug.Write($"{this.ipHost.HostName}-");
            Debug.Write((arg.chanel == null ? $"general" : $"{arg.chanel}"));
            Debug.Write($" : {arg.message}");
        }

        private void _OnError(string? type, dynamic error)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{this.ipHost.HostName}-");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{type} ");
            Console.Write($"{error.StackTrace.Split("\\")[error.StackTrace.Split("\\").Length - 1].Split(":line")[0]}:");
            Console.Write($"{error.StackTrace.Split(":line ")[error.StackTrace.Split(":line ").Length - 1]} ");
            Console.ResetColor();
            Console.Write($" {error.Message}");
            Console.WriteLine("");

            Debug.Write($"{this.ipHost.HostName}-");
            Debug.Write($"{type} ");
            Debug.Write($"{error.StackTrace.Split("\\")[error.StackTrace.Split("\\").Length - 1].Split(":line")[0]}:");
            Debug.Write($"{error.StackTrace.Split(":line ")[error.StackTrace.Split(":line ").Length - 1]} ");
            Debug.Write($" {error.Message}");
        }

    }
}
