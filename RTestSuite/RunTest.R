#rm(list=ls()) #remove for production
library("XML")
library("RSQLite")

args <- commandArgs(TRUE)
#args <- c("C:\\ApsimX\\ApsimX\\Tests\\", "C:\\ApsimX\\ApsimX\\Tests\\") # for testing only 

# needed to cut the trailing '\' off... worked ok on local, but not remote.
args[1] <- substr(args[1], 1, nchar(args[1]) - 1)
setwd(args[1])
source("../RTestSuite/tests.R")

# read control file - this will come from .apsimx in the future
doc <- xmlTreeParse(list.files(args[1], pattern="^.*\\.(apsimx)$"), useInternalNodes=TRUE)
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
  dbName <- list.files(args[1], pattern="^.*\\.(db)$")
  simsToTest <- unlist(strsplit(currentSimGroup[1, 1], ","))
  
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
    tests <- unlist(strsplit(currentSimGroup[3, 1], ","))
    
    #run the tests
    for (i in c(1:length(tests))) {    
      #get columns to run them on
         cols <- unlist(strsplit(currentSimGroup[4, 1], ","))
           simOutput <- subset(readSimOutput, select=unlist(cols))
            
      # retrieve the test name
      func <- match.fun(tests[i])
      #unpack parameters
      params <- as.numeric(unlist(strsplit(currentSimGroup[5, 1], ",")))
      ifelse(i == 1,      
      results <- func((simOutput), tests$name[i], params),
      results <- c(results, func(simOutput, tests$name[i], params)))
    }
  }
}
print(tests)
print(results)

if (all(results) == FALSE) stop("One or more tests failed.")