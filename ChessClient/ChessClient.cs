using ChessLogic;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace ChessNetworking
{
    public class ChessClient
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private AutoResetEvent inputNeeded = new AutoResetEvent(false);
        private bool isConnected = false;

        

        public event EventHandler<GameStateEventArgs> GameStarted;

        public ChessClient(string serverIp, int serverPort)
        {
           
            try
            {
                client = new TcpClient(serverIp, serverPort);
                Console.WriteLine($"Connected to the chess server at {serverIp}.");
                isConnected = true;
                var stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                Task.Run(() => ListenToServerMessagesAsync());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to connect to server: {e.Message}");
                isConnected = false;
            }
        }


        private async Task ListenToServerMessagesAsync()
        {
            try
            {
                while (isConnected && client.Connected)
                {
                    var message = await reader.ReadLineAsync();
                    if (message != null)
                    {
                        Console.WriteLine($"Received from server: {message}");
                        if (message.Contains("Please enter your name"))
                        {
                            Console.WriteLine("Please enter your name:");
                            inputNeeded.Set();
                            HandleUserInput();
                        }
                        else if (message.Contains("Game is starting now."))
                        {
                            Console.WriteLine("Triggering game start on client.");
                            StartGameClientSide();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Disconnected from server: {e.Message}");
            }
        }



        private void StartGameClientSide()
        {
            Console.WriteLine("The game has started. You can make your move.");
            GameState newGameState = new GameState(Player.White, Board.Initial());
            OnGameStarted(newGameState);
        }

        protected virtual void OnGameStarted(GameState gameState)
        {
            GameStarted?.Invoke(this, new GameStateEventArgs { GameState = gameState });
        }

        private void HandleUserInput()
        {
            inputNeeded.WaitOne();
            string input = Console.ReadLine();
            Send(input);
        }

        public void Send(string message)
        {
            if (writer != null)
            {
                try
                {
                    writer.WriteLine(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error sending message: {e.Message}");
                }
            }
        }
    }

    public class GameStateEventArgs : EventArgs
    {
        public GameState GameState { get; set; }
    }
}
