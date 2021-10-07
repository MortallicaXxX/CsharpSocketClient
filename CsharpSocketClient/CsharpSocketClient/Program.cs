using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using CsharpSocketClient.Models;

namespace socketCsharp
{
    class Program
    {
        static void Main(string[] args)
        {
            ////////////////////////
            /////SOCKETCAPTOR///////
            ////////////////////////

            /*instancie et démarre le serveur sur une seule ligne | peut se faire sur deux ligne*/
            SocketCaptor sc = new SocketCaptor(11111).Start();

            /*Affiche les infos du serveur*/
            sc.Infos();

            ////////////////////////
            ////////CHANELS/////////
            ////////////////////////

            /*création statique d'un chanel lié à un callback*/
            sc.On("Test1", Test1);

            /*création dynamique d'un chanel lié à un callback*/
            sc.On("Test2", (dynamic cli, SocketMessage arg) =>
            {
                return cli.Reply("lol");
            });

            /*Exemple de suppression - /!\ CREER UNE ERREUR /!\*/
            //se.Chanels().Delete("Test");

            ////////////////////////
            /////SOCKETEMITOR///////
            ////////////////////////

            /*Instancie et Souscris l'émeteur à un serveur | peut se faire sur deux ligne*/
            SocketEmitor se = new SocketEmitor().Subscribe(/*Dois Contenir une adresse IPV6*/);

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

        /// <summary>
        /// Exemple de création statique d'une fonction utilisée comme callback à un chanel
        /// </summary>
        /// <param name="cli"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static bool Test1(dynamic cli, SocketMessage arg)
        {
            return cli.Reply("C'est ma reponse");
        }
    }
}
