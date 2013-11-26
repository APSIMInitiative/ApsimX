#rm(list=ls()) # for testing only 
setwd(".\\")
#setwd("c:\\ApsimX\\ApsimX") # for testing only
library("XML")
library("RSQLite")
library("RODBC")

args <- commandArgs(TRUE)
#args <- c("C:\\ApsimX\\ApsimX\\Tests\\Test.apsimx", "Windows_NT", 500) # for testing only 

#if(length(args) == 0)
#  stop("Usage: rscript RunTest.R <path to .apsimx>")

args[1] <- ifelse(is.na(unlist(strsplit(args[1], ".apsimx", fixed = TRUE))), args[1], unlist(strsplit(args[1], ".apsimx", fixed = TRUE)))
dbConnect <- unlist(read.table("\\ApsimXdbConnect.txt", sep="|", stringsAsFactors=FALSE))
connection <- odbcConnect("RDSN", uid=dbConnect[1], pwd=dbConnect[2]) #any computer running this needs an ODBC set up (Windows: admin tools > data sources)

source("Tests/RTestSuite/tests.R")

#create a blank data frame to hold all test output
buildRecord <- data.frame(BuildID=integer(), System=character(),Date=character(), Time=character(), Simulation=character(), ColumnName=character(), Test=character(), 
                          BaseValue=double(), RunValue=double(), Passed=logical(), Paramaters=double())

# read tests from .apsimx
# this will need to change to search the file instead of using an absolute path
doc <- xmlTreeParse(paste(args[1], ".apsimx",sep=""), useInternalNodes=TRUE)
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
  dbName <- paste(args[1], ".db",sep="")
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
#disabled for infrastructure testing
#sqlSave(connection, buildRecord, tablename="BuildOutput", append=TRUE, rownames=FALSE, colnames=FALSE, safer=TRUE, addPK=FALSE)
odbcCloseAll()

if (all(results) == FALSE) stop("One or more tests failed.")