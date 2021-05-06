#include <stdio.h>
#include <stdlib.h>
#include "pipeutils.h"

int main(int argc, char** argv)
{
    // This is the name of the pipe as defined in the C# server code.
    char *pipeName = "testpipe";

    // Connect to the socket.
    printf("Connecting to server...");
    int sock = connectToSocket(pipeName);
    printf("connected\n");

    // Send a message through the socket.
    sendToSocket(sock, "Hello from C!");

    // Read the server's response.
    char* response = readFromSocket(sock);
    printf("Response from server: %s\n", response);
    free(response);

    // Close the socket connection.
    printf("Disconnecting from server...\n");
    disconnectFromSocket(sock);
    return 0;
}
