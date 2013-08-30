rm(list=ls()) #remove for production
options(java.parameters = "-Xmx1024m")
library("XML")
library("RSQLite")
source("../RTestSuite/tests.R")

args <- commandArgs(TRUE)
#args <- c("C:\\ApsimX\\ApsimX\\Tests\\", "C:\\ApsimX\\ApsimX\\Tests\\")

# needed to cut the trailing '\' off... worked ok on local, but not remote.
args[1] <- substr(args[1], 1, nchar(args[1]) - 1)
setwd(args[1])

# read control file - this will come from .apsimx in the future
doc <- xmlTreeParse("test.xml", useInternalNodes=TRUE)
group <- getNodeSet(doc, "/tests/sims")

groupdf <- list()
c=1
for (n in group){
  groupdf[[c]] <- xmlToDataFrame(n)
  c <- c+1
}
rm(c)

#run tests on each simulation group
for (ind in c(1:length(groupdf))){
  currentSimGroup <- groupdf[[ind]]
  dbName <- list.files(args[1], pattern="^.*\\.(db)$")
  simsToTest <- unlist(strsplit(currentSimGroup[1,1], ","))
  
  for (sim in c(1:length(simsToTest)))
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
    tests <- currentSimGroup[!is.na(currentSimGroup[2]), -1]
    
    #run the tests
    for (i in c(1:nrow(tests))) {    
      #get columns to run them on
      if (tests[i, "col"] != "all"){
         cols <- unlist(strsplit(tests$col[i], ","))
         if (ncol(readSimOutput) > 1) 
           simOutput <- subset(readSimOutput, select=unlist(cols))
      }
      else {
        simOutput <- readSimOutput
      }
            
      # retrieve the test name
      func <- match.fun(tests$name[i])
      #unpack parameters
      params <- as.numeric(unlist(strsplit(tests$params[i], ",")))
      ifelse(i == 1,      
      results <- func((simOutput), params),
      results <- c(results, func(simOutput, params)))
    }
  }
#}
print(tests$name)
print(results)

if (all(results) == FALSE) stop("One or more tests failed.")