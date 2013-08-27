#rm(list=ls()) #remove for production
options(java.parameters = "-Xmx1024m")
library(XLConnect)
library(RSQLite)
source("c:\\apsimx\\apsimx\\RTestSuite\\tests.R")

args <- commandArgs(TRUE)
#args <- c("C:\\ApsimX\\ApsimX\\Tests", "C:\\ApsimX\\ApsimX\\Tests")
setwd(args[1])

# read control file
wb <- loadWorkbook("Sensibility.xlsx", FALSE)
sheets <- getSheets(wb)

#run tests on each sheet
for (ind in c(1:length(sheets))){
  # get list of sims to test in this suite TODO handle 'all'
  master <- readWorksheet(wb, sheet = ind, header = FALSE)
  simsToTest <- master[master[1] == "sim", -1]
  simsToTest <- simsToTest[sapply(simsToTest, function(x) !any(is.na(x)))] #remove NA columns
  dbName <- list.files(args[1], pattern="^.*\\.(db)$")
  
  for (sim in c(1:ncol(simsToTest)))
  {
    print(as.character(simsToTest[sim]))
    db <- dbConnect(SQLite(), dbname = dbName)
    
    #get report ID and extract relevant info from table
    simID <- dbGetQuery(db, paste("SELECT ID FROM Simulations WHERE Name='", simsToTest[sim], "'", sep=""))
    readSimOutput <- dbReadTable(db, "Report")
    readSimOutput <- readSimOutput[readSimOutput$SimulationID == simID,]
    
    # redirect result to temp var so we dont have it appearing in output
    junk <- dbDisconnect(db)
    rm(junk)
    
    #TODO: drop Date column if it exists; either by pattern match or db field type (might not be called Date - field type better (may not use / either)
    readSimOutput <- subset(readSimOutput, select=-Clock.Today) #temporary
    
    #get tests to run
    tests <- master[master[1] == "test",]
    
    for (i in c(1:nrow(tests))) {
      
      #get columns to run them on
      if (tests[i, "Col6"] != "all"){
         cols <- tests[i,c(grep("Col6",colnames(tests)):ncol(tests))]
         cols <- cols[sapply(cols, function(x) !any(is.na(x)))] #remove NA columns
         print(unlist(cols))
         if (ncol(readSimOutput) > 1) 
           simOutput <- subset(readSimOutput, select=unlist(cols))
      }
      else 
        simOutput <- readSimOutput
            
      # retrieve the test name and run it
      # test must accept a data frame
      func <- match.fun(tests$Col2[i])
      ifelse(i == 1,
             #testing
       #  results <- paste(tests$Col2[i], func(simOutput, tests$Col3[i], tests$Col4[i], tests$Col5[i])),
       #   results <- c(results, paste(tests$Col2[i], func(simOutput, tests$Col3[i], tests$Col4[i], tests$Col5[i]))))
      
      results <- func(simOutput, tests$Col3[i], tests$Col4[i], tests$Col5[i]),
      results <- c(results, func(simOutput, tests$Col3[i], tests$Col4[i], tests$Col5[i])))
    }
  }
}
#print(results)
print(all(results))

if (all(results) == FALSE) stop("One or more tests failed.")

