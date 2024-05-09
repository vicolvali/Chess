// Program.cs
// Program.cs
using System;
using ChessNetworking;  // This imports the namespace

namespace ChessClient
{
    class Program
    {
        public static void Main(string[] args)
        {
            // Here you should use the full name if there's a naming conflict
            ChessNetworking.ChessClient client = new ChessNetworking.ChessClient("127.0.0.1", 13000);
            Console.ReadLine(); // Keep the client application running
        }
    }
}

