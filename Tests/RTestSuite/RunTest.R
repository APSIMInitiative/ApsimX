#rm(list=ls()) # for testing only 
setwd(".\\")
options(warn = -1)
#setwd("c:\\ApsimX") # for testing only

library("XML")
library("RSQLite")
library("RODBC")

args <- commandArgs(TRUE)
#args <- c("Windows_NT", 500) # for testing only 

if(length(args) == 0)
  stop("Usage: rscript RunTest.R <path to .apsimx>")

source("Tests/RTestSuite/tests.R")

files <- list.files(path="Tests", pattern="apsimx$", full.names=TRUE, recursive=TRUE, ignore.case=TRUE)

for (fileNumber in 1:length(files)){
  print(files[fileNumber])
  dbConnect <- unlist(read.table("\\ApsimXdbConnect.txt", sep="|", stringsAsFactors=FALSE))
  connection <- odbcConnect("RDSN", uid=dbConnect[1], pwd=dbConnect[2]) #any computer running this needs an ODBC set up (Windows: admin tools > data sources)
  

  #create a blank data frame to hold all test output
  buildRecord <- data.frame(BuildID=integer(), System=character(),Date=character(), Time=character(), Simulation=character(), ColumnName=character(), Test=character(), 
                            BaseValue=double(), RunValue=double(), Passed=logical(), Paramaters=double())
  
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
      dbBase <- dbConnect(SQLite(), dbname = paste(dbName, ".baseline", sep=""))
      
      #get report ID and extract relevant info from table
      simID <- dbGetQuery(db, paste("SELECT ID FROM Simulations WHERE Name='", simsToTest[sim], "'", sep=""))
      readSimOutput <- dbReadTable(db, "Report")
      readSimOutput <- readSimOutput[readSimOutput$SimulationID == simID,]
                          
      #do the same thing for baseline data
      readSimOutputBase <- dbReadTable(dbBase, "Report")
      readSimOutputBase <- readSimOutputBase[readSimOutputBase$SimulationID == simID,]                        
      
      # redirect result to temp var so we dont have it appearing in output
      junk <- dbDisconnect(db)
      junk <- dbDisconnect(dbBase)
      rm(junk)
      
      # drop Date column if it exists
      readSimOutput     <- readSimOutput[,     -grep("[0-9]{4}-[0-9]{2}-[0-9]{2}", readSimOutput)]
      readSimOutputBase <- readSimOutputBase[, -grep("[0-9]{4}-[0-9]{2}-[0-9]{2}", readSimOutputBase)]
                          
      #get tests to run
      tests <- unlist(strsplit(currentSimGroup[3, 1], ","))
      
      #prepare to run the tests
      for (i in c(1:length(tests))) {    
        #get columns to run them on
           cols <- unlist(strsplit(currentSimGroup[4, 1], ","))
           simOutput    <- subset(readSimOutput,      select=unlist(cols))
           simOutputBase <- subset(readSimOutputBase, select=unlist(cols))
              
        # retrieve the test name
        func <- match.fun(tests[i])
        #unpack parameters - TODO: catch error when invalid
        params <- as.numeric(unlist(strsplit(currentSimGroup[5, 1], ",")))
        #run each test
        ifelse(i == 1,      
          results <- func(simOutput, tests, params, simOutputBase),
          results <- c(results, func(simOutput, tests, params, simOutputBase)))
        print(paste(tests, results[i], cols, sep=" "))
      }
    }
  }
  print(results)
  
  sqlSave(connection, buildRecord, tablename="BuildOutput", append=TRUE, rownames=FALSE, colnames=FALSE, safer=TRUE, addPK=FALSE)
  odbcCloseAll()
}

if (all(results) == FALSE) stop("One or more tests failed.")