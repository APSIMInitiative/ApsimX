library(rzmq)
library(msgpackR)
library(processx)

apsimDir <- "/home/uqpdevo1/src/ApsimX-ZMQ"
#apsimDir <- "/usr/local/lib/apsim/"

# Interactive protocol
testProto2 <- function() {
  # Set up a listening server on a random port
  apsim <- list()
  apsim$apsimSocket <- open_zmq2(port = 0)
  
  apsim$randomPort <- unlist(strsplit(get.last.endpoint(apsim$apsimSocket),":"))[3]
  cat("Listening on", get.last.endpoint(apsim$apsimSocket), "\n")
  
  apsim$process <- process$new("/usr/bin/dotnet", args=c(
    paste0(apsimDir, "/bin/Debug/net6.0/ApsimZMQServer.dll"), 
    "-p", apsim$randomPort, 
    "-P", "interactive",
    "-f", paste0(apsimDir, "/Tests/Simulation/ZMQ-Sync/ZMQ-sync.apsimx")),
    stdout="", stderr="")
  
  if (! apsim$process$is_alive() ) { stop("Didnt start apsim?") }
  
  cat("Started Apsim process id ", apsim$process$get_pid(), "\n")
  return(apsim)
}

# Set up a listening (response) socket that the simulation will connect to
open_zmq2 <- function(port) {
  zcontext <<- init.context()  # This needs to be kept around (global environment)..
  out.socket <- init.socket(zcontext, "ZMQ_REP")
  bind.socket(out.socket, paste0("tcp://0.0.0.0:", port))
  return(out.socket)
}

send_multipart <- function(socket, listOfStrings) {
  for (i in 1:length(listOfStrings)) {
    send.socket(socket, charToRaw(listOfStrings[[i]]), 
                serialize = F, send.more = (i != length(listOfStrings)))
  }
}

# The response loop. When the simulation connects we tell it what to do.
# connect -> ok
# paused -> resume/get/set
# finished -> ok
poll_zmq2 <- function(socket) {
  while (TRUE) {
    msg <- receive.string(socket)
    # cat(msg, "\n")
    if (msg == "connect") {
      send.raw.string(socket, "ok",send.more=FALSE)
    } else if (msg == "paused") {
      
      send_multipart(socket, list("get", "[Clock].Today.Day"))
      msg <- receive.socket(socket, unserialize = F)
      msg <- msgpackR::unpack(msg)
      #cat("Day = ", msg, "\n")
      
      send_multipart(socket, list("set", "[Manager].Script.DummyStringVar", "Blork"))
      msg <- receive.socket(socket, unserialize = F)
      msg <- msgpackR::unpack(msg)
      #cat("result = ", msg, "\n")
      
      #msg <- receive.string(socket)
      #cat(msg, "\n")s
      send.raw.string(socket, "resume",send.more=FALSE)
    } else if (msg == "finished") {
      send.raw.string(socket, "ok",send.more=FALSE)
      break
    } 
  }
}

close_zmq2 <- function(socket) {
  disconnect.socket(socket, get.last.endpoint(socket))
}

apsim <- testProto2()
rec <- data.frame(iter = 1:100, mem=NA)
for (i in 1:nrow(rec)) {
  poll_zmq2(apsim$apsimSocket)
  rec$mem[i] <- apsim$process$get_memory_info()[["rss"]]
}

close_zmq2(apsim$apsimSocket)

# kill off apsim server
apsim$process$kill()

print(rec)
