# HTTP Server

This project is a simple HTTP server implemented in C with the help of the platform CodeCrafters. It handles basic HTTP GET and POST requests and can serve files from a specified directory. It includes support for compression using gzip and handles `Accept-Encoding` and `Content-Encoding` headers. The server listens on port 4221 by default.

## Features

* Serve static files from a specified directory.
* Handle GET and POST requests.
* Echo responses for specific endpoints.
* Return User-Agent details.
* Allow file uploads via POST requests.
* Allow file uploads via POST requests.
* Support gzip compression based on `Accept-Encoding` header.

## Setup and Usage

### Compilation

Once you have cloned the repo and are in the project folder, you can compile the server with the following commands.

```bash
cd src
csc Server.cs
```

### Running the server

Run the compiled server with an optional --directory argument to specify the root directory for serving files:

```bash
./Server --directory /path/to/directory
```

If no directory is specified, the server will use the current directory.

## Testing the Server

You can test the server using `curl`. Below are some examples:

1. Simple GET Request

```bash
curl -v http://localhost:4221/
```

2. Echo Request

```bash
curl -v http://localhost:4221/echo/hello
```

3. User-Agent Request
```bash
curl -v http://localhost:4221/user-agent
```

4. File Upload
```bash
curl -v -X POST http://localhost:4221/files/upload.txt -d 'Hello World'
```

5. File Download
```bash
curl -v http://localhost:4221/files/upload.txt
```

5. Request with Gzip Compression
```bash
curl -v -H "Accept-Encoding: gzip" http://localhost:4221/echo/abc | hexdump -C
```

## How it Works

### `Main()`

The `Main` function sets up the server with the following steps:

1. **Parse Command-Line Arguments**
    - Checks if the `--directory` flag is provided and sets the directory to serve files from. Defaults to `/tmp` if not provided.

2. **Start the Server**
    - Creates a `TcpListener` to listen on port 4221.
    - Begins listening for incoming connections.

3. **Accept Connections**
    - Enters an infinite loop to accept client connections.
    - For each connection, spawns a new thread to handle the client.

### `HandleClient()`

The `HandleClient` function processes the client's request and sends an appropriate response:

1. **Read the Request**
    - Reads the incoming request data into a buffer and converts it to a string.

2. **Extract Request Information**
    - Parses the request line to get the HTTP method and URL path.
    - Extracts headers such as `Accept-Encoding`, `User-Agent`, and `Content-Length`.

3. **Handle File Upload (`POST /files/`)**
    - Extracts the file path from the URL.
    - Validates the request and extracts the file content.
    - Writes the content to a file in the specified directory.
    - Responds with `201 Created` if successful or `400 Bad Request` if invalid.

4. **Handle Echo Request (`GET /echo/`)**
    - Extracts the string after `/echo/` from the URL.
    - Checks if gzip compression is requested in the `Accept-Encoding` header.
    - Compresses the response body if gzip is acceptable.
    - Sends the response with `200 OK` and `Content-Encoding: gzip` if compressed.

5. **Handle User-Agent Request (`GET /user-agent`)**
    - Responds with the `User-Agent` header value.

6. **Handle File Reading (`GET /files/`)**
    - Extracts the file path from the URL.
    - Reads the file content and checks if gzip compression is requested.
    - Compresses the file content if gzip is acceptable.
    - Sends the file content with `200 OK` and `Content-Encoding: gzip` if compressed, or `404 Not Found` if the file does not exist.

7. **Default Response**
    - Responds with `404 Not Found` for unrecognized URLs.

8. **Close the Connection**
    - Closes the socket connection after handling the request.

