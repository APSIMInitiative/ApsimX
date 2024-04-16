#ifndef _REPLACEMENT_H
#define _REPLACEMENT_H

#include <stdint.h>
typedef struct Replacement
{
    char* path;
    int32_t paramType;
    char* value;
    int value_len;
} replacement_t;

replacement_t* createIntReplacement(char* path, int32_t value);
replacement_t* createDoubleReplacement(char* path, double value);

/*
Create a replacement representing a change for a numeric array.

@param path: Apsim path to the variable to be changed
@param value: The new value of the variable
@param length: Length of the value array

@return a replacement representing a change for the numeric array.
*/
replacement_t* createDoubleArrayReplacement(char* path, double* value, int length);

// Free a replacement instance.
void replacement_free(replacement_t* replacement);

// TODO: bool, date, string replacements.

#endif
