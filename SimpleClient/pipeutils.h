// Connect to the C# named pipe with the given name.
int connectToSocket(char *pipeName);

// Disconnect from the C# named pipe using the file descriptor returned
// from a call to `connectToSocket()`.
void disconnectFromSocket(int fd);

// Send a message through the socket.
void sendToSocket(int sock, char *msg);

// Read the server's response over the socket.
char* readFromSocket(int sock);
