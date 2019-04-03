﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Chat
{
    class Program
    {
        static IPHostEntry host;

        static string myIp;
        static string myPort;
        static Object _lock = new Object();

        static int connectionCount = 0;
        static Dictionary<int, TcpClient> connections = new Dictionary<int, TcpClient>();

        static void Main(string[] args)
        {
            host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress address = Array.Find(host.AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
            myIp = address.ToString();
            myPort = args[0];

            //Console.WriteLine(System.Net.IPAddress.Loopback);
            TcpListener listenSocket = new TcpListener(address, int.Parse(myPort));
            listenSocket.Start();

            new Thread(HandleCommandLine).Start();

            while (true)
            {
                TcpClient newConnection = listenSocket.AcceptTcpClient();
                if (newConnection != null)
                {
                    Console.WriteLine("Incoming Connection from " + newConnection.Client.RemoteEndPoint.ToString());
                    lock (_lock)
                    {

                        connections.Add(++connectionCount, newConnection);
                        KeyValuePair<int, TcpClient> pair = new KeyValuePair<int, TcpClient>(connectionCount, newConnection);
                        new Thread(HandleConnection).Start(pair);
                    }
                }

            }
        }

        static void HandleConnection(object obj)
        {
            KeyValuePair<int, TcpClient> pair = (KeyValuePair<int, TcpClient>)obj;
            TcpClient con = pair.Value;

            string[] ip_port = con.Client.RemoteEndPoint.ToString().Split(':');

            var currentMessage = new List<byte>();

            while (true)
            {
                var readMessage = new byte[100];
                int readMessageSize;

                try
                {
                    readMessageSize = con.GetStream().Read(readMessage, 0, 100);
                }
                catch (Exception)
                {

                    break;
                }

                if (readMessageSize <= 0)
                {
                    Console.WriteLine("Connection with ID: " + pair.Key + " was terminated.");
                    break;
                }

                foreach (var b in readMessage)
                {
                    if (b == 0) break;

                    if (b == 4)
                    {

                        Console.WriteLine("Message received from " + ip_port[0]);
                        Console.WriteLine("Sender's Port: " + ip_port[1]);
                        Console.WriteLine("Message: \"" + new ASCIIEncoding().GetString(currentMessage.ToArray()) + "\"");
                        currentMessage.Clear();
                    }
                    else
                    {
                        currentMessage.Add(b);
                    }
                }
            }

            lock (_lock)
            {
                connections.Remove(pair.Key);
            }
        }

        static void HandleCommandLine()
        {
            while (true)
            {
                string input = Console.ReadLine();
                string[] split = input.Split(' ');
                switch (split[0])
                {
                    case "exit":
                        Environment.Exit(1);
                        break;

                    case "help":
                        Console.WriteLine(
                            "***CHAT HELP***" +
		                    "\nList of Commands supported:" +
                            "\n> help" +
                            "\n> myip" +
                            "\n> myport" +
                            "\n> connect <destination> <port> " +
                            "\n> list" +
                            "\n> terminate <connection id>" +
                            "\n> send <connection id> <message>\n"
                            );
                        break;

                    case "myip":
                        Console.WriteLine("Local IP Address: " + myIp);
                        break;

                    case "myport":
                        Console.WriteLine("Listening on Port: " + myPort);
                        break;

                    case "connect":
                        {
                            if (split.Length == 3)
                            {
                                lock (_lock)
                                {
                                    TcpClient newConnection = new TcpClient(split[1], int.Parse(split[2]));
                                    if (newConnection != null)
                                    {
                                        Console.WriteLine("Connected to " + split[1] + ":" + split[2]);

                                        connections.Add(++connectionCount, newConnection);
                                        KeyValuePair<int, TcpClient> pair = new KeyValuePair<int, TcpClient>(connectionCount, newConnection);
                                        new Thread(HandleConnection).Start(pair);
                                    }
                                }
                            }
                        }
                        break;

                    case "list":
                        Console.WriteLine("ID\tIP Address\t\tPort No.");
                        foreach (KeyValuePair<int, TcpClient> connection in connections)
                        {
                            string[] ip_port = connection.Value.Client.RemoteEndPoint.ToString().Split(':');
                            Console.WriteLine(connection.Key + ":\t" + ip_port[0] + "\t\t" + ip_port[1]);
                        }
                        break;

                    case "terminate":
                        if (split.Length == 2)
                        {
                            lock (_lock)
                            {
                                if (connections.ContainsKey(int.Parse(split[1])))
                                {
                                    Console.WriteLine("Terminating connection ID: " + split[1]);
                                    connections[int.Parse(split[1])].Close();
                                    connections.Remove(int.Parse(split[1]));
                                }
                                else
                                {
                                    Console.WriteLine("Connection with this ID does not exist.");
                                }
                            }
                        }
                        break;

                    case "send":
                        if (split.Length >= 3)
                        {
                            string message = String.Join(" ", split.Skip(2));
                            message += (char)4;
                            byte[] data = new ASCIIEncoding().GetBytes(message);
                            try
                            {
                                connections[int.Parse(split[1])].GetStream().Write(data, 0, data.Length);
                                Console.WriteLine("Message sent to " + split[1]);
                            }
                            catch (Exception e)
                            {

                                Console.WriteLine(e.Message);
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
    