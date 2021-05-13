#include <stdio.h>
#include <stdlib.h>

// This should be moved eventually.
#include <assert.h>

// tmp
#include <string.h>

#include "replacement.h"
#include "utils.h"
#include "client.h"

int main(int argc, char** argv)
{
    // This is the name of the pipe as defined in the C# server code.
    char *pipeName = "testpipe";

    // Connect to the socket.
    printf("Connecting to server...");
    fflush(stdout);
    int sock = connectToServer(pipeName);
    printf("connected\n");

    // OK, let's try and kick off a simulation run.
    // We will be mofifying the juvenile TT target.
    struct Replacement* change = createDoubleReplacement("[Sorghum].Phenology.Juvenile.Target.FixedValue", 120.5);
    runWithChanges(sock, &change, 1);
    free(change->value);
    free(change);

    // Close the socket connection.
    printf("Disconnecting from server...\n");
    disconnectFromServer(sock);
    return 0;
}
