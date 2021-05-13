#include <stdint.h>
#include <assert.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <sys/un.h>
#include <sys/socket.h>
#include <unistd.h>

#include "replacement.h"

const char* ACK = "ACK";
const char* FIN = "FIN";
const char* COMMAND_RUN = "RUN";

// Connect to the APSIM Server with the specified name.
int connectToServer(char* name) {
    // The .NET Runtime will always generate the pipe at this path.
    char *pipePrefix = "/tmp/CoreFxPipe_";
    char pipe[strlen(name) + strlen(pipePrefix)];
    strcpy(pipe, pipePrefix);
    strcat(pipe, name);

    int sock = socket(AF_UNIX, SOCK_STREAM, 0);
    struct sockaddr_un address;
    address.sun_family = AF_UNIX;
    strcpy(address.sun_path, pipe);
    int err;
    while ( (err = connect(sock, (struct sockaddr *)&address, sizeof(address)) < 0) || errno == EINTR);
    assert(err >= 0);
    return sock;
}

// Disconnect from the APSIM Server specified by fd.
void disconnectFromServer(int fd) {
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
void sendToSocket(int sock, const char *msg, size_t len) {
    // Ensure the message is not too long.
    long maxLength = ((long)1 << 31) - 1;
    assert(len < maxLength);

    // Send message length.
    int err = send(sock, (char*)&len, 4, 0);
    assert(err >= 0);

    // Send the message itself.
    err = send(sock, msg, len, 0);
    assert(err >= 0);
}

// Read the server's response over the socket.
char* readFromSocket(int sock) {
    // Read message length (4 bytes).
    char length_raw[4];
    int err = read(sock, length_raw, 4);
    assert(err >= 0);

    // Read n bytes
    int32_t n = (int32_t)(*length_raw) + 1;
    char* resp = malloc(n * sizeof(char));
    resp[n - 1] = 0;
    err = read(sock, resp, n);
    assert(err >= 0);
    return resp;
}

// Read a message from the server and ensure that it matches the
// expected value.
void validateResponse(int sock, const char* expected) {
    char* resp = readFromSocket(sock);
    if (strcmp(resp, expected) != 0) {
        printf("Expected response '%s' but got '%s'\n", expected, resp);
        assert(strcmp(resp, expected) == 0);
    }
    free(resp);
}

// Send a text message to the server. This is just a shorthand for
// calling sendToSocket with strlen() for the length parameter.
void sendStringToSocket(int sock, const char* msg) {
    sendToSocket(sock, msg, strlen(msg));
}

// Send the replacement/property change to the server.
// The protocol is to send the path, then parameter type, then value.
// The server should responsd with ACK after each message.
void sendReplacementToSocket(int sock, struct Replacement* change) {
    // 1. Send parameter path.
    sendStringToSocket(sock, change->path);
    validateResponse(sock, ACK);

    // 2. Send parameter type.
    sendToSocket(sock, (char*)&change->paramType, sizeof(int32_t));
    validateResponse(sock, ACK);

    // 3. Send the parameter itself.
    sendToSocket(sock, change->value, change->value_len);
    validateResponse(sock, ACK);
}

// Tell the server to re-run the file with the specified changes.
void runWithChanges(int sock, struct Replacement** changes, unsigned int n) {
    sendStringToSocket(sock, COMMAND_RUN);
    validateResponse(sock, ACK);
    for (int i = 0; i < n; i++) {
        sendReplacementToSocket(sock, changes[i]);
    }
    sendStringToSocket(sock, FIN);
    validateResponse(sock, ACK);
    // Server will send through a second response when the command finishes
    // (FIN for success, otherwise a longer string detailing the error).
    char* resp = readFromSocket(sock);
    if (strcmp(resp, FIN) == 0) {
        printf("Command ran successfully.\n");
    } else {
        printf("Command ran with errors: %s\n", resp);
    }
    free(resp);
}
