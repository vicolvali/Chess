using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using ChessLogic;
using ChessServer;
using ChessUI;// Assuming ChessLogic is correctly referenced

namespace ChessServer
{
    public class ChessServerApp
    {
        private TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>();
        private List<GameSession> sessions = new List<GameSession>();

        public ChessServerApp(string ip, int port)
        {
            listener = new TcpListener(IPAddress.Parse(ip), port);
            listener.Start();
            Console.WriteLine($"Server started on {ip}:{port}");
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
                    if (clients.Count % 2 == 0)
                    {
                        StartGameSession(clients[clients.Count - 2], clients[clients.Count - 1]);
                    }
                }
            }
            finally
            {
                foreach (var client in clients)
                {
                    client.Close();
                }
                listener.Stop();
            }
        }

        private void StartGameSession(TcpClient player1, TcpClient player2)
        {
            var session = new GameSession(player1, player2);
            sessions.Add(session);
            Console.WriteLine("New game session started.");
            //session.StartGame();

            // Start handling clients in parallel threads
            Thread player1Thread = new Thread(() => HandleClient(player1, session));
            Thread player2Thread = new Thread(() => HandleClient(player2, session));
            player1Thread.Start();
            player2Thread.Start();

            if (session.IsReadyToStart())
            {
                session.StartGame();
                BroadcastMessage(session, "Both players are connected. Game starts now.");
            }
        }


        private void HandleClient(TcpClient client, GameSession session)
        {
            // Declare playerName outside the try block so it's accessible throughout the method
            string playerName = null;

            var stream = client.GetStream();
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream) { AutoFlush = true };

            try
            {
                writer.WriteLine("Welcome to the Chess Server! Please enter your name.");
                playerName = reader.ReadLine();
                session.SetPlayerName(client, playerName);
                Console.WriteLine($"{playerName} has joined the game session.");
                writer.WriteLine("Waiting for another player to start the game...");

                if (session.IsReadyToStart())
                {
                    BroadcastMessage(session, "Both players are connected. Game starts now.");
                    session.StartGame();
                }

                string input;
                while ((input = reader.ReadLine()) != null)
                {
                    if (input.StartsWith("move"))
                    {
                        var move = session.ParseMove(input);
                        if (session.MakeMove(move, client))
                        {
                            BroadcastMessage(session, $"{playerName} made a move: {input}");
                        }
                        else
                        {
                            writer.WriteLine("Invalid move");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error handling client: {e.Message}");
            }
            finally
            {
                session.RemovePlayer(client);
                if (playerName != null)  // Check if playerName was set
                {
                    Console.WriteLine($"{playerName} disconnected.");
                }
                client.Close();
            }
        }


        private void BroadcastMessage(GameSession session, string message)
        {
            foreach (var player in session.Players)
            {
                try
                {
                    var writer = new StreamWriter(player.GetStream()) { AutoFlush = true };
                    writer.WriteLine(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to send message to player: {e.Message}");
                }
            }
        }
    }

    // Assume these classes are part of the ChessLogic namespace
    public class GameState
    {
        public Board CurrentBoard { get; set; }

        public GameState(Player startingPlayer, Board initialBoard)
        {
            CurrentBoard = initialBoard;
        }
    }

    public class GameSession
    {
        public TcpClient Player1 { get; private set; }
        public TcpClient Player2 { get; private set; }
        private Dictionary<TcpClient, string> playerNames = new Dictionary<TcpClient, string>();
        public GameState GameState;

        public IEnumerable<TcpClient> Players
        {
            get
            {
                yield return Player1;
                yield return Player2;
            }
        }

        public GameSession(TcpClient player1, TcpClient player2)
        {
            Player1 = player1;
            Player2 = player2;
            GameState = new GameState(Player.White, Board.Initial());
        }

        public void SetPlayerName(TcpClient client, string name)
        {
            playerNames[client] = name;
        }

        public bool IsReadyToStart()
        {
            return playerNames.Count == 2;
        }

        public void StartGame()
        {
            // Initialize or reset the game state here
            Console.WriteLine("Game started between " + playerNames[Player1] + " and " + playerNames[Player2]);
        }

        public bool MakeMove(Move move, TcpClient client)
        {
            if (move.IsLegal(GameState.CurrentBoard))
            {
                move.Execute(GameState.CurrentBoard);
                return true;
            }
            return false;
        }

        public Move ParseMove(string moveCommand)
        {
            // Implement move parsing based on your game's rules and command syntax
            return null; // Placeholder
        }

        public void RemovePlayer(TcpClient client)
        {
            playerNames.Remove(client);
        }
    }
}
