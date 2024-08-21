using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

// Parse the --directory flag
string directory = args.Length > 1 && args[0] == "--directory" ? args[1] : "/tmp";

// Start the server
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    using Socket socket = server.AcceptSocket();
    
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

    // Determine the response based on the URL path
    string httpResponse;

    if (urlPath == "/")
    {
        httpResponse = "HTTP/1.1 200 OK\r\n\r\n";
    }
    else if (urlPath.StartsWith("/echo/"))
    {
        // Extract the string after "/echo/"
        string echoString = urlPath.Substring(6);

        // Construct the response headers and body
        httpResponse = "HTTP/1.1 200 OK\r\n" +
                       "Content-Type: text/plain\r\n" +
                       $"Content-Length: {echoString.Length}\r\n" +
                       "\r\n" +
                       echoString;
    }
    else if (urlPath == "/user-agent")
    {
        // Extract the User-Agent header
        string userAgent = string.Empty;
        string[] headers = request.Split("\r\n");

        foreach (var header in headers)
        {
            if (header.StartsWith("User-Agent:"))
            {
                userAgent = header.Substring(12).Trim();  // Extract User-Agent value
                break;
            }
        }

        // Construct the response headers and body
        httpResponse = "HTTP/1.1 200 OK\r\n" +
                       "Content-Type: text/plain\r\n" +
                       $"Content-Length: {userAgent.Length}\r\n" +
                       "\r\n" +
                       userAgent;
    }
    else if (urlPath.StartsWith("/files/"))
    {
        // Extract the filename after "/files/"
        string filename = urlPath.Substring(7);
        string filePath = Path.Combine(directory, filename);

        if (File.Exists(filePath))
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            httpResponse = "HTTP/1.1 200 OK\r\n" +
                           "Content-Type: application/octet-stream\r\n" +
                           $"Content-Length: {fileBytes.Length}\r\n" +
                           "\r\n" +
                           Encoding.UTF8.GetString(fileBytes);
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

server.Stop();
