using System;
using System.Collections.Generic;
using System.Text;
using CsharpSocketClient.models;
using System.Threading;
using System.Threading.Tasks;

namespace CsharpSocketClient.models
{

    /*
     * @{name}      Testeur
     * @{type}      class
     * @{desc}      Représente le routeur socket, il contient les modules "SocketEmitor" & "SocketCaptor"
     */
    public class Testeur
    {

        private SocketRouter _routeur;
        private CancellationTokenSource _action;
        private System.Timers.Timer _interval;

        private List<bool> TestSend = new List<bool>();
        private List<bool> TestEmit = new List<bool>();

        public Testeur(SocketRouter routeur)
        {
            this._routeur = routeur;

            this._testChanels();
            this._testEmit();

        }

        ////////////////////////
        ///METHODES PRIVATE/////
        ////////////////////////

        private void _ping()
        {
            this._routeur.EmitOn("ping", "ping");
        }

        private void _EnvoisAvecInterval(int max = 2 , int i = 0)
        {

            /*            Action<object> setInterval = (object obj) => 
                        {
                            CancellationToken token = (CancellationToken)obj;

                            Action callBack_stop = () =>
                            {
                                Interval.Stop(this._interval);
                            };

                            Action callBack_interval = () =>
                            {
                                this._routeur.Send($"Message {i}");
                                if (i == max) Interval.Stop(this._interval);
                                i++;
                            };

                            this._interval = Interval.Set(callBack_interval, 500);

                            this._action.Cancel();
                            this._action.Dispose();
                        };

                        // création du token pour le test d'envois avec interval
                        this._action = new CancellationTokenSource();

                        // Utilisation du token avec le callbask async liée dans la pile de tâches
                        ThreadPool.QueueUserWorkItem(new WaitCallback(setInterval), this._action.Token);*/

            Action callBack_interval = () =>
            {
                Thread.Sleep(1000);
                this._routeur.Send($"Message {i}");
                if(i == max) Thread.Sleep(0);
                i++;
            };

            this._interval = Interval.Set(callBack_interval, 500);
            Thread.Sleep(100);
            Interval.Stop(this._interval);
        }

        private void _testChanels()
        {

            //Console.Write("Test send / sendOn Chanels : ");

            this._routeur.On("callback_Test1", (dynamic cli, Message arg) =>
            {
                return cli.Reply("Reply callback_Test1");
            });

            this._routeur.On("callback_Test2", (dynamic cli, Message arg) =>
            {
                return cli.Reply("Reply callback_Test2");
            });

            this._routeur.On("callback_Test3", (dynamic cli, Message arg) =>
            {
                return cli.Reply("Reply callback_Test3");
            });

            this._routeur.On("callback_Test4", (dynamic cli, Message arg) =>
            {
                return cli.Reply("Reply callback_Test4");
            });

            this._routeur.Send("RouteurTest send Message").OnReply((dynamic cli , Message arg) => {
                Console.WriteLine("BLOUBLOU");
            });

            this._routeur.SendOn("callback_Test1", "callback1").OnReply((dynamic cli, Message arg) => {
                this.TestSend.Add(true);
            });
            this._routeur.SendOn("callback_Test2", "callback2").OnReply((dynamic cli, Message arg) => {
                this.TestSend.Add(true);
            });
            this._routeur.SendOn("callback_Test3", "callback4").OnReply((dynamic cli, Message arg) => {
                this.TestSend.Add(true);
            });
            this._routeur.SendOn("callback_Test4", "callback4").OnReply((dynamic cli, Message arg) => {
                this.TestSend.Add(true);
            });

            this._routeur.DeleteOn("callback_Test1");
            this._routeur.DeleteOn("callback_Test2");
            this._routeur.DeleteOn("callback_Test3");
            this._routeur.DeleteOn("callback_Test4");

/*            if (this.TestSend.Contains(false))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR !");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("OK !");
            }
            Console.WriteLine();
            Console.ResetColor();*/

            this.TestSend = new List<bool>();

        }

        private void _testEmit()
        {

            this._routeur.On("callback_Emit_Test1", (dynamic cli, Message arg) =>
            {
                return cli.Reply("Reply callback_Emit_Test1");
            });

            this._routeur.DeleteOn("callback_Emit_Test1");

/*            if (this.TestEmit.Contains(false))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR !");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("OK !");
            }
            Console.WriteLine();
            Console.ResetColor();*/

            this.TestEmit = new List<bool>();
        }

        ////////////////////////
        /////CLASS PRIVATE//////
        ////////////////////////

        // Création d'un interval , utiliser que pour les test , cause des soucis de mémoires
        private class Interval
        {
            public static System.Timers.Timer Set(System.Action action, int interval)
            {
                var timer = new System.Timers.Timer(interval);
                timer.Elapsed += (s, e) => {
                    timer.Enabled = false;
                    action();
                    timer.Enabled = true;
                };
                timer.Enabled = true;
                return timer;
            }

            public static void Stop(System.Timers.Timer timer)
            {
                timer.Stop();
                timer.Dispose();
            }
        }

    }
}
