using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
    }

    static void HandleClient(Socket socket, string directory)
    {
        try
        {
            // Buffer to store the incoming request
            byte[] buffer = new byte[1024];
            int received = socket.Receive(buffer);

            // Convert the bytes received into a string
            string request = Encoding.UTF8.GetString(buffer, 0, received);

            // Extract the request line (first line of the request)
            string requestLine = request.Split("\r\n")[0];

            // Extract the URL path from the request line
            string[] requestParts = requestLine.Split(' ');
            string method = requestParts[0];
            string urlPath = requestParts[1];

            // Extract headers
            string[] headers = request.Split("\r\n");
            string acceptEncoding = null;
            string userAgent = null;
            int contentLength = 0;
            bool isContentLengthFound = false;

            foreach (var header in headers)
            {
                if (header.StartsWith("Accept-Encoding:"))
                {
                    acceptEncoding = header.Substring(17).Trim();  // Extract Accept-Encoding value
                }
                else if (header.StartsWith("User-Agent:"))
                {
                    userAgent = header.Substring(12).Trim();  // Extract User-Agent value
                }
                else if (header.StartsWith("Content-Length:"))
                {
                    contentLength = int.Parse(header.Split(":")[1].Trim());
                    isContentLengthFound = true;
                }
            }

            string httpResponse;

            if (method == "POST" && urlPath.StartsWith("/files/"))
            {
                // Handle file creation
                string filename = urlPath.Substring(7);
                string filePath = Path.Combine(directory, filename);

                if (!isContentLengthFound || contentLength == 0)
                {
                    // Invalid request, respond with 400 Bad Request
                    httpResponse = "HTTP/1.1 400 Bad Request\r\n\r\n";
                    socket.Send(Encoding.UTF8.GetBytes(httpResponse));
                    return;
                }

                // Extract the body (the part after the headers)
                string requestBody = request.Substring(request.IndexOf("\r\n\r\n") + 4, contentLength);

                // Create the file and write the contents to it
                File.WriteAllText(filePath, requestBody);

                // Respond with 201 Created
                httpResponse = "HTTP/1.1 201 Created\r\n\r\n";
                socket.Send(Encoding.UTF8.GetBytes(httpResponse));
            }
            else if (urlPath == "/")
            {
                httpResponse = "HTTP/1.1 200 OK\r\n\r\n";
            }
            else if (urlPath.StartsWith("/echo/"))
            {
                // Extract the string after "/echo/"
                string echoString = urlPath.Substring(6);

                // Determine if the response should be gzip compressed
                bool shouldGzip = !string.IsNullOrEmpty(acceptEncoding) && acceptEncoding.Split(',').Select(e => e.Trim()).Contains("gzip");
                
                byte[] responseBody = Encoding.UTF8.GetBytes(echoString);
                byte[] compressedBody = responseBody;

                // Compress the body if gzip is acceptable
                if (shouldGzip)
                {
                    using (var output = new MemoryStream())
                    using (var gzip = new GZipStream(output, CompressionMode.Compress))
                    {
                        gzip.Write(responseBody, 0, responseBody.Length);
                        gzip.Close();
                        compressedBody = output.ToArray();
                    }
                }

                // Construct the response headers and body
                httpResponse = "HTTP/1.1 200 OK\r\n" +
                               "Content-Type: text/plain\r\n" +
                               $"Content-Length: {compressedBody.Length}\r\n";

                if (shouldGzip)
                {
                    httpResponse += "Content-Encoding: gzip\r\n";
                }

                httpResponse += "\r\n";

                // Send the response
                socket.Send(Encoding.UTF8.GetBytes(httpResponse));
                socket.Send(compressedBody);  // Send compressed body if applicable
            }
            else if (urlPath == "/user-agent")
            {
                // Construct the response headers and body
                httpResponse = "HTTP/1.1 200 OK\r\n" +
                               "Content-Type: text/plain\r\n" +
                               $"Content-Length: {userAgent.Length}\r\n" +
                               "\r\n" +
                               userAgent;
                socket.Send(Encoding.UTF8.GetBytes(httpResponse));
            }
            else if (urlPath.StartsWith("/files/"))
            {
                // Handle file reading
                string filename = urlPath.Substring(7);
                string filePath = Path.Combine(directory, filename);

                if (File.Exists(filePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);

                    // Determine if the response should be gzip compressed
                    bool shouldGzip = !string.IsNullOrEmpty(acceptEncoding) && acceptEncoding.Split(',').Select(e => e.Trim()).Contains("gzip");
                    byte[] compressedFileBytes = fileBytes;

                    // Compress the file content if gzip is acceptable
                    if (shouldGzip)
                    {
                        using (var output = new MemoryStream())
                        using (var gzip = new GZipStream(output, CompressionMode.Compress))
                        {
                            gzip.Write(fileBytes, 0, fileBytes.Length);
                            gzip.Close();
                            compressedFileBytes = output.ToArray();
                        }
                    }

                    // Construct the response headers and body
                    httpResponse = "HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: application/octet-stream\r\n" +
                                   $"Content-Length: {compressedFileBytes.Length}\r\n";

                    if (shouldGzip)
                    {
                        httpResponse += "Content-Encoding: gzip\r\n";
                    }

                    httpResponse += "\r\n";

                    // Send the response
                    socket.Send(Encoding.UTF8.GetBytes(httpResponse));
                    socket.Send(compressedFileBytes);  // Send compressed file content if applicable
                    return;
                }
                else
                {
                    httpResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                }
            }
            else
            {
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
