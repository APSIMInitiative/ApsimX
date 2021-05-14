#include <stdint.h>
struct output
{
    char* data;
    uint32_t len;
};

// Connect to the APSIM Server with the specified name. Returns the
// socket FD which is used by the other functions here.
int connectToServer(char* name);

// Tell the server to re-run the file with the specified changes.
void runWithChanges(int sock, struct Replacement** changes, unsigned int n);

// Disconnect from the APSIM Server specified by fd.
void disconnectFromServer(int fd);

// Read the simulation output with the given name from the
// specified table. It's up to the caller to cast/interpret
// the return value.
struct output** readOutput(int sock, char* table, char** param_names, uint32_t nparams);
