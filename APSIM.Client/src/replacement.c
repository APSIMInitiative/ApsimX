#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include "replacement.h"

const int PROPERTY_TYPE_INT = 0;
const int PROPERTY_TYPE_DOUBLE = 1;
const int PROPERTY_TYPE_BOOL = 2;
const int PROPERTY_TYPE_DATE = 3;
const int PROPERTY_TYPE_STRING = 4;
const int PROPERTY_TYPE_INT_ARRAY = 5;
const int PROPERTY_TYPE_DOUBLE_ARRAY = 6;
const int PROPERTY_TYPE_BOOL_ARRAY = 7;
const int PROPERTY_TYPE_DATE_ARRAY = 8;
const int PROPERTY_TYPE_STRING_ARRAY = 9;

replacement_t* createIntReplacement(char* path, int32_t value) {
    replacement_t* result = malloc(sizeof(replacement_t));

    size_t pathLen = strlen(path);
    result->path = calloc(pathLen, sizeof(char));
    memcpy(result->path, path, pathLen);

    result->paramType = PROPERTY_TYPE_INT;
    result->value = malloc(sizeof(int32_t));
    memcpy(result->value, &value, sizeof(int32_t));
    result->value_len = sizeof(int32_t);

    return result;
}

replacement_t* createDoubleReplacement(char* path, double value) {
    replacement_t* result = malloc(sizeof(replacement_t));

    size_t pathLen = strlen(path);
    result->path = calloc(pathLen, sizeof(char));
    memcpy(result->path, path, pathLen);

    result->paramType = PROPERTY_TYPE_DOUBLE;
    // result->value = (char*)&value;
    result->value_len = sizeof(double);
    result->value = malloc(result->value_len);
    memcpy(result->value, &value, result->value_len);
    return result;
}

/*
Create a replacement representing a change for a numeric array.

@param path: Apsim path to the variable to be changed
@param value: The new value of the variable
@param length: Length of the value array

@return a replacement representing a change for the numeric array.
*/
replacement_t* createDoubleArrayReplacement(char* path, double* value, int length) {
    replacement_t* result = malloc(sizeof(replacement_t));

    size_t pathLen = strlen(path);
    result->path = calloc(pathLen, sizeof(char));
    memcpy(result->path, path, pathLen);

    result->paramType = PROPERTY_TYPE_DOUBLE_ARRAY;
    result->value_len = length * sizeof(double);
    result->value = malloc(result->value_len);
    memcpy(result->value, value, result->value_len);
    return result;
}

// Free a replacement instance.
void replacement_free(replacement_t* replacement) {
    free(replacement->path);
    free(replacement->value);
    free(replacement);
}
