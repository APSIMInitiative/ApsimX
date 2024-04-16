#include <stdint.h>
#include <stddef.h>

// void sendToSocket(int sock, const char *msg, size_t len);
// char* readFromSocket(int sock, uint32_t* len);
void sendReplacementToSocket(int sock, replacement_t* change);