using System;
using System.Net.Sockets;
using System.Text;

namespace UseDiscountCodeClient
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                using (var client = new TcpClient("127.0.0.1", 8080))
                {
                    Console.WriteLine("Client connected to server Successfully.");

                    var networkStream = client.GetStream();

                    // Prompt the user to enter a discount code
                    Console.Write("Input your discount code (or type 'exit' to quit): ");
                    var discountCode = Console.ReadLine();

                    // Exit loop if user types 'exit'
                    if (discountCode.ToLower() == "exit")
                    {
                        Console.WriteLine("Exiting the client. Goodbye!");
                        break;
                    }

                    var message = $"USE|{discountCode}";
                    var bytes = Encoding.ASCII.GetBytes(message);

                    // Send the discount code to the server
                    networkStream.Write(bytes, 0, bytes.Length);

                    // Read the response from the server
                    var buffer = new byte[client.ReceiveBufferSize];
                    var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Display the server response
                    Console.WriteLine($"Server response: {response}");

                    // Inform the user if the discount code was successfully used
                    if (response == "SUCCESS")
                    {
                        Console.WriteLine("The Discount Code is Correct and you get 100% discount.");
                    }
                    else if (response == "FAILURE")
                    {
                        Console.WriteLine("The discount code is either invalid or already used.");
                    }
                    else
                    {
                        Console.WriteLine("An unexpected error occurred.");
                    }

                    Console.WriteLine(); // Add a blank line for better readability
                }
            }
        }
    }
}
