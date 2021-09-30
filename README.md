# CsharpSocketClient
Socket Client pour C#

CsharpSocketClient c'est quoi ?
Une bibliothèque de classes.

Documentation :

classe socketChanels
    socketChanels
Méthodes :



classe socketCaptor

    socketCaptor est le serveur socket.
    Il permet de créer un serveur afin d'y faire transiter des données en temps réel.

Variables :

    - private int port;
    - private IPHostEntry ipHost;
    - private IPAddress ipAddr;
    - private IPEndPoint localEndPoint;
    - private Socket listener;
    - private Task serverTask;
    - private Socket clientSocket;
    - private int nbrUsers;
    - private socketChanels chanels;
    
Méthodes :

    - public void Start() Permet de démarrer le serveur sur un thread.
    - public IPAddress IP()
    - public int Port()

    - public void On(string chanel, Func<dynamic> callback)
    - private void _setEndpoint() Définition du "endpoint" de l'émeteur. Cet exemple utilise le port 11111 sur l’ordinateur local.
    - private void _setListener()
    - private void _send(string message) Envois d'un message au client à l’aide de la méthode Send()
    - private string _Onmessage()
    - private void _print(string message) Affichage en bleu d'un message dans la console.
    - private void _execServer()
    - private void _close() Fermez le socket client à l’aide de la méthode Close().



classe socketCaptor

    socketCaptor est le client socket.
    Il permet de créer un client afin d'envoyer des messages en temps réel à un serveur.

Variables :

    - private IPHostEntry ipHost;
    - private IPAddress ipAddr;
    - private IPEndPoint localEndPoint;
    - private Socket sender;
    - private bool isConnected = false;

Méthodes :

    - public void Connect()
    - public void Send(string message)
    - public void Close()

    - private byte[] _normalize(string message) Processus de normalisation d'un string en tableau de byte
    - private void _setEndpoint() Établissez le point de terminaison distant pour le socket. Cet exemple utilise le port 11111 sur l’ordinateur local.
    - private void _setSender() Création d’un socket TCP/IP à l’aide du constructeur de la classe socket.
    - private void _connect() Connection du Socket au point de terminaison distant à l’aide de la méthode Connect()
    - private void _send(string message) Envois d'un message au serveur
    - private string _Onmessage()
    - private void _print(string message)
    - private void _execClient()
    - private void _close() Close Socket à l’aide de la méthode Close()
    - private void _switchConnectionState()
