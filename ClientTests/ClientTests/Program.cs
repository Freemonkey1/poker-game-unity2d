﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClientTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //SZYBKI PORADNIK
            /*
             * 
             * 
             * juz nie
             * 
             * 
             * 
             * 
             * 
             */




            TcpClient server = new TcpClient();
            server.Connect("127.0.0.1", 6937);
            NetworkStream ns = server.GetStream();





            Console.WriteLine("Login: ");
            string username = Console.ReadLine();
            Console.WriteLine("Password: ");
            string password = Console.ReadLine();

            byte[] message = System.Text.Encoding.ASCII.GetBytes(username + ' ' + password);
            ns.Write(message, 0, message.Length);


            byte[] myReadBuffer = new byte[1024];
            int numberOfBytesRead = 0;
            StringBuilder myCompleteMessage = new StringBuilder();
            numberOfBytesRead = ns.Read(myReadBuffer, 0, myReadBuffer.Length);
            myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
            string[] request = myCompleteMessage.ToString().Split(new char[] { ' ' });
            string token = request[0];
            ns.Flush();
            bool error = false;

                    
            if (token == "##&&@@0000")
            {
                error = true;
                Console.WriteLine("Server Error");
            }        
            else if (token == "##&&@@0001")
            {
                error = true;
                Console.WriteLine("bad login");
            }
            else if (token == "##&&@@0002")
            {
                error = true;
                Console.WriteLine("bad password");
            }        
            else if (token == "##&&@@0003")
            {
                error = true;
                Console.WriteLine("already logged");
            }          
            Console.ReadKey();   
            if(!error)
            {
                TcpClient serverGame = new TcpClient();
                serverGame.Connect("127.0.0.1", 6938);
                string login = request[3];
                string tokens = request[2];

                ConsoleKeyInfo cki;
                Console.CursorVisible = false;
                var sb = new StringBuilder();
                var emptySpace = new StringBuilder();
                emptySpace.Append(' ', 10);
                bool running = true;

                Console.Clear();
                int nr = int.Parse(tokens);
                while (running)
                {
                    Console.SetCursorPosition(0, 0);
                    sb.Clear();

                    byte[] tosendtables = System.Text.Encoding.ASCII.GetBytes(token + ' ' + "2");
                    ns.Write(tosendtables, 0, tosendtables.Length);
                    Thread.Sleep(1000);
                    if (ns.DataAvailable)
                    {
                        byte[] readBuf = new byte[4096];
                        StringBuilder menuRequestStr = new StringBuilder();
                        int nrbyt = ns.Read(readBuf, 0, readBuf.Length);
                        menuRequestStr.AppendFormat("{0}", Encoding.ASCII.GetString(readBuf, 0, nrbyt));
                        string[] tables = menuRequestStr.ToString().Split(new string(":T:")); //na poczatku tez dzieli i wykrywa 1 pusty string 
                        //dlatego tutaj i=1
                        for (int i = 1; i < tables.Length; i++)
                        {
                            string[] mess = tables[i].Split(' ');
                            sb.AppendLine("---Table---");
                            sb.AppendLine("Name       : " + mess[0]);
                            sb.AppendLine("Owner      : " + mess[1]);
                            sb.AppendLine("Human count: " + mess[2]);
                            sb.AppendLine("Bot count  : " + mess[3]);
                            sb.AppendLine("min XP     : " + mess[4]);
                            sb.AppendLine("min tokens : " + mess[5]);
                        }
                    }


                    Console.WriteLine(sb);
                    if (Console.KeyAvailable)
                    {
                        cki = Console.ReadKey();
                        if (cki.Key == ConsoleKey.Escape)
                        {
                            running = false;
                        }
                        if (cki.Key == ConsoleKey.A)
                        {
                            byte[] tosend = System.Text.Encoding.ASCII.GetBytes(token + ' ' + "0" + ' ' + login + nr.ToString() + ' ' + "1" + ' ' + "3" + ' ' + "0" + ' ' + "16" + ' ');
                            ns.Write(tosend, 0, tosend.Length);
                            nr++;

                        }
                        if (cki.Key == ConsoleKey.J)
                        {
                            Console.WriteLine("Enter table name");
                            string tableName = Console.ReadLine();
                            byte[] tosend = System.Text.Encoding.ASCII.GetBytes(token + ' ' + "1" + ' ' + tableName + ' ');
                            ns.Write(tosend, 0, tosend.Length);
                            nr++;

                        }
                        if (cki.Key == ConsoleKey.C)
                        {
                            byte[] tosend = System.Text.Encoding.ASCII.GetBytes(token + ' ' + "7" + ' ' + "1" + ' ' + "3" + ' ' + "20" + ' ' + "55" + ' ');
                            ns.Write(tosend, 0, tosend.Length);
                            nr++;

                        }
                    }
                    Console.WriteLine(emptySpace);
                }
                byte[] tose = System.Text.Encoding.ASCII.GetBytes(token + ' ' + "4");
                ns.Write(tose, 0, tose.Length);
                Thread.Sleep(1000);
                ns.Flush();
                serverGame.Close();
            }
            
        }
    }
}
