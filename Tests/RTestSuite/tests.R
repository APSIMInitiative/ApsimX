# ApsimX Tests
# All tests must return TRUE or FALSE
# All tests must accept unknown arguments (by adding ...)
# Note that R will pass the data in as a list.
# You might need to unlist() it to get it to work. 

############# Output #############
# On a completed test, print out all values along with T/F list
# @Param x:multi - list
# @Param passed: bool - did the test pass?
# @Param output:bool - results of the test
# @Param func:string - name of the calling test
# @Param params: tests paramaters, if any
# @Param baseData: baseline data for the test
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
  output <- cbind(args[1], output)
  output <- cbind(args[2], output)
  output <- cbind(output, paste(params, collapse=","))
  names(output) <- c("BuildID", "System", "Date","Time","Simulation", "ColumnName", "Test","BaseValue", "RunValue","Passed", "Paramaters")
  buildRecord <<- rbind(buildRecord, output)
  if (!passed)
    haveTestsPassed <<- FALSE
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
  ifelse(all(output), Output(x, TRUE, output, func, params), Output(x, FALSE, output, func, params))
}

############# LessThan ############
# Return true if all values in x < y
# @Param x:multi  - list
# @Param func:string - name of the calling test
# @Param 1:double - maximum allowed value (exclusive)
##################################
LessThan <- function (x, func, params, ...) {
  output <- x < params[1]
  ifelse(all(output), Output(x, TRUE, output, func, params), Output(x, FALSE, output, func, params))
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
  ifelse(all(output), Output(x, TRUE, output, func, params), Output(x, FALSE, output, func, params))
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
  ifelse(output, Output(x, TRUE, output, func, params), Output(x, FALSE, output, func, params))
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
  if (is.na(baseData))
    stop("Tolerance test requires a baseline and one was not found.")
  if (params[1]) {
      output <- abs(x) <= abs(baseData) + abs(baseData) * params[2] / 100 &
                abs(x) >= abs(baseData) - abs(baseData) * params[2] / 100
    } else {
      output <- x <= baseData + params[2] &
                x >= baseData - params[2]
    }  
   ifelse(all(output), Output(x, TRUE, output, func, params, baseData), Output(x, FALSE, output, func, params, baseData))
  }
  
############# CompareToInput ############
# Return true if each value in x is within +-[2](%) of [3]
# Similar to Tolerance but uses an Input table instead of a seperate .db
# @Param x: multi  - list
# @Param func:string - name of the calling test
# @Param 1: bool   - 1; use percentage, 0; use absolute
# @Param 2: double - tolerance value (inclusive)
# @baseData: vector - reference data
##################################
CompareToInput <- function (x, func, params, input, ...) {

    index <- grep(names(x), names(inputTable))
    
    if (length(index) == 0) 
        stop(paste("Could not find column", names(x), "in Input table.", sep=" "))
             
    compare <- input[, index]

    if (params[1]) {
        output <- abs(x) <= abs(compare) + abs(compare) * params[2] / 100 &
            abs(x) >= abs(compare) - abs(compare) * params[2] / 100
    } else {
        output <- x <= compare + params[2] &
            x >= compare - params[2]
    }
    
    ifelse(all(output), Output(x, TRUE, output, func, params, compare), Output(x, FALSE, output, func, params, compare))
}

############# EqualTo ############
# Return true if each value in x is equal to
# corresponding value in baseData
# @Param x: multi  - list
# @Param func:string - name of the calling test
# @Param params - unused
# @baseData: vector - reference data
##################################
EqualTo <- function (x, func, params, baseData, ...) {
   output <- x == params[1]
   ifelse(all(output), Output(x, TRUE, output, func, params), Output(x, FALSE, output, func, params))
  }