// Connect to the APSIM Server with the specified name. Returns the
// socket FD which is used by the other functions here.
int connectToServer(char* name);

// Tell the server to re-run the file with the specified changes.
void runWithChanges(int sock, struct Replacement** changes, unsigned int n);

// Disconnect from the APSIM Server specified by fd.
void disconnectFromServer(int fd);
