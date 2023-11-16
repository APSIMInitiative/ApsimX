#ifndef __PROTOCOL_H__
#define __PROTOCOL_H__

#include <stdint.h>

/*
Send an integer to the server.
@param sock: Server socket.
@param value: The value to send.
*/
void sendInt(int sock, int value);

/*
Read an integer from the server.
@param sock: Server socket.
*/
int32_t readInt(int sock);

/*
Send a double to the server.
@param sock: Server socket.
@param value: The value to send.
*/
void sendDouble(int sock, double value);

/*
Read a double from the server.
@param sock: Server socket.
*/
double readDouble(int sock);

/*
Send a string to the server.
todo: encoding!! This probably doesn't work for non-ascii text.
@param sock: Server socket.
@param string: The utf-8 formatted string.
*/
void sendString(int sock, const char* string);

/*
Read a string from the server. The return value must be freed by the caller.
todo: encoding!! This probably doesn't work for non-ascii text.
@param sock: Server socket.
@param len: Length of the string.
*/
char* readString(int sock, uint32_t* len);

/*
Send an array of doubles to the server.
@param sock: Server socket.
@param values: The values to send.
@param n: Number of values in the array.
*/
void sendDoubleArray(int sock, const double* values, uint32_t n);

/*
Read a double array from the server.
@param sock: Server socket.
@param result: (out parameter) Data read from the server.
@return Number of elements in the array.
*/
uint32_t readDoubleArray(int sock, double* result);

/*
Send the provided binary data.
@param sock: Server socket.
@param data: Data to send.
@param len: Data message length.
*/
void sendToSocket(int sock, const char* data, uint32_t len);

/*
Read raw binary data from the server, but don't attempt to interpret it.
Return value must be freed by the caller.
@param sock: Server socket.
@param len: (out parameter) Length of the returned data.
@return The raw data from the server.
*/
char* readFromSocket(int sock, uint32_t* len);

#endif
