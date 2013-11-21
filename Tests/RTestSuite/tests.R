# ApsimX Tests
# All tests must return TRUE or FALSE
# All tests must accept unknown arguments (by adding ...)
# Note that R will pass the data in as a list.
# You might need to unlist() it to get it to work. 

############# Output #############
# On a failed test, print out all values along with T/F list
# @Param x:multi - list
# @Param output:bool - results of the test
# @Param func:string - name of the calling test
######################################
Output <- function(x, passed, output, func, params=NA, baseData=NA, ...) {
  split <- unlist(strsplit(as.character(Sys.time()), " ", fixed=TRUE))
  date <-split[1]
  time <- split[2]
  temp <- list()
  output <- cbind(x,output)
  #reorder the columns
  index <- 0
  for (i in seq(1, ncol(output) / 2, by = 1)) {
    index <- c(index, i, i + ncol(output) / 2)
  }
  index <- index[-1]
  output <- output[, index]
  output <- cbind(baseData, output)
  output <- cbind(tests, output)
  output <- cbind(cols, output)
  output <- cbind(simsToTest, output)
  output <- cbind(time, output)
  output <- cbind(date, output)
  output <- cbind(output, paste(params, collapse=","))
  names(output) <- c("Date","Time","Simulation", "Column", "Test","BaseValue", "RunValue","Passed", "Paramaters")
  buildRecord <<- rbind(buildRecord, output)  
  print(head(output, n=10))
  return(passed)
}

############# AllPos #############
# Return true if all values in x >= 0
# @Param x:multi - list
# @Param func:string - name of the calling test
##################################
AllPos <- function(x, func, ...) {
  output <- ifelse(x >= 0, TRUE, FALSE)
  
  ifelse(all(output), Output(x, TRUE, output, func), Output(x, FALSE, output, func))
}

############# GreaterThan ########
# Return true if all values in x > y
# @Param x:multi - list
# @Param func:string - name of the calling test
# @Param 1:double - minimum allowed value (exclusive)
##################################
GreaterThan <- function (x, func, params, ...) {
  output <- x > params[1]
  ifelse(all(output), Output(x, TRUE, output, func), Output(x, FALSE, output, func))
}

############# LessThan ############
# Return true if all values in x < y
# @Param x:multi  - list
# @Param func:string - name of the calling test
# @Param 1:double - maximum allowed value (exclusive)
##################################
LessThan <- function (x, func, params, ...) {
  output <- x < params[1]
  ifelse(all(output), Output(x, TRUE, output, func), Output(x, FALSE, output, func))
}

############# Between ############
# Return true if all values in x <= y and >= z
# @Param x:multi  - list
# @Param func:string - name of the calling test
# @Param 1:double - minimum allowed value (inclusive)
# @Param 2:double - maximum allowed value (inclusive)
##################################
Between <- function (x, func, params, ...) {
  output <- x >= params[1] &  x <= params[2]
  ifelse(all(output), Output(x, TRUE, output, func), Output(x, FALSE, output, func))
}

############# Mean ############
# Return true if mean of vector x is within +-y% of z
# @Param x:multi  - list
# @Param func:string - name of the calling test
# @Param 1:double - percentage tolerance
# @Param 2:double - reference value
##################################
Mean <- function (x, func, params, ...) {
  x <- unlist(x)
  output <- mean(x) <= params[2] + params[2] * params[1] / 100 &
            mean(x) >= params[2] - params[2] * params[1] / 100
  ifelse(output, Output(x, TRUE, output, func), Output(x, FALSE, output, func))
}

############# Tolerance ############
# Return true if each value in x is within +-[2](%) of [3]
# @Param x: multi  - list
# @Param func:string - name of the calling test
# @Param 1: bool   - 1; use percentage, 0; use absolute
# @Param 2: double - tolerance value (inclusive)
# @baseData: vector - reference data
##################################
Tolerance <- function (x, func, params, baseData, ...) {
  if (params[1]) {
      output <- abs(x) <= abs(baseData) + abs(baseData) * params[2] / 100 &
                abs(x) >= abs(baseData) - abs(baseData) * params[2] / 100
    } else {
      output <- x <= baseData + params[2] &
                x >= baseData - params[2]
    }  
   ifelse(all(output), Output(x, TRUE, output, func, params, baseData), Output(x, FALSE, output, func, params, baseData))
  }