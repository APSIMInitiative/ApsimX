# ApsimX Tests
# All tests must return TRUE or FALSE
# All tests must accept unknown arguments (by adding ...)
# Note that R will pass the data in as a list.
# You might need to unlist() it to get it to work.

############# AllPos #############
# Return true if all values in x >= 0
# @Param x:multi - list
##################################
AllPos <- function(x, ...) {
  all(ifelse(x >= 0, TRUE, FALSE))
}

############# GreaterThan ########
# Return true if all values in x > y
# @Param x:multi - list
# @Param 1:double - minimum allowed value (exclusive)
##################################
GreaterThan <- function (x, params, ...) {
  all(ifelse( x > params[1], TRUE, FALSE))
}

############# LessThan ############
# Return true if all values in x < y
# @Param x:multi  - list
# @Param 1:double - maximum allowed value (exclusive)
##################################
LessThan <- function (x, params, ...) {
  all(ifelse( x < params[1], TRUE, FALSE))
}

############# Between ############
# Return true if all values in x <= y and >= z
# @Param x:multi  - list
# @Param 1:double - minimum allowed value (inclusive)
# @Param 2:double - maximum allowed value (inclusive)
##################################
Between <- function (x, params, ...) {
  all(ifelse(x >= params[1] &  x <= params[2], TRUE, FALSE))
}

############# Mean ############
# Return true if mean of vector x is within +-y% of z
# @Param x:multi  - list
# @Param 1:double - percentage tolerance
# @Param 2:double - reference value
##################################
Mean <- function (x, params, ...) {
  print(params)
  x <- unlist(x)
  ifelse(mean(x) < params[2] + params[2] * params[1] / 100 &
         mean(x) > params[2] - params[2] * params[1] / 100, TRUE, FALSE)
}

############# Tolerance ############
# Return true if each value in x is within +-[1](%) of [2]
# @Param x: multi  - list
# @Param 1: bool   - TRUE; use percentage, FALSE; use absolute
# @Param 2: double - tolerance value (inclusive)
# @Param 3: vector - reference data
##################################
Tolerance <- function (x, params, ...) {
  if (bool) {
    result <- x < params[3] + params[3] * params[2] / 100 &
              x > params[3] - params[3] * params[2] / 100
  } else {
    result <- x < params[3] + params[2] / 100 &
              x > params[3] - params[2] / 100
  }
  
   if(all(result)){
      return(TRUE)
   } else{
     print(paste(x, params[3], result, sep=", "))
     return(FALSE)
   }
  }