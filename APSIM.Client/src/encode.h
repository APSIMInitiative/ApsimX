#ifndef __ENCODE_H__
#define __ENCODE_H__

#include <stdint.h>

// TBI: send/receive int array, bool, date, string

/*
Convert integer value to binary suitable for transmission to the server.
The return value must be freed by the caller.
@param value: the integer value to be converted.
@param len: (out parameter) length of the return value.
*/
char* encodeInt(int32_t value, uint32_t* len);

/*
Parse an integer from little-endian binary data, as received from the server.
@param data: binary data received from the server.
@param n: Length of binary data.
*/
int decodeInt(const char* data, uint32_t n);

/*
Convert a double value to binary suitable for transmission to the server.
The return value must be freed by the caller.
@param value: the value to be converted.
@param len: (out parameter) length of the return value.
*/
char* encodeDouble(double value, uint32_t* len);

/*
Parse a double from little-endian binary data, as received from the server.
@param data: binary data received from the server.
@param n: Length of binary data.
*/
double decodeDouble(const char* data, uint32_t n);

// /*
// Convert an integer array to binary suitable for transmission to the server.
// The return value must be freed by the caller.
// @param values: the data to be converted.
// @param n: Number of elements in array.
// @param len: (out parameter): Length of the return value.
// */
// char* encodeIntArray(const int32_t* values, uint32_t n, uint32_t* len);

// /*
// Parse an integer array from little-endian binary data, as received from the server.
// The return value must be freed by the caller.
// @param data: binary data received from the server.
// @param n: Length of binary data.
// @param len: (out parameter) Number of elements in the returned array.
// */
// int* decodeIntArray(const char* data, uint32_t n, uint32_t* len);

/*
Convert a double array to binary suitable for transmission to the server.
The return value must be freed by the caller.
@param values: the data to be converted.
@param n: Number of elements in array.
@param len: (out parameter) Length of the return value.
*/
char* encodeDoubleArray(const double* values, uint32_t n, uint32_t* len);

/*
Parse a double array from little-endian binary data, as received from the server.
The return value must be freed by the caller.
@param data: binary data received from the server.
@param n: Length of binary data.
@param len: (out parameter) Number of elements in the returned array.
*/
double* decodeDoubleArray(const char* data, uint32_t n, uint32_t* len);

#endif
