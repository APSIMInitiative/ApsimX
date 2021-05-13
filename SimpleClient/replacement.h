#include <stdint.h>
struct Replacement
{
    char* path;
    int32_t paramType;
    char* value;
    int value_len;
};

struct Replacement* createIntReplacement(char* path, int32_t value);
struct Replacement* createDoubleReplacement(char* path, double value);
// TODO: bool, date, string replacements.
