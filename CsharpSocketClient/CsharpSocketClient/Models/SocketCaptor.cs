using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CsharpSocketClient.Models
{
    public class SocketCaptor
    {
        public int Port { get; set; }
        public int NbUsers { get; set; }
        public IPHostEntry IPHostEntry { get; set; }
        public IPAddress IPAddress { get; set; }
        public IPEndPoint IPEndPoint { get; set; }
        public Socket Listener { get; set; }
        public Task ServerTask { get; set; }
        public Socket ClientSocket { get; set; }
        public SocketChanel Channels { get; set; }
        public SocketSubscription Subscription { get; set; }

        public SocketCaptor(int? nbUsers = null, int? port = null)
        {
            NbUsers = nbUsers != null ? (int)nbUsers : 10;
            Port = port != null ? (int)port : 11111;
            Channels = new SocketChanel(this);
            SetEndpoint();
            SetListener();
            SetHandlers();
            ServerTask = new Task(ExecServer);
            Subscription = new SocketSubscription();
        }

        public void Infos()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"socketCaptor server is running on {IPAddress}:{Port}");
            Console.ResetColor();
        }

        public SocketCaptor Start()
        {
            ServerTask.Start();
            return this;
        }

        public void Subscribe(string ipv6)
        {
            Subscription.Add(ipv6);
        }

        public void Unsubscribe(string ipv6)
        {
            Subscription.Remove(ipv6);
        }

        public void On(string chanel, Func<dynamic, SocketMessage, bool> callback)
        {
            Channels.Add(chanel, callback);
        }

        public bool Reply(string message)
        {
            _send(message);
            _close();
            return true;
        }

        public void Emit(string message)
        {
            Subscription.SendToAll(this, message);
        }

        public void EmitOn(string chanel, string message)
        {
            Subscription.SendToAllOn(this, chanel, message);
        }

        private void SetEndpoint()
        {
            IPHostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress = IPHostEntry.AddressList[0];
            IPEndPoint = new IPEndPoint(IPAddress, Port);
        }

        private void SetListener()
        {
            Listener = new Socket(IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        private void SetHandlers()
        {
            Func<dynamic, SocketMessage, bool> Subscribe = (dynamic cli, SocketMessage arg) =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write($"{IPHostEntry.HostName}-");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write((arg.Channel == null ? $"general" : $"{arg.Channel}"));
                Console.ResetColor();
                Console.Write($" : {arg.Message}");
                Console.WriteLine();

                cli.Subscribe(arg.Message);
                return cli.Reply("Souscris");
            };

            Func<dynamic, SocketMessage, bool> Unsubscribe = (dynamic cli, SocketMessage arg) =>
            {
                Console.WriteLine($"Delete d'abonement {arg.Message}");
                cli.Unsubscribe(arg.Message);
                return cli.Reply("Désouscris");
            };

            On("subscribe", Subscribe);            // création d'un chanel lié à un callback
            On("unsubscribe", Unsubscribe);            // création d'un chanel lié à un callback
        }

        private void _send(string message)
        {
            ClientSocket.Send(Encoding.ASCII.GetBytes(message));
        }

        private SocketMessage _Onmessage()
        {
            // Data buffer
            byte[] bytes = new Byte[1024];
            string data = null;

            while (true)
            {

                int numByte = ClientSocket.Receive(bytes);

                data += Encoding.ASCII.GetString(bytes, 0, numByte);

                if (data.IndexOf("<EOF>") > -1)
                    break;
            }

            return new SocketMessage(data.Split("<EOF>")[0]);
        }

        private void _print(SocketMessage data)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"{IPHostEntry.HostName}-");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write((data.Channel == null ? $"general" : $"{data.Channel}"));
            Console.ResetColor();
            Console.Write($" : {data.Message}");
            Console.WriteLine();
            if (data.Channel != null) Channels.Exec(data);
            else
            {
                _send("true");
                _close();
            }
        }

        private void ExecServer()
        {
            try
            {
                // En utilisant la méthode Bind(), nous associons une adresse réseau au socket serveur.
                // Tout le client qui se connectera à ce socket serveur doit connaître cette adresse réseau.
                Listener.Bind(IPEndPoint);

                // En utilisant la méthode Listen(), nous créons la liste des clients qui voudront se connecter au serveur.
                Listener.Listen(NbUsers);

                _accept();

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
                ClientSocket = Listener.Accept();

                // Affichage du message entrant
                _print(_Onmessage());
            }
        }

        private void _close()
        {
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
        }
    }
}
