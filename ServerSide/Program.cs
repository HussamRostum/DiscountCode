using MySql.Data.MySqlClient;  //namespace for working with the MYSQL db (Connecting, executing Commands and managing transactions).
using System;  // namespace for core.NET library (input/output operations,data types and exeption handiling).
using System.Collections.Generic; // used to manage collection of data.
using System.Linq; // namespace provide language integrated query (enablin queries on collections and arrays using query syntax)
using System.Net; // namespace includes classes for working with network operations, such as retrieving IP addresses and working with internet protocols.
using System.Net.Sockets; // namespace provides classes for working with low-level network communication, including TCP and UDP protocols.(TcpListener and TcpClient)
using System.Text;//namespace includes classes for handling text encoding and manipulation, such as Encoding, which converts text to and from byte arrays.
using System.Threading.Tasks; // This namespace provides support for asynchronous programming and parallel tasks.(task class:  used for running asynchronous operations without blocking the main thread. )

// this is the server namespace which will manage the connect to the database and check it and generate the discount codes and send and recieve the queries from to the data base and connect with the clients.
namespace Server
{
    class Program
    {
        //Establish a connection to the MySQL database
        private static string connectionString = "Server=localhost;Database=DiscountCodesDB;User ID=root;Password=;Pooling=true;SslMode=none;";

        static void Main(string[] args)
        {
            while (true)
            {
                if (InitializeDatabaseAndGenerateCodes())
                {
                    break; // Proceed if the database is properly initialized
                }
                else
                {
                    Console.WriteLine("Retrying database initialization...");
                }
            }
            //Sets up a TCP server that listens on port 8080, making it ready to handle client requests.
            var server = new TcpListener(IPAddress.Any, 8080);
            server.Start();
            Console.WriteLine("Server started on port 8080...");
            Console.WriteLine("Pleas Conect to the Client Side to test the connection and use the discounts");
            while (true)
            {
                //process multiple client requests simultaneously, enabling efficient handling of concurrent connections.
                var client = server.AcceptTcpClient();
                Task.Run(() => HandleClient(client));
            }
        }

        private static bool InitializeDatabaseAndGenerateCodes()
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if the table exists
                    var command = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'DiscountCodesDB' AND table_name = 'DiscountCodes';",
                        connection);
                    var tableExists = Convert.ToInt32(command.ExecuteScalar()) > 0;

                    if (!tableExists)
                    {
                        Console.WriteLine("Table 'DiscountCodes' does not exist. Creating table...");
                        var createTableCommand = new MySqlCommand(
                            @"CREATE TABLE DiscountCodes (
                                Id INT AUTO_INCREMENT PRIMARY KEY,
                                Code VARCHAR(8) UNIQUE NOT NULL,
                                Used BOOLEAN NOT NULL DEFAULT FALSE
                              );",
                            connection);
                        createTableCommand.ExecuteNonQuery();
                        Console.WriteLine("Table 'DiscountCodes' created successfully.");
                    }

                    // Check if the table is empty
                    command = new MySqlCommand("SELECT COUNT(*) FROM DiscountCodes;", connection);
                    var rowCount = Convert.ToInt32(command.ExecuteScalar());

                    if (rowCount == 0)
                    {
                        Console.WriteLine("Table of Codes is empty. Generating discount codes......");
                        GenerateDiscountCodes(1000, 8); // Generate 1000 codes with 8 characters
                        Console.WriteLine("Discount codes generated and saved to the database.");
                    }

                    connection.Close();
                }

                return true; // Database successfully initialized
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization failed: {ex.Message}");
                return false; // Retry initialization
            }
        }

        //Represents the connected client. This is used to read and write data over the network.
        private static void HandleClient(TcpClient client)
        {
            using (var networkStream = client.GetStream())
            {
                // Read Data from the Client:
                var buffer = new byte[client.ReceiveBufferSize];
                var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                var message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (message.StartsWith("GENERATE"))
                {
                    var parts = message.Split('|');
                    var count = ushort.Parse(parts[1]);
                    var length = byte.Parse(parts[2]);
                    var response = GenerateDiscountCodes(count, length);

                    //Response Message Contains "SUCCESS" if the discount codes were generated successfully; otherwise, "FAILURE".
                    var responseMessage = response ? "SUCCESS" : "FAILURE";
                    var responseBytes = Encoding.ASCII.GetBytes(responseMessage);
                    networkStream.Write(responseBytes, 0, responseBytes.Length);
                }
                else if (message.StartsWith("USE"))
                {
                    var parts = message.Split('|');
                    var code = parts[1];
                    var response = UseDiscountCode(code);

                    var responseMessage = response ? "SUCCESS" : "FAILURE";
                    var responseBytes = Encoding.ASCII.GetBytes(responseMessage);
                    networkStream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
        }

        //The code to  generate the discountcods with specific requirements.
        private static bool GenerateDiscountCodes(ushort count, byte length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var codes = new HashSet<string>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                while (codes.Count < count)
                {
                    var code = new string(Enumerable.Repeat(chars, length)
                        .Select(s => s[random.Next(s.Length)]).ToArray());

                    if (codes.Add(code))
                    {
                        var command = new MySqlCommand("INSERT INTO DiscountCodes (Code) VALUES (@Code)", connection);
                        command.Parameters.AddWithValue("@Code", code);

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (MySqlException)
                        {
                            // Handle duplicate code error
                        }
                    }
                }
                connection.Close();
            }

            return true;
        }

        //use the code for checking and testing 
        private static bool UseDiscountCode(string code)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var command = new MySqlCommand("UPDATE DiscountCodes SET Used = TRUE WHERE Code = @Code AND Used = FALSE", connection);
                command.Parameters.AddWithValue("@Code", code);
                var rowsAffected = command.ExecuteNonQuery();

                connection.Close();
                return rowsAffected > 0;
            }
        }
    }
}
