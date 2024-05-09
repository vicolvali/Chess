using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using ChessLogic; // Assuming this namespace includes your game logic classes

namespace ChessLogic
{
    public class ChessServer
    {
        private TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>();
        private GameState gameState; // This should manage your chess game state

        public ChessServer(string ip, int port)
        {
            listener = new TcpListener(IPAddress.Parse(ip), port);
            listener.Start();
            Console.WriteLine("Server started on " + ip + ":" + port);

            // Assuming you have a method in the Board class to initialize the board with pieces
            Board initialBoard = Board.Initial();
            Player startingPlayer = Player.White; // Assuming this sets who starts the game

            gameState = new GameState(startingPlayer, initialBoard); // Correct instantiation of GameState
        }

        public void Run()
        {
            try
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    clients.Add(client);
                    Console.WriteLine("Client connected.");
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            try
            {
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    string input;
                    while ((input = reader.ReadLine()) != null)
                    {
                        if (input.StartsWith("move "))
                        {
                            ProcessMoveCommand(input, writer);
                        }
                        else if (input.StartsWith("chat "))
                        {
                            BroadcastMessage(input);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Client disconnected: {e.Message}");
            }
            finally
            {
                clients.Remove(client);
                client.Close();
            }
        }

        private void ProcessMoveCommand(string command, StreamWriter writer)
        {
            // Implement logic to process moves
            // Example: command = "move e2 e4"
            writer.WriteLine("Move processed: " + command);
            BroadcastMessage("Server: " + command);
        }

        private void BroadcastMessage(string message)
        {
            foreach (var c in clients)
            {
                StreamWriter writer = new StreamWriter(c.GetStream()) { AutoFlush = true };
                writer.WriteLine(message);
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            ChessServer server = new ChessServer("127.0.0.1", 13000); // Set to localhost IP for local testing
            server.Run();
        }
    }
}
