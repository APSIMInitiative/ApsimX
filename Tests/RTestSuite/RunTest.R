#rm(list=ls()) # for testing only 
setwd(".\\")
options(warn = -1)
#setwd("c:\\ApsimX") # for testing only
haveTestsPassed <- TRUE;
library("XML")
library("RSQLite")
library("RODBC")

args <- commandArgs(TRUE)
#args <- c("Windows_NT", -1) # for testing only 

if(length(args) == 0)
  stop("Usage: rscript RunTest.R <path to .apsimx>")

source("Tests/RTestSuite/tests.R")

# if 1 argument, assume it is a single test to run
ifelse(length(args) == 1, files <- args[1], files <- list.files(path="Tests", pattern="apsimx$", full.names=TRUE, recursive=TRUE, ignore.case=TRUE))

# create a blank data frame to hold all test output
buildRecord <- data.frame(BuildID=integer(), System=character(),Date=character(), Time=character(), Simulation=character(), ColumnName=character(), Test=character(), 
                          BaseValue=double(), RunValue=double(), Passed=logical(), Paramaters=double())

results <- -1

for (fileNumber in 1:length(files)){
  print(noquote(files[fileNumber]))
  if(length(args) > 1){
    dbConnect <- unlist(read.table("\\ApsimXdbConnect.txt", sep="|", stringsAsFactors=FALSE))
    connection <- odbcConnect("RDSN", uid=dbConnect[1], pwd=dbConnect[2]) #any computer running this needs an ODBC set up (Windows: admin tools > data sources)
  } 

  time <- proc.time()
  # read tests from .apsimx
  doc <- xmlTreeParse(files[fileNumber], useInternalNodes=TRUE)
  group <- getNodeSet(doc, "/Simulations/Tests/Test")
  
  groupdf <- list()
  c <- 1
  for (n in group){
    groupdf[[c]] <- xmlToDataFrame(n, stringsAsFactors=FALSE)
    c <- c+1
  }
  rm(c)
  
  #run tests on each test group
  for (ind in c(1:length(groupdf))){
    currentSimGroup <- groupdf[[ind]]
    dbName <- paste(files[fileNumber], ".db",sep="")
    dbName <- gsub(".apsimx", "", dbName)
    simsToTest <- unlist(strsplit(currentSimGroup[1, 1], ","))
    
    for (sim in c(1:length(simsToTest)))
    {
      #connect to simulator output and baseline data if available
      db <- dbConnect(SQLite(), dbname = dbName)
      if(file.exists(paste(dbName, ".baseline", sep="")))
          dbBase <- dbConnect(SQLite(), dbname = paste(dbName, ".baseline", sep=""))
      
      #get report ID and extract relevant info from table
      simID <- dbGetQuery(db, paste("SELECT ID FROM Simulations WHERE Name='", simsToTest[sim], "'", sep=""))
      possibleError <- tryCatch({
          readSimOutput <- dbReadTable(db, "Report")
          readSimOutput <- readSimOutput[readSimOutput$SimulationID == as.numeric(simID),]
          
          if(nrow(readSimOutput) == 0)
            stop(paste("Error: No data returned for simulation: ", simsToTest[sim]))
          
          #do the same thing for baseline data
          if(file.exists(paste(dbName, ".baseline", sep=""))){
            readSimOutputBase <- dbReadTable(dbBase, "Report")
            readSimOutputBase <- readSimOutputBase[readSimOutputBase$SimulationID == as.numeric(simID),]
          }
          else
            readSimOutputBase <- NA
          
          
          # redirect result to temp var so we dont have it appearing in output
          junk <- dbDisconnect(db)
          if (!is.na(readSimOutputBase))
            junk <- dbDisconnect(dbBase)
          rm(junk)
          
          # drop Date column if it exists
          readSimOutput     <- readSimOutput[,     -grep("[0-9]{4}-[0-9]{2}-[0-9]{2}", readSimOutput)]
          if (!is.na(readSimOutputBase))
              readSimOutputBase <- readSimOutputBase[, -grep("[0-9]{4}-[0-9]{2}-[0-9]{2}", readSimOutputBase)]
                              
          #get tests to run
          tests <- unlist(strsplit(currentSimGroup[3, 1], ","))
          
          #prepare to run the tests
          for (i in c(1:length(tests))) {    
            #get columns to run them on
               cols <- unlist(strsplit(currentSimGroup[4, 1], ","))
               cols <- gsub("[()]", "\\.", cols) # replace ( and ) with . to handle R's renaming of these characters
               
               tryCatch({
                   simOutput    <- subset(readSimOutput, select=unlist(cols))
               }, error = function(err) {
                   print(noquote(paste("A column in the set: [", cols, "] could not be found in the database set: [", paste(names(readSimOutput), collapse=", "), "]", sep="")))
               })
               
               if(!is.na(readSimOutputBase))
                 tryCatch({
                   simOutputBase <- subset(readSimOutputBase, select=unlist(cols))
                   }, error = function(err) {
                     print(noquote(paste("A column in the set: [", cols, "] could not be found in the database set: [", paste(names(readSimOutputBase), collapse=", "),
                                 "]. Do you need to update the baseline?", sep="")))
                   })
                  
            # retrieve the test name
            func <- match.fun(tests[i])
            #unpack parameters - TODO: catch error when invalid
            params <- as.numeric(unlist(strsplit(currentSimGroup[5, 1], ",")))

            #run each test
            ifelse(results == -1,      
              results <- func(simOutput, tests, params, simOutputBase),
              results <- c(results, func(simOutput, tests, params, simOutputBase)))
            print(noquote(paste(tests, tail(results,1), cols, sep=" ")))
           }
      }, error = function(err) {
        #print(paste("Could not find Report table for ", dbName, sep=""))
        print(noquote(err))
        haveTestsPassed <<- FALSE
      }) 
      if(inherits(possibleError, "error")) next
      }
  }
  if(exists("results"))
    print(noquote(paste((proc.time() - time)[3], "seconds", sep=" ")))
 
  if (length(args) > 1){
      # this line does the save to the external database. comment out to stop this happening for testing
      #    sqlSave(connection, buildRecord, tablename="BuildOutput", append=TRUE, rownames=FALSE, colnames=FALSE, safer=TRUE, addPK=FALSE)
  }
  odbcCloseAll()
}

#write.csv(buildRecord,"c:\\temp\\output.csv") # used for testing
if (all(results) == FALSE | haveTestsPassed == FALSE) stop("One or more tests failed.")