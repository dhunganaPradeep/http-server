using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Logs from your program will appear here!");

        // Initialize and start the TcpListener to listen for incoming connections on port 4221
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();

        while (true)
        {
            // Asynchronously accept an incoming connection
            TcpClient client = await server.AcceptTcpClientAsync();
            // Handle the client connection asynchronously
            _ = HandleClientAsync(client); // Fire and forget to handle multiple connections concurrently
        }
    }

    // Asynchronous method to handle client requests
    static async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            // Get the network stream to read and write data
            using NetworkStream stream = client.GetStream();
            
            // Buffer to store incoming data
            byte[] buffer = new byte[1024];
            // Asynchronously read data from the network stream
            int received = await stream.ReadAsync(buffer, 0, buffer.Length);

            // Convert the bytes received into a string
            string request = Encoding.UTF8.GetString(buffer, 0, received);
            
            // Extract the request line from the request
            string requestLine = request.Split("\r\n")[0];
            // Split the request line to get the HTTP method and URL path
            string[] requestParts = requestLine.Split(' ');
            string method = requestParts[0];
            string urlPath = requestParts[1];

            string httpResponse;

            // Determine the response based on the URL path
            if (urlPath == "/")
            {
                // Root path responds with 200 OK
                httpResponse = "HTTP/1.1 200 OK\r\n\r\n";
            }
            else if (urlPath.StartsWith("/echo/"))
            {
                // Extract the string after "/echo/"
                string echoString = urlPath.Substring(6);

                // Construct the response with Content-Type and Content-Length headers
                httpResponse = "HTTP/1.1 200 OK\r\n" +
                               "Content-Type: text/plain\r\n" +
                               $"Content-Length: {echoString.Length}\r\n" +
                               "\r\n" +
                               echoString;
            }
            else if (urlPath == "/user-agent")
            {
                // Extract the User-Agent header from the request
                string userAgent = string.Empty;
                string[] headers = request.Split("\r\n");

                foreach (var header in headers)
                {
                    if (header.StartsWith("User-Agent:"))
                    {
                        userAgent = header.Substring(12).Trim(); // Extract the value after "User-Agent:"
                        break;
                    }
                }

                // Construct the response with User-Agent value in the body
                httpResponse = "HTTP/1.1 200 OK\r\n" +
                               "Content-Type: text/plain\r\n" +
                               $"Content-Length: {userAgent.Length}\r\n" +
                               "\r\n" +
                               userAgent;
            }
   else if (urlPath.StartsWith("/files/"))
            {
                // Extract the filename from the URL path
                string filename = urlPath.Substring(7);

                // Path to the file (Assuming files are stored in the "wwwroot" directory)
                string filePath = Path.Combine("wwwroot", filename);

                if (File.Exists(filePath))
                {
                    // Read the file contents
                    byte[] fileContents = await File.ReadAllBytesAsync(filePath);

                    // Determine content type based on file extension
                    string contentType = "application/octet-stream"; // Default MIME type
                    string extension = Path.GetExtension(filename).ToLower();
                    if (extension == ".html")
                        contentType = "text/html";
                    else if (extension == ".txt")
                        contentType = "text/plain";
                    else if (extension == ".jpg" || extension == ".jpeg")
                        contentType = "image/jpeg";
                    else if (extension == ".png")
                        contentType = "image/png";

                    // Construct the response headers and body
                    httpResponse = $"HTTP/1.1 200 OK\r\n" +
                                   $"Content-Type: {contentType}\r\n" +
                                   $"Content-Length: {fileContents.Length}\r\n" +
                                   "\r\n";
                    
                    // Send headers
                    byte[] responseHeaders = Encoding.UTF8.GetBytes(httpResponse);
                    await stream.WriteAsync(responseHeaders, 0, responseHeaders.Length);
                    
                    // Send file contents
                    await stream.WriteAsync(fileContents, 0, fileContents.Length);
                }
                else
                {
                    // File not found
                    httpResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(httpResponse);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }
            else
            {
                // Handle unknown paths
                httpResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                byte[] responseBytes = Encoding.UTF8.GetBytes(httpResponse);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur while handling the client
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            // Ensure the client connection is closed after processing
            client.Close();
        }
    }
}
