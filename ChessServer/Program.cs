// See https://aka.ms/new-console-template for more information

using ChessServer;
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting server...");
        ChessServerApp server = new ChessServerApp("127.0.0.1", 13000); // Use the new class name
        server.Run();
    }
}


