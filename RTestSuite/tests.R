# ApsimX Tests
# All tests must return TRUE or FALSE
# All tests must accept unknown arguments (by adding ...)

############# AllPos #############
# Return true if all values in x >= 0
# @Param x:multi - numerical vector or data frame
##################################
AllPos <- function(x, ...) {
  all(ifelse(x >= 0, TRUE, FALSE))
  }

############# GreaterThan ########
# Return true if all values in x > y
# @Param x:multi - numerical vector or data frame
# @Param y:double - minimum allowed value (exclusive)
##################################
GreaterThan <- function (x, y, ...) {
  all(ifelse( x > as.numeric(y), TRUE, FALSE))
}

############# LessThan ############
# Return true if all values in x < y
# @Param x:multi - numerical vector or data frame
# @Param y:double - maximum allowed value (exclusive)
##################################
LessThan <- function (x, y, ...) {
  all(ifelse( x < as.numeric(y), TRUE, FALSE))
}

############# Between ############
# Return true if all values in x <= y and >= z
# @Param x:multi - numerical vector or data frame
# @Param y:double - minimum allowed value (inclusive)
# @Param z:double - maximum allowed value (inclusive)
##################################
Between <- function (x, y, z, ...) {
  all(ifelse(x >= as.numeric(y) &  x <= as.numeric(z), TRUE, FALSE))
}