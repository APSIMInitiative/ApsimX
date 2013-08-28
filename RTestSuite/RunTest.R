#rm(list=ls()) #remove for production
options(java.parameters = "-Xmx1024m")
library(XLConnect)
library(RSQLite)
source("../RTestSuite/tests.R")

args <- commandArgs(TRUE)
#args <- c("C:\\ApsimX\\ApsimX\\Tests\\", "C:\\ApsimX\\ApsimX\\Tests\\")

# needed to cut the trailing '\' off... worked ok on local, but not remote.
args[1] <- substr(args[1], 1, nchar(args[1]) - 1)
setwd(args[1])

# read control file
wb <- loadWorkbook("sensibility.xlsx", FALSE)
sheets <- getSheets(wb)

#run tests on each sheet
for (ind in c(1:length(sheets))){
  dbName <- list.files(args[1], pattern="^.*\\.(db)$")
  # get list of sims to test in this suite TODO: handle 'all' - do we want to do this in a db?
  master <- readWorksheet(wb, sheet = ind, header = FALSE)
  simsToTest <- master[master[1] == "sim", -1]
  simsToTest <- simsToTest[sapply(simsToTest, function(x) !any(is.na(x)))] #remove NA columns  
  
  for (sim in c(1:ncol(simsToTest)))
  {
    db <- dbConnect(SQLite(), dbname = dbName)
    
    #get report ID and extract relevant info from table
    simID <- dbGetQuery(db, paste("SELECT ID FROM Simulations WHERE Name='", simsToTest[sim], "'", sep=""))
    readSimOutput <- dbReadTable(db, "Report")
    readSimOutput <- readSimOutput[readSimOutput$SimulationID == simID,]
    
    # redirect result to temp var so we dont have it appearing in output
    junk <- dbDisconnect(db)
    rm(junk)
    
    # drop Date column if it exists
    readSimOutput <- readSimOutput[, -grep("[0-9]{4}-[0-9]{2}-[0-9]{2}", readSimOutput)]
    
    #get tests to run
    tests <- master[master[1] == "test",]
    
    for (i in c(1:nrow(tests))) {    
      #get columns to run them on
      if (tests[i, "Col6"] != "all"){
         cols <- tests[i,c(grep("Col6",colnames(tests)):ncol(tests))]
         cols <- cols[sapply(cols, function(x) !any(is.na(x)))] #remove NA columns
         if (ncol(readSimOutput) > 1) 
           simOutput <- subset(readSimOutput, select=unlist(cols))
      }
      else 
        simOutput <- readSimOutput
            
      # retrieve the test name and run it
      # test must accept a data frame
      func <- match.fun(tests$Col2[i])
      ifelse(i == 1,      
      results <- func(simOutput, tests$Col3[i], tests$Col4[i], tests$Col5[i]),
      results <- c(results, func(simOutput, tests$Col3[i], tests$Col4[i], tests$Col5[i])))
    }
  }
}
print(results)
#print(all(results))

if (all(results) == FALSE) stop("One or more tests failed.")