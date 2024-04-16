#ifndef _APSIM_CLIENT_H
#define _APSIM_CLIENT_H

#include <stdint.h>
#include "replacement.h"

typedef struct output
{
    char* data;
    uint32_t len;
} output_t;

// Connect to the APSIM Server with the specified name. Returns the
// socket FD which is used by the other functions here.
int connectToServer(char* name);

// Connect to the APSIM Server running on a remote machine listening on
// the specified IP Address and port number.
//
// @param ip_addr: IPv4 Address of the server in ascii notation (e.g. "127.0.0.1")
// @param port: Port number on which the server is listening.
int connectToRemoteServer(char* ip_addr, uint16_t port);

// Tell the server to re-run the file with the specified changes.
void runWithChanges(int sock, replacement_t** changes, unsigned int n);

// Disconnect from the APSIM Server specified by fd.
void disconnectFromServer(int fd);

// Read the simulation output with the given name from the
// specified table. It's up to the caller to cast/interpret
// the return value.
output_t** readOutput(int sock, char* table, char** param_names, uint32_t nparams);

#endif
