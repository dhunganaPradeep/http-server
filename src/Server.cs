using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

//  Uncomment this block to pass the first stage
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
    else
    {
        httpResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
    }

    // Send the response
    socket.Send(Encoding.UTF8.GetBytes(httpResponse));
}

server.Stop();
