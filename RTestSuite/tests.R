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
# @Param y:double - minimum allowed value (exclusive)
##################################
GreaterThan <- function (x, y, ...) {
  all(ifelse( x > as.numeric(y), TRUE, FALSE))
}

############# LessThan ############
# Return true if all values in x < y
# @Param x:multi - list
# @Param y:double - maximum allowed value (exclusive)
##################################
LessThan <- function (x, y, ...) {
  all(ifelse( x < as.numeric(y), TRUE, FALSE))
}

############# Between ############
# Return true if all values in x <= y and >= z
# @Param x:multi - list
# @Param y:double - minimum allowed value (inclusive)
# @Param z:double - maximum allowed value (inclusive)
##################################
Between <- function (x, y, z, ...) {
  all(ifelse(x >= as.numeric(y) &  x <= as.numeric(z), TRUE, FALSE))
}

############# Mean ############
# Return true if mean of vector x is within +-y% of z
# @Param x:multi - list
# @Param y:double - percentage tolerance
# @Param z:double - reference value
##################################
Mean <- function (x, y, z, ...) {
  x <- unlist(x)
  ifelse(mean(x) < z + z * y / 100 &  mean(x) > z - z * y / 100, TRUE, FALSE)
}