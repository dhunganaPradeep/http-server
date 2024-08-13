using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

//  Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

var socket = server.AcceptSocket();

// Construct the HTTP response
string httpResponse = "HTTP/1.1 200 OK\r\n" +
                      "Content-Length: 0\r\n" +
                      "Connection: close\r\n" +
                      "\r\n";
socket.Send(Encoding.UTF8.GetBytes(httpResponse));
socket.Close();
server.Stop();
