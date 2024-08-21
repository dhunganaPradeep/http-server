using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static string fileDirectory;

    static void Main(string[] args)
    {
        // Check if the directory argument is provided
        if (args.Length < 2 || args[0] != "--directory")
        {
            Console.WriteLine("Usage: ./your_server --directory <directory_path>");
            return;
        }

        // Set the file directory from the command-line argument
        fileDirectory = args[1];

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

            if (urlPath.StartsWith("/files/"))
            {
                // Extract the filename from the URL
                string fileName = urlPath.Substring(7);

                // Get the full file path
                string filePath = Path.Combine(fileDirectory, fileName);

                if (File.Exists(filePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);

                    // Construct the response headers and body
                    httpResponse = "HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: text/plain\r\n" +  // Set appropriate content type based on file
                                   $"Content-Length: {fileBytes.Length}\r\n" +
                                   "\r\n";

                    socket.Send(Encoding.UTF8.GetBytes(httpResponse));
                    socket.Send(fileBytes); // Send file content separately
                }
                else
                {
                    httpResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                    socket.Send(Encoding.UTF8.GetBytes(httpResponse));
                }
            }
            else
            {
                httpResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                socket.Send(Encoding.UTF8.GetBytes(httpResponse));
            }
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
