#include <assert.h>
#include <errno.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <sys/socket.h>
#include <unistd.h>

#include "protocol.h"
#include "encode.h"

/*
Send the data to the server. Errors will cause a failed assertion and program
termination.

@param sock: Server socket.
@param data: Data to be sent.
@param len: Length of the data.
*/
void sendToSocket(int sock, const char* data, uint32_t len) {
    int err = send(sock, data, len, 0);
    if (err == -1) {
        fprintf(stderr, "send() failure: %s\n", strerror(errno));
        assert (err >= 0);
    }
}

/*
Read the specified number of bytes from the server. Return the number of bytes
read.
@param sock: Server socket.
@param expected_length: Expected number of bytes to be read.
@param resp: Data from the server will be stored here.
*/
uint32_t readFromServer(int sock, uint32_t expected_length, char* resp) {
    uint32_t total_read = 0;
    while (total_read < expected_length) {
        // Read from server at the current offset into resp.
        int err = read(sock, resp + total_read, expected_length - total_read);

        // Error checking.
        if (err < 0) {
            fprintf(stderr, "read() failure: %s\n", strerror(errno));
            assert (err >= 0);
        }
        total_read += err;

        // Exit now if server has sent nothing.
        // fixme: resp may not have been initialized at this point.
        if (err == 0) {
        	break;
        }
    }

    return total_read;
}

/*
Read the specified number of bytes from the server, and assert that the correct
number of bytes has been read.
@param sock: Server socket.
@param expected_length: Expected number of bytes to be read.
@param resp: Data from the server will be stored here.
*/
uint32_t readFromServerStrict(int sock, uint32_t expected_length, char* resp) {
    uint32_t total_read = readFromServer(sock, expected_length, resp);
    if (total_read != expected_length) {
        fprintf(stderr, "read() failure: Expected %d bytes but received %d\n", expected_length, total_read);
        assert(total_read == sizeof(int));
    }
    return total_read;
}

/*
Send an integer to the server.
@param sock: Server socket.
@param value: The value to send.
*/
void sendInt(int sock, int value) {
    uint32_t len;
    char* bin = encodeInt(value, &len);
    sendToSocket(sock, bin, len);
    free(bin);
}

/*
Read an integer from the server.
@param sock: Server socket.
*/
int32_t readInt(int sock) {
    char resp[sizeof(int)];
    readFromServerStrict(sock, sizeof(int), resp);
    return decodeInt(resp, sizeof(int));
}

/*
Send a double to the server.
@param sock: Server socket.
@param value: The value to send.
*/
void sendDouble(int sock, double value) {
    uint32_t len;
    char* bin = encodeDouble(value, &len);
    sendToSocket(sock, bin, len);
    free(bin);
}

/*
Read a double from the server.
@param sock: Server socket.
*/
double readDouble(int sock) {
    char resp[sizeof(double)];
    readFromServerStrict(sock, sizeof(double), resp);
    return decodeDouble(resp, sizeof(double));
}

/*
Send a string to the server.
todo: encoding!! This probably doesn't work for non-ascii text.
@param sock: Server socket.
@param string: The utf-8 formatted string.
*/
void sendString(int sock, const char* string) {
    sendInt(sock, strlen(string));
    sendToSocket(sock, string, strlen(string));
}

/*
Read a string from the server. The return value must be freed by the caller.
todo: encoding!! This probably doesn't work for non-ascii text.
@param sock: Server socket.
@param len: Length of the string.
*/
char* readString(int sock, uint32_t* len) {
    int32_t length = readInt(sock);

    // Allocate an extra character, for null terminator.
    char* result = malloc(length + 1);
    result[length] = 0; // (n - 1)-th index.

    *len = readFromServerStrict(sock, length, result);
    return result;
}

/*
Send an array of doubles to the server.
@param sock: Server socket.
@param values: The values to send.
@param n: Number of values in the array.
*/
void sendDoubleArray(int sock, const double* values, uint32_t n) {
    uint32_t len;
    char* bin = encodeDoubleArray(values, n, &len);
    sendToSocket(sock, bin, len);
    free(bin);
}

/*
Read a double array from the server.
@param sock: Server socket.
@param result: (out parameter) Data read from the server.
@return Number of elements in the array.
*/
uint32_t readDoubleArray(int sock, double* result) {
    uint32_t message_length = readInt(sock);

    // Ensure that the response is a whole number of elements (ie message size
    // should be a multiple of double size).
    if (message_length % sizeof(double) != 0) {
        fprintf(stderr, "While reading double array: received %d bytes from server, which is not a whole number of doubles.\n", message_length);
        assert (message_length % sizeof(double) == 0);
    }

    uint32_t n = message_length / sizeof(double);

    // Read results, one at a time.
    for (uint32_t i = 0; i < n; i++) {
        result[i] = readDouble(sock);
    }

    return n;
}

/*
Read raw binary data from the server, but don't attempt to interpret it.
Return value must be freed by the caller.
@param sock: Server socket.
@param len: (out parameter) Length of the returned data.
@return The raw data from the server.
*/
char* readFromSocket(int sock, uint32_t* len) {
    int32_t length = readInt(sock);

    // Allocate an extra character, for null terminator.
    char* result = malloc(length);

    *len = readFromServerStrict(sock, length, result);
    return result;
}
