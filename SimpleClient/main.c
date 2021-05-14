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
    char* path = "[Sorghum].Phenology.Juvenile.Target.FixedValue";
    double value = 120.5;
    struct Replacement* change = createDoubleReplacement(path, value);
    printf("Running sims with the following changes:\n");
    printf("  %s = %.2f\n", path, value);
    runWithChanges(sock, &change, 1);
    free(change->value);
    free(change);

    // Let's read some outputs.
    char* table = "Report";
    uint32_t nparams = 3;
    char* params[nparams];
    params[0] = "Sorghum.Phenology.Juvenile.Target.FixedValue";
    params[1] = "Sorghum.AboveGround.Wt";
    params[2] = "Sorghum.Leaf.LAI";
    struct output** outputs = readOutput(sock, table, params, nparams);

    for (uint32_t i = 0; i < nparams; i++) {
        uint32_t n = outputs[i]->len / sizeof(double); // all of our outputs are double for now
        printf("Received output %s with %u elements: [", params[i], n);
        for (size_t j = 0; j < n; j++) {
            double val;
            memcpy(&val, &outputs[i]->data[j * sizeof(double)], sizeof(double));
            printf("%.2f%s", val, j < n - 1 ? ", " : "");
        }
        free(outputs[i]->data);
        free(outputs[i]);
        printf("]\n\n");
    }
    free(outputs);

    // Close the socket connection.
    printf("Disconnecting from server...\n");
    disconnectFromServer(sock);
    return 0;
}
