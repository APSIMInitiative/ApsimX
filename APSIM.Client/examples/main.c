#include <stdio.h>
#include <stdlib.h>

// This should be moved eventually.
#include <assert.h>

// tmp
#include <string.h>
#include <time.h>
#include <sys/time.h>

#include "replacement.h"
#include "apsimclient.h"

double get_wall_time() {
    struct timeval time;
    if (gettimeofday(&time,NULL)){
        //  Handle error
        return 0;
    }
    return (double)time.tv_sec + (double)time.tv_usec * .000001;
}

int main(int argc, char** argv)
{
    double start_time = get_wall_time();

    // This is the name of the pipe as defined in the C# server code.
    char *pipeName = "testpipe";
    // char* ipAddress = argc > 1 ? argv[1] : "127.0.0.1";
    // uint16_t port = 27746;
    /*
    if (argc > 2) {
        int portNo = atoi(argv[2]);
        assert(portNo <= UINT16_MAX);
        assert(portNo >= 0);
        port = (uint16_t)portNo;
    }
    */
    int n_iter = 1;
    if (argc > 3) {
        int iter = atoi(argv[3]);
        assert(iter > 0);
        n_iter = iter;
    }

    // Connect to the socket.
    fprintf(stdout, "Connecting to server on %s...", pipeName);
    int sock = connectToServer(pipeName);
    fprintf(stdout, "connected\n");

    // OK, let's try and kick off a simulation run.
    // We will be mofifying the juvenile TT target.
    char* path = "Sand.Water";
    double value[] = {0.090, 0.100, 0.187, 0.197, 0.169, 0.196};
    for (int iter = 0; iter < n_iter; iter++) {
		if (iter != 0) {
	    	value[0] = 0.095;
	    	value[1] = 0.105; 
	    	value[2] = 0.192;
	    	value[3] = 0.202;
	    	value[4] = 0.174; 
	    	value[5] = 0.201;
		}
//        double value = minValue + iter * increment;
        replacement_t* change = createDoubleArrayReplacement(path, value, 6);
        fprintf(stdout, "Running sims with the following changes:\n");
        fprintf(stdout, "  %s = [%.3f, %.3f, %.3f, %.3f, %.3f, %.3f]\n", path, 
        	value[0], value[1], value[2], value[3], value[4], value[5]);
        double run_clock = get_wall_time();
        runWithChanges(sock, &change, 1);
        run_clock = get_wall_time() - run_clock;
        fprintf(stdout, "Ran sims in %.2fms\n", run_clock * 1000);

        replacement_free(change);

        // Let's read some outputs.
        char* table = "Report";
        uint32_t nparams = 1;
        char* params[nparams];
        params[0] = "Wheat.Grain.Total";
        // params[1] = "Sorghum.AboveGround.Wt";
        // params[2] = "Sorghum.Leaf.LAI";
        double t = get_wall_time();

        output_t** outputs = readOutput(sock, table, params, nparams);
        t = get_wall_time() - t;
        printf("Read %d outputs in %.2fms\n", nparams, t * 1000);
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
    }
    // Close the socket connection.
    printf("Disconnecting from server...\n");
    disconnectFromServer(sock);

    double duration = get_wall_time() - start_time;
    fprintf(stdout, "Total program execution: %.2fms\n", duration * 1000);
    return 0;
}
