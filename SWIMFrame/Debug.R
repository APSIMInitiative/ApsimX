net <- read.table("C:\\Users\\fai04d\\OneDrive\\SWIM Conversion 2015\\log.txt", header=FALSE, fill = TRUE, stringsAsFactors = FALSE)
fort <- read.table("C:\\Users\\fai04d\\OneDrive\\SWIM Conversion 2015\\test.out", header=FALSE, skip = 72, fill = TRUE, stringsAsFactors = FALSE)
v <- "dx"
ncols <- 20
head(net[net$V2==v,1:ncols],n=10)
head(fort[fort$V2==v & fort$V1=="twofluxes",1:ncols],n=10)
