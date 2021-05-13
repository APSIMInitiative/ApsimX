#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include "utils.h"
#include "replacement.h"

const int PROPERTY_TYPE_INT = 0;
const int PROPERTY_TYPE_DOUBLE = 1;
const int PROPERTY_TYPE_BOOL = 2;
const int PROPERTY_TYPE_DATE = 3;
const int PROPERTY_TYPE_STRING = 4;

struct Replacement* createIntReplacement(char* path, int32_t value) {
    struct Replacement* result = malloc(sizeof(struct Replacement));
    result->path = path;
    result->paramType = PROPERTY_TYPE_INT;
    result->value = malloc(sizeof(int32_t));
    memcpy(result->value, &value, sizeof(int32_t));
    result->value_len = sizeof(int32_t);

    return result;
}

struct Replacement* createDoubleReplacement(char* path, double value) {
    struct Replacement* result = malloc(sizeof(struct Replacement));
    result->path = path;
    result->paramType = PROPERTY_TYPE_DOUBLE;
    // result->value = (char*)&value;
    result->value_len = sizeof(double);
    result->value = malloc(result->value_len);
    memcpy(result->value, &value, result->value_len);
    return result;
}
