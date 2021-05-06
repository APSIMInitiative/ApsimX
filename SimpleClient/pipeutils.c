#include <stdint.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <assert.h>
#include <unistd.h>
#include <stdlib.h>
#include "pipeutils.h"
#include <errno.h>

char* toBinary(int32_t in) {
    char* len = malloc(4 * sizeof(char));
    for (int i = 0; i < 4; i++) {
        len[i] = in >> (i * 8);
    }
    return len;
}

int32_t binToInt(char* bin) {
    int32_t result = 0;
    for (int i = 0; i < 4; i++) {
        result += (int)bin[i] * (1 << (i * 8));
    }
    return result;
}

// Connect to the C# named pipe with the given name.
int connectToSocket(char *pipeName) {
    // The .NET Runtime will always generate the pipe at this path.
    char *pipePrefix = "/tmp/CoreFxPipe_";
    char pipe[strlen(pipeName + strlen(pipePrefix))];
    strcpy(pipe, pipePrefix);
    strcat(pipe, pipeName);

    int sock = socket(AF_UNIX, SOCK_STREAM, 0);
    struct sockaddr_un address;
    address.sun_family = AF_UNIX;
    strcpy(address.sun_path, pipe);
    int err;
    while ( (err = connect(sock, (struct sockaddr *)&address, sizeof(address)) < 0) || errno == EINTR);
    assert(err >= 0);
    return sock;
}

// Disconnect from the C# named pipe using the file descriptor returned
// from a call to `connectToSocket()`.
void disconnectFromSocket(int fd) {
    int err = close(fd);
    assert(err >= 0);
}

// Send a message over the socket connection, in a format that APSIM
// will recognise.
// The protocol is as folows:
// 1. Send 4 bytes indicating message length.
// 2. Send message (number of bytes must be as indicated earlier).
//
// Therefore, the max message length (in chars) is 2^31 - 1. This
// is because apsim reads message length as a 32-bit signed int.
void sendToSocket(int sock, char *msg) {
    // Ensure the message is not too long.
    long maxLength = ((long)1 << 31) - 1;
    assert(strlen(msg) < maxLength);

    // Send message length.
    char* len = toBinary((int32_t)strlen(msg));
    int err = send(sock, len, 4, 0);
    assert(err >= 0);
    free(len);

    // Send the message itself.
    err = send(sock, msg, strlen(msg), 0);
    assert(err >= 0);
}

// Read the server's response over the socket.
char* readFromSocket(int sock) {
    // Read message length (4 bytes).
    char length[4];
    int err = read(sock, length, 4);
    assert(err >= 0);

    // Read n bytes
    int32_t n = binToInt(length);
    char* resp = malloc(n * sizeof(char));
    err = read(sock, resp, n);
    assert(err >= 0);
    return resp;
}
