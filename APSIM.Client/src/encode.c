#include <assert.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include "encode.h"

/*
Reverse the array in-place.
@param data: array to be reversed.
@param len: length of the array.
*/
void reverse(char* data, uint32_t len) {
    for (uint32_t i = 0; i < len / 2; i++) {
        int indexFromEnd = len - i - 1;
        char tmp = data[i];
        data[i] = data[indexFromEnd];
        data[indexFromEnd] = tmp;
    }
}

/*
Convert binary data from host endian-ness to little-endian.
@param data: data to be converted. This will be modified.
@param len: Length of the data.
*/
void toLittleEndian(char* data, uint32_t len) {
#if __BYTE_ORDER__ == __ORDER_BIG_ENDIAN__
    // Host architecture is big-endian. Therefore reverse the data.
    reverse(data, len);
#endif
}

/*
Convert binary data from little-endian to host endian-ness.
@param data: data to be converted. This will be modified.
@param len: Length of the data.
*/
void fromLittleEndian(char* data, uint32_t len) {
#if __BYTE_ORDER__ == __ORDER_BIG_ENDIAN__
    // Host architecture is big-endian. Therefore reverse the data.
    reverse(data, len);
#endif
}

/*
Convert integer value to binary suitable for transmission to the server.
The return value must be freed by the caller.
@param value: the integer value to be converted.
@param len: (out parameter) length of the return value.
*/
char* encodeInt(int32_t value, uint32_t* len) {
    *len = sizeof(int32_t);
    char* data = malloc(*len);
    memcpy(data, &value, *len);
    toLittleEndian(data, *len);
    return data;
}

/*
Parse an integer from little-endian binary data, as received from the server.
@param data: binary data received from the server.
@param n: Length of binary data.
*/
int32_t decodeInt(const char* data, uint32_t n) {
    assert(n == sizeof(int32_t));
    char data_cpy[n];
    memcpy(data_cpy, data, n);
    fromLittleEndian(data_cpy, n);
    int32_t value;
    memcpy(&value, data_cpy, n);
    return value;
}

/*
Convert a double value to binary suitable for transmission to the server.
The return value must be freed by the caller.
@param value: the value to be converted.
@param len: (out parameter) length of the return value.
*/
char* encodeDouble(double value, uint32_t* len) {
    *len = sizeof(double);
    char* data = malloc(*len);
    memcpy(data, &value, *len);
    toLittleEndian(data, *len);
    return data;
}

/*
Parse a double from little-endian binary data, as received from the server.
@param data: binary data received from the server.
@param n: Length of binary data.
*/
double decodeDouble(const char* data, uint32_t n) {
    assert(n == sizeof(double));

    // Convert to native endian-ness.
    char data_cpy[n];
    memcpy(data_cpy, data, n);
    fromLittleEndian(data_cpy, n);

    double value;
    memcpy(&value, data_cpy, n);
    return value;
}

/*
Convert a double array to binary suitable for transmission to the server.
The return value must be freed by the caller.
@param values: the data to be converted.
@param n: Number of elements in array.
@param len: (out parameter) Length of the return value.
*/
char* encodeDoubleArray(const double* values, uint32_t n, uint32_t* len) {
    *len = n * sizeof(double);
    char* result = malloc(*len);

    // Convert each value to little-endian individually and store in array.
    for (uint32_t i = 0; i < n; i++) {
        uint32_t x;
        char* encoded = encodeDouble(values[i], &x);
        memcpy(result + i * sizeof(double), encoded, sizeof(double));
        free(encoded);
    }

    return result;
}

/*
Parse a double array from little-endian binary data, as received from the server.
The return value must be freed by the caller.
@param data: binary data received from the server.
@param n: Length of binary data.
@param len: (out parameter) Number of elements in the returned array.
*/
double* decodeDoubleArray(const char* data, uint32_t n, uint32_t* len) {
    // Ensure that the data length is an exact multiple of length of a double.
    assert(n % sizeof(double) == 0);
    *len = n / sizeof(double);

    double* result = calloc(*len, sizeof(double));
    for (uint32_t i = 0; i < *len; i++) {
        const char* chunk = data + i * sizeof(double);
        result[i] = decodeDouble(chunk, sizeof(double));
    }
    return result;
}
