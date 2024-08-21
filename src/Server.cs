using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started on port 4221");

        while (true)
        {
            Socket clientSocket = server.AcceptSocket();
            Task.Run(() => HandleClient(clientSocket)); // Handle each client in a new task
        }
    }

    static void HandleClient(Socket socket)
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
                // Extract the filename from the URL
                string fileName = urlPath.Substring(7);

                // Define the directory where files are stored
                string fileDirectory = "files";  // Assuming files are stored in a folder named "files"

                // Get the full file path
                string filePath = Path.Combine(fileDirectory, fileName);

                if (File.Exists(filePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string fileContent = Encoding.UTF8.GetString(fileBytes);

                    // Construct the response headers and body
                    httpResponse = "HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: text/plain\r\n" +  // Set appropriate content type based on file
                                   $"Content-Length: {fileBytes.Length}\r\n" +
                                   "\r\n" +
                                   fileContent;
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
        catch (Exception ex)
        {
            Console.WriteLine("Error handling client: " + ex.Message);
        }
        finally
        {
            socket.Close();
        }
    }
}
