net <- read.table("C:\\Users\\fai04d\\OneDrive\\SWIM Conversion 2015\\log.txt", header=FALSE, fill = TRUE, stringsAsFactors = FALSE)
fort <- read.table("C:\\Users\\fai04d\\OneDrive\\SWIM Conversion 2015\\test.out", header=FALSE, skip = 72, fill = TRUE, stringsAsFactors = FALSE)
v <- "ii"
m <- "fd"
ncols <-3
nrows <- 10
head(net[net$V2==v & net$V1==m,1:ncols],n=nrows)
head(fort[fort$V2==v & fort$V1==m,1:ncols],n=nrows)

head(net[net$V1=="fd",1:ncols],n=24)
head(fort[fort$V1=="fd",1:ncols],n=24)
