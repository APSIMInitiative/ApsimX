#include <stdint.h>
#include <assert.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <sys/un.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <sys/socket.h>
#include <unistd.h>

#include "replacement.h"
#include "apsimclient.h"
#include "client-private.h"
#include "protocol.h"

const char* ACK = "ACK";
const char* FIN = "FIN";
const char* COMMAND_RUN = "RUN";
const char* COMMAND_READ = "READ";

// Connect to the given socket address, blocking until a connection
// is established. Failed assertion will occur if a connection is
// not established.
void connectTo(int sock, struct sockaddr* address, socklen_t length) {
    int err;
    while ( (err = connect(sock, address, length) < 0) || errno == EINTR);
    if (err < 0 && errno != EINTR) {
        fprintf(stderr, "connect() failure: %s\n", strerror(errno));
    }
    assert(err >= 0);
}

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

    connectTo(sock, (struct sockaddr*)&address, sizeof(address));
    return sock;
}

// Connect to the APSIM Server running on a remote machine listening on
// the specified IP Address and port number.
//
// @param ip_addr: IPv4 Address of the server in ascii notation (e.g. "127.0.0.1")
// @param port: Port number on which the server is listening.
int connectToRemoteServer(char* ip_addr, uint16_t port) {
    int sock = socket(AF_INET, SOCK_STREAM, 0);
    struct sockaddr_in address;
    address.sin_family = AF_INET;
    address.sin_port = htons(port);
    address.sin_addr.s_addr = inet_addr(ip_addr);

    connectTo(sock, (struct sockaddr*)&address, sizeof(address));
    return sock;
}

// Disconnect from the APSIM Server specified by fd.
void disconnectFromServer(int fd) {
    int err = close(fd);
    assert(err >= 0);
}

// Read a message from the server and ensure that it matches the
// expected value.
void validateResponse(int sock, const char* expected) {
    uint32_t len;
    char* resp = readString(sock, &len);
    if (strcmp(resp, expected) != 0) {
        fprintf(stderr, "Expected response '%s' but got '%s'\n", expected, resp);
        assert(strcmp(resp, expected) == 0);
    }
    free(resp);
}

// Send the replacement/property change to the server.
// The protocol is to send the path, then parameter type, then value.
// The server should responsd with ACK after each message.
void sendReplacementToSocket(int sock, replacement_t* change) {
    // 1. Send parameter path.
    sendString(sock, change->path);
    validateResponse(sock, ACK);

    // 2. Send parameter type.
    sendInt(sock, change->paramType);
    validateResponse(sock, ACK);

    // 3. Send the parameter itself.
    sendToSocket(sock, change->value, change->value_len);
    validateResponse(sock, ACK);
}

// Tell the server to re-run the file with the specified changes.
void runWithChanges(int sock, replacement_t** changes, unsigned int n) {
    sendString(sock, COMMAND_RUN);
    validateResponse(sock, ACK);
    for (int i = 0; i < n; i++) {
        sendReplacementToSocket(sock, changes[i]);
    }
    sendString(sock, FIN);
    validateResponse(sock, ACK);
    // Server will send through a second response when the command finishes
    // (FIN for success, otherwise a longer string detailing the error).
    uint32_t msg_len;
    char* resp = readString(sock, &msg_len);
    int err = strcmp(resp, FIN) != 0;
    if (err) {
        fprintf(stderr, "Command ran with errors: %.*s\n", msg_len, resp);
    }
    free(resp);
    assert(!err);
}

// Read the simulation output with the given name from the
// specified table. It's up to the caller to cast/interpret
// the return value.
//
// The protocol is:
// 1. Send READ command
// 2. Receive ACK
// 3. Send table name
// 4. Receive ACK
// 5. Send parameter names one at a time (receive ACK after each).
// 5a) Send number of items in array
// 5b) Receive ACK
// 5c) Send items one by one (receive ACK after each)
// 6. Send FIN
// 7. Receive FIN (after command finishes running)
// 7a.If command ran with error, we will receive error message instead of FIN
// 8. Send ACK
// 9. Receive one message per parameter name sent. Send ACK after each.
output_t** readOutput(int sock, char* table, char** param_names, uint32_t nparams) {
    // 1. Send READ command.
    sendString(sock, COMMAND_READ);
    // 2. Receive ACK.
    validateResponse(sock, ACK);
    // 3. Send table name.
    sendString(sock, table);
    // 4. Receive ACK.
    validateResponse(sock, ACK);
    // 5. Send parameter names one at a time.
    for (uint32_t i = 0; i < nparams; i++) {
        sendString(sock, param_names[i]);
        // Should receive ACK after each message.
        validateResponse(sock, ACK);
    }
    // Send FIN to indicate end of parameter names.
    sendString(sock, FIN);

    // Server will send FIN if command executed succesfully, or an error
    // message otherwise.
    // Server will send through a second response when the command finishes
    // (FIN for success, otherwise a longer string detailing the error).
    uint32_t msg_len;
    char* resp = readString(sock, &msg_len);
    int err = strcmp(resp, FIN) != 0;
    if (err) {
        fprintf(stderr, "ReadCommand ran with errors: %.*s\n", msg_len, resp);
    }
    free(resp);
    assert(!err);

    // Send ACK.
    sendString(sock, ACK);

    output_t** outputs = malloc(nparams * sizeof(output_t));

    // Now we should receive one result per parameter name.
    for (uint32_t i = 0; i < nparams; i++) {
        outputs[i] = malloc(sizeof(output_t));
        outputs[i]->data = readFromSocket(sock, &outputs[i]->len);
        sendString(sock, ACK);
    }
    return outputs;
}
