using System;
using CsharpSocketClient.models;

namespace socketCsharp
{

    class Program
    {
        /*Exemple de création statique d'une fonction utilisée comme callback à un chanel*/
        private static bool Test1(dynamic cli, Message arg)
        {
            return cli.Reply("Reply callback_Test1");
        }

        static void Main(string[] args)
        {

            ////////////////////////
            ///////EXEMPLES/////////
            ////////////////////////

            SocketRouter router = new SocketRouter().Start().Subscribe("fe80::fdba:3812:1a1f:5d7b");

            router.Infos();

            //router.Test();

/*            router.On("callback_Test1", Test1);

            router.On("callback_Test2", (dynamic cli, Message arg) =>
            {
                return cli.Reply("Reply callback_Test2");
            });

            router.On("callback_Test3", (dynamic cli, Message arg) =>
            {
                return cli.Reply("Reply callback_Test3");
            });

            router.On("callback_Test4", (dynamic cli, Message arg) =>
            {
                return cli.Reply("Reply callback_Test4");
            });*/

            /*            router.Emit("RouteurTest send Emit");
                        router.EmitOn("callback_Test3" , "RouteurTest send Emit on chanel");*/


            ////////////////////////
            /////SOCKETCAPTOR///////
            ////////////////////////

            /*instancie et démarre le serveur sur une seule ligne | peut se faire sur deux ligne*/
            //SocketCaptor sc = new SocketCaptor(11111).Start();

            /*Affiche les infos du serveur*/
            //sc.Infos();

            ////////////////////////
            ////////CHANELS/////////
            ////////////////////////

            /*création statique d'un chanel lié à un callback*/
            //sc.On("Test1", Test1);

            /*création dynamique d'un chanel lié à un callback*/
            /*sc.On("Test2", (dynamic cli, Message arg) =>
            {
                return cli.Reply("lol");
            });*/

            /*Exemple de suppression - /!\ CREER UNE ERREUR /!\*/
            //sc.Chanels().Delete("Test1");

            ////////////////////////
            /////SOCKETEMITOR///////
            ////////////////////////

            /*Instancie et Souscris l'émeteur à un serveur | peut se faire sur deux ligne*/
            //SocketEmitor se = new SocketEmitor().Subscribe(/*Dois Contenir une adresse IPV6*/ /*"fe80::fdba:3812:1a1f:5d7b"*/);

            /*Affiche les infos de l'émeteur*/
            //se.Infos();

            ////////////////////////
            ////////MESSAGES////////
            ////////////////////////

            /*Envois d'un premier message sur le chanel général*/
            //se.Send("Test Data");
            /*Envois d'un deuxième message sur le chanel général*/
            //se.Send("Test Data2");
            /*Envois d'un message sur un chanel particulier*/
            //se.SendOn("Test1", "helloworld");

            ////////////////////////
            //////////EMIT//////////
            ////////////////////////

            /*Envois d'une information à tout utilisateur inscrit sur le serveur*/
            //sc.Emit("Hello Tout le monde");
            /*Envois d'une information à sur le chanel correspondant à chaque utilisateur inscrit sur le serveur*/
            //sc.EmitOn("Test2","Salut tout le monde!");

            Console.ReadKey();

        }
    }
}
