using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        // Parse the --directory flag
        string directory = args.Length > 1 && args[0] == "--directory" ? args[1] : "/tmp";

        // Start the server
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();

        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            // Accept a new client connection
            Socket socket = server.AcceptSocket();

            // Handle the client connection in a new thread
            Thread clientThread = new Thread(() => HandleClient(socket, directory));
            clientThread.Start();
        }

        // server.Stop(); // Server will keep running, no need to stop it here
    }

    static void HandleClient(Socket socket, string directory)
    {
        try
        {
            // Buffer to store the incoming request
            byte[] buffer = new byte[4096]; // Increased buffer size for larger requests
            int received = socket.Receive(buffer);

            // Convert the bytes received into a string
            string request = Encoding.UTF8.GetString(buffer, 0, received);

            // Extract the request line (first line of the request)
            string requestLine = request.Split("\r\n")[0];

            // Extract the URL path from the request line
            string[] requestParts = requestLine.Split(' ');
            string method = requestParts[0];
            string urlPath = requestParts[1];

            // Determine the response based on the URL path
            string httpResponse;

            if (method == "GET" && urlPath.StartsWith("/files/"))
            {
                // Handle the GET request
                string filename = urlPath.Substring(7);
                string filePath = Path.Combine(directory, filename);

                if (File.Exists(filePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    httpResponse = "HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: application/octet-stream\r\n" +
                                   $"Content-Length: {fileBytes.Length}\r\n" +
                                   "\r\n";
                    socket.Send(Encoding.UTF8.GetBytes(httpResponse));
                    socket.Send(fileBytes);  // Send file content separately
                    return;
                }
                else
                {
                    httpResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                }
            }
            else if (method == "POST" && urlPath.StartsWith("/files/"))
            {
                // Handle the POST request
                string filename = urlPath.Substring(7);
                string filePath = Path.Combine(directory, filename);

                // Extract the Content-Length header to determine the size of the body
                string[] headers = request.Split("\r\n");
                int contentLength = 0;
                foreach (var header in headers)
                {
                    if (header.StartsWith("Content-Length:"))
                    {
                        contentLength = int.Parse(header.Substring("Content-Length:".Length).Trim());
                        break;
                    }
                }

                // Extract the body from the request (the body starts after two consecutive \r\n)
                int headerEndIndex = request.IndexOf("\r\n\r\n");
                if (headerEndIndex != -1)
                {
                    string requestBody = request.Substring(headerEndIndex + 4, contentLength);

                    // Write the request body to the specified file
                    File.WriteAllText(filePath, requestBody);

                    // Respond with 201 Created
                    httpResponse = "HTTP/1.1 201 Created\r\n\r\n";
                }
                else
                {
                    // Malformed request
                    httpResponse = "HTTP/1.1 400 Bad Request\r\n\r\n";
                }
            }
            else
            {
                // Handle other requests or 404 for unsupported methods
                httpResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
            }

            // Send the response
            socket.Send(Encoding.UTF8.GetBytes(httpResponse));
        }
        finally
        {
            // Close the socket connection
            socket.Close();
        }
    }
}
