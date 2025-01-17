rm(list=ls())

setwd("~/work/Projects/GRDC Pulse 2019/Lentils/Third Cut/")

library(readxl)
library(xlsx)
library(xml2)
library(dplyr)

library(jsonStrings)
options("jsonStrings.prettyPrint" = TRUE)
source("jsonV8.R")

# this writes out simulations to "Lentil.apsimx", 
# and obs data in "Lentil observed.xlsx"

# Read the supplied data and construct an apsim file with simulations from 3 sources:
# xlsx file is from liz meier
# csv data is from DR
# more canopy data from fernanda

getTOS <- function(x) {
  return(paste0("TOS", substr(strsplit(x, "TOS")[[1]][2], 1,1)))
}

getIRR <- function(x) {
  y <- strsplit(x, "Irr")[[1]][2]
  return(ifelse(!is.na(y), paste0("Irr", substr(y, 1,1)), ""))
}

getPop <- function(x) {
  y <- strsplit(x, "Pop")[[1]][2]
  return(ifelse(!is.na(y), y, ""))
}

getSite <- function(x) {
  y <- strsplit(x, "TOS")[[1]][1]
  y <- ifelse(!is.na(y), y, "")
  y <- gsub('[[:digit:]]+', '', y)
  y <- gsub('GattonPhen', 'Gatton', y, fixed = T)
  return(y)  
}

getCV <- function(x) {
  y <- strsplit(x, "Cv")[[1]][2]
  return(ifelse(!is.na(y), y, ""))
}


# Data from Liz Meier
# Harvest data in first sheet. Just want the design for now
harv <- read_xlsx("./Observed for daniel.xlsx") %>%
  mutate(SimulationName = gsub("Dry", "Irr0", SimulationName, fixed = T),
         SimulationName = gsub("Wet", "Irr1", SimulationName, fixed = T),
         SimulationName = gsub("DR_2001Dooen", "Dooen01TOS1CvDigger", SimulationName, fixed = T),
         Season = ifelse(grepl("Dooen", SimulationName), "01", "19"))

harv$site <- sapply(harv$SimulationName, getSite)
harv$TOS <- sapply(harv$SimulationName, getTOS)
harv$IRR <- sapply(harv$SimulationName, getIRR) 
harv$Pop <- sapply(harv$SimulationName, getPop)

CV <- sapply(harv$SimulationName, getCV)
missingCV <- which(is.na(harv$Lentil.SowingData.Cultivar))
harv$Lentil.SowingData.Cultivar[missingCV] <- CV [missingCV]

harv$Clock.Today[!grepl('^[0-9]+$', harv$Clock.Today)]  <- NA
harv$Clock.Today <- as.Date(as.numeric(harv$Clock.Today),  origin="1900-01-01" )

# 2001 experiments from DR
exps2 <- rbind(
  expand.grid(site=c("Horsham"), 
              Lentil.SowingData.Cultivar= c("Digger", "Northfield", "Nugget"), 
              TOS=c("TOS1","TOS2","TOS3","TOS4"),
              Pop = "", IRR = "", Irrigation="Dryland", Season="01"),
  expand.grid(site=c("Beulah"), 
              Lentil.SowingData.Cultivar= c("Digger", "Northfield", "Nugget"), 
              TOS=c("TOS1","TOS2","TOS3"),
              Pop = "", IRR = "", Irrigation="Dryland", Season="01"),
  expand.grid(site=c("Birchip"), 
              Lentil.SowingData.Cultivar= c("Digger", "Northfield", "Nugget"), 
              TOS=c("TOS1","TOS2","TOS3"),
              Pop = "", IRR = "", Irrigation="Irrigated", Season="01")) %>%
  mutate(SimulationName = paste0(site, Season, TOS, "Cv", Lentil.SowingData.Cultivar, Irrigation)) %>%
  merge(read.csv("vic2001.csv"), by=c("site", "Lentil.SowingData.Cultivar", "TOS"))

harv <- harv %>% bind_rows(exps2)

# Now convert it to the apsim simulation name that reflects factor levels
harv$SimulationName <- with(harv, paste0(site, "TOS", TOS, "Cultivar", Lentil.SowingData.Cultivar, 
                                          ifelse (IRR != "", paste0("Irr", Irrigation), ""),
                                          ifelse (Pop != "", paste0("Pop", Pop), "")))

# Defaults 
defaults<- list()
defaults[["Soil"]] <- "Black Vertosol-Mywybilla (Bongeen No001)"
defaults[["sowing_density"]] <- "120" 
#defaults[["Operations"]] <- ""

defaults[["Emerald,TOS1"]] <- as.Date("10-May-2019", format="%d-%b-%Y")
defaults[["Emerald,TOS2"]] <- as.Date("3-jun-2019", format="%d-%b-%Y")
defaults[["Emerald,TOS3"]] <- as.Date("27-jun-2019", format="%d-%b-%Y")
defaults[["Emerald,row_spacing"]] <- 500 
defaults[["Emerald,sowing_density"]] <- 120
defaults[["Emerald,Soil"]] <- "Vertosol No7 (PAWC-204 No519-Generic)"
defaults[["Emerald,isw"]] <- "0.326, 0.328, 0.345, 0.360, 0.359, 0.326, 0.326"
defaults[["Emerald,isn"]] <- 35   #kg/ha tot
defaults[["Emerald,metfile"]] <- "Emerald2019Combo.met"
defaults[["Emerald,Dryland"]] <- list()

defaults[["Dooen,TOS1"]] <- as.Date("28-Jun-2001", format="%d-%b-%Y")
defaults[["Dooen,row_spacing"]] <- 230 
defaults[["Dooen,sowing_density"]] <- 230 
defaults[["Dooen,Soil"]] <- "dooen"
defaults[["Dooen,isw"]] <- "0.310, 0.240, 0.210, 0.220, 0.245, 0.240, 0.257"
defaults[["Dooen,isn"]] <- 35
defaults[["Dooen,metfile"]] <- "horsham.met"
defaults[["Dooen,Dryland"]] <- list()
defaults[["Dooen,Irrigated"]] <- list("10-aug" = 45, "22-sep" = 45)

defaults[["Mildura,TOS1"]] <- as.Date("1-May-2019", format="%d-%b-%Y")
defaults[["Mildura,TOS2"]] <- as.Date("18-May-2019", format="%d-%b-%Y")
defaults[["Mildura,TOS3"]] <- as.Date("7-June-2019", format="%d-%b-%Y")
defaults[["Mildura,row_spacing"]] <- 250 
defaults[["Mildura,sowing_density"]] <- 110 
defaults[["Mildura,Soil"]] <- "Sandy Loam (Kerribee No359)"
defaults[["Mildura,isw"]] <- "0.105, 0.149, 0.225, 0.257, 0.255, 0.255"
defaults[["Mildura,isn"]] <- 35
defaults[["Mildura,metfile"]] <- "MildurahPO.met"
defaults[["Mildura,Dryland"]] <- list("10-aug"=45, "22-sep"=45)

defaults[["Caragabal,TOS1"]] <- as.Date('8-May-2019', format="%d-%b-%Y")
defaults[["Caragabal,TOS2"]] <- as.Date('17-Jun-2019', format="%d-%b-%Y")
defaults[["Caragabal,row_spacing"]] <- 230 
defaults[["Caragabal,sowing_density"]] <- 110 
defaults[["Caragabal,Soil"]] <- "Vertosol No7 (PAWC-204 No519-Generic)"
defaults[["Caragabal,isw"]] <- "0.234, 0.291, 0.279, 0.290, 0.322, 0.335, 0.327"
defaults[["Caragabal,isn"]] <- 35
defaults[["Caragabal,metfile"]] <- "CaragabalPO.met"
defaults[["Caragabal,Dryland"]] <- list()

defaults[["Greenethorpe,TOS1"]] <- as.Date("30-apr-2019", format="%d-%b-%Y")
defaults[["Greenethorpe,TOS2"]] <- as.Date("21-may-2019", format="%d-%b-%Y")
defaults[["Greenethorpe,TOS3"]] <- as.Date("12-jun-2019", format="%d-%b-%Y")
defaults[["Greenethorpe,row_spacing"]] <- 230 
defaults[["Greenethorpe,sowing_density"]] <- 120 
defaults[["Greenethorpe,Soil"]] <- "Greenethorpe_soil_2023"

# These are LL15; plot level in .csv file occurs at runtime
iswG <- read.csv("ISWGreenethorpe2019.csv")
defaults[["Greenethorpe,isw,Dryland,TOS1"]] <- paste(iswG$Vol_mm.mm[iswG$TOS=="TOS1" & iswG$WaterTrt == "Dryland"], collapse=",")
defaults[["Greenethorpe,isw,Dryland,TOS2"]] <- paste(iswG$Vol_mm.mm[iswG$TOS=="TOS2" & iswG$WaterTrt == "Dryland"], collapse=",")
defaults[["Greenethorpe,isw,Dryland,TOS3"]] <- paste(iswG$Vol_mm.mm[iswG$TOS=="TOS3" & iswG$WaterTrt == "Dryland"], collapse=",")
defaults[["Greenethorpe,isw,Irrigated,TOS1"]] <- paste(iswG$Vol_mm.mm[iswG$TOS=="TOS1" & iswG$WaterTrt == "Irrigated"], collapse=",")
defaults[["Greenethorpe,isw,Irrigated,TOS2"]] <- paste(iswG$Vol_mm.mm[iswG$TOS=="TOS2" & iswG$WaterTrt == "Irrigated"], collapse=",")
defaults[["Greenethorpe,isw,Irrigated,TOS3"]] <- paste(iswG$Vol_mm.mm[iswG$TOS=="TOS3" & iswG$WaterTrt == "Irrigated"], collapse=",")

defaults[["Greenethorpe,isn"]] <- 70
defaults[["Greenethorpe,metfile"]] <- "GreenethorpeSILO.met"

# factor levels here
defaults[["Greenethorpe,Dryland"]] <- list()
defaults[["Greenethorpe,Irrigated,TOS2"]] <- list(
  "21-may" = 15, # for TOS 2 sowing
  "13-aug" = 10, "14-aug" = 10, "15-aug" = 10, "26-aug" = 10,
  "31-aug" = 10, "03-sep" = 10, "25-sep" = 10, "30-sep" = 10, "1-oct" = 10)

defaults[["Greenethorpe,Irrigated"]] <- list(
  "13-aug" = 10, "14-aug" = 10, "15-aug" = 10, "26-aug" = 10,
  "31-aug" = 10, "03-sep" = 10, "25-sep" = 10, "30-sep" = 10, "1-oct" = 10)

#
#Greenethorpe phenology trial:
#•all treatments are irrigated - "JW: For greenethorpe simulate unlimited. We tried to keep it unlimited but there would have been some stress no measure of water just applied when ever we were out there." 
#•they use different TOS to the other Greenethorpe trial so will be simulated separately 
#•there are only 2 TOS for the phenology trial at Greenethorpe 
#•only treatments without lights used 
#•phenology measurements up until the time of flowering only are recorded 
defaults[["GreenethorpePhen,TOS1"]] <-as.Date("18-apr-2019", format="%d-%b-%Y")
defaults[["GreenethorpePhen,TOS2"]] <- as.Date("21-may-2019", format="%d-%b-%Y")
defaults[["GreenethorpePhen,row_spacing"]] <- 230 
defaults[["GreenethorpePhen,sowing_density"]] <- 120 
defaults[["GreenethorpePhen,Soil"]] <- "Greenethorpe_soil_2019"
#defaults[["GreenethorpePhen,isw"]] <- c(0.181, 0.204, 0.567, 0.319, 0.310, 0.283, 0.269, 0.269, 0.241)
defaults[["GreenethorpePhen,isw"]] <- "0.25, 0.21, 0.232, 0.287, 0.311, 0.35, 0.35, 0.35, 0.335, 0.335, 0.335"
defaults[["GreenethorpePhen,isn"]] <- 70
defaults[["GreenethorpePhen,metfile"]] <- "GreenethorpeSILO.met"
defaults[["GreenethorpePhen,Irrigated"]] <- "Auto" 

defaults[["Gatton,TOS1"]] <- as.Date("10-May-2019", format="%d-%b-%Y")
defaults[["Gatton,TOS2"]] <- as.Date("7-Jun-2019", format="%d-%b-%Y")
defaults[["Gatton,TOS3"]] <- as.Date("19-Jul-2019" , format="%d-%b-%Y")
defaults[["Gatton,row_spacing"]] <- 230 
defaults[["Gatton,sowing_density"]] <- 120 
defaults[["Gatton,Soil"]] <- "Vertosol No2 (PAWC-269 No514-Generic)"
defaults[["Gatton,isw"]] <- "0.519, 0.508, 0.501, 0.415, 0.295, 0.295, 0.295" # 50% filled from top
defaults[["Gatton,isn"]] <- 70
defaults[["Gatton,metfile"]] <- "Gatton.met"
defaults[["Gatton,patchmetfile"]] <- "GilbertWS.met"
defaults[["Gatton,Irrigated"]] <- list("16-apr"=10, "23-apr" = 9, "26-apr" = 10,
                                        "10-may"= 4, "7-jun" = 4, "20-jul" = 18,
                                        "31-jul"=11, "14-aug"=18, "27-aug"=15, 
                                        "13-sep"=18)

defaults[["Walgett,TOS1"]] <- as.Date("21-May-2019", format="%d-%b-%Y")
defaults[["Walgett,TOS2"]] <- as.Date("9-Jun-2019", format="%d-%b-%Y")
defaults[["Walgett,TOS3"]] <- as.Date("28-Jun-2019", format="%d-%b-%Y")
defaults[["Walgett,row_spacing"]] <- 375 
defaults[["Walgett,Soil"]] <- "Vertosol No11 (PAWC-136 No523-Generic)"
defaults[["Walgett,isw"]] <- "0.199, 0.282, 0.304, 0.303, 0.297, 0.297, 0.297"
defaults[["Walgett,isn"]] <- 35
defaults[["Walgett,metfile"]] <- "Walgett.met"
defaults[["Walgett,Dryland"]] <- list()

defaults[["Millmerran,TOS1"]] <- as.Date("15-Jun-2019", format="%d-%b-%Y")
defaults[["Millmerran,TOS2"]] <- as.Date("2-Jul-2019", format="%d-%b-%Y")
defaults[["Millmerran,TOS3"]] <- as.Date("15-Jul-2019", format="%d-%b-%Y")
defaults[["Millmerran,row_spacing"]] <- 300 
defaults[["Millmerran,sowing_density"]] <- 52 
defaults[["Millmerran,Soil"]] <- "Vertosol No11 (PAWC-136 No523-Generic)"
defaults[["Millmerran,isw"]] <- "0.199, 0.282, 0.304, 0.303, 0.297, 0.297, 0.297"
defaults[["Millmerran,isn"]] <- 35   #kg/ha tot
defaults[["Millmerran,metfile"]] <- "Millmerran.met"
defaults[["Millmerran,Irrigated"]] <- list() # fixme - no records?

# 2001 experiments from DR
defaults[["Horsham,TOS1"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 122
defaults[["Horsham,TOS2"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 153
defaults[["Horsham,TOS3"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 164
defaults[["Horsham,TOS4"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 185
defaults[["Horsham,row_spacing"]] <- 250 
defaults[["Horsham,sowing_density"]] <- 152 
defaults[["Horsham,Soil"]] <- "dooen"
defaults[["Horsham,isw"]] <- "0.157, 0.157, 0.207, 0.219, 0.247, 0.248, 0.282" # 5%
defaults[["Horsham,isn"]] <- 60
defaults[["Horsham,metfile"]] <- "horsham.met"
defaults[["Horsham,Dryland"]] <- list()

defaults[["Beulah,TOS1"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 129
defaults[["Beulah,TOS2"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 167
defaults[["Beulah,TOS3"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 192
defaults[["Beulah,row_spacing"]] <- 250 
defaults[["Beulah,sowing_density"]] <- 152 
defaults[["Beulah,Soil"]] <- "Beulah"
defaults[["Beulah,isw"]] <- "0.173, 0.236, 0.237, 0.272, 0.298, 0.333, 0.333" # 10%
defaults[["Beulah,isn"]] <- 130
defaults[["Beulah,metfile"]] <- "beulah.met"
defaults[["Beulah,Dryland"]] <- list()

defaults[["Birchip,TOS1"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 129
defaults[["Birchip,TOS2"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 167
defaults[["Birchip,TOS3"]] <- as.Date("1-Jan-2001", format="%d-%b-%Y") + 192
defaults[["Birchip,row_spacing"]] <- 250 
defaults[["Birchip,sowing_density"]] <- 152 
defaults[["Birchip,Soil"]] <- "FSbirchip"
defaults[["Birchip,isw"]] <- "0.146, 0.253, 0.317, 0.336, 0.380, 0.257, 0.291" # 10%
defaults[["Birchip,isn"]] <- 130
defaults[["Birchip,metfile"]] <- "birchip.met"
defaults[["Birchip,Irrigated"]] <- list() # fixme - no trace of irrigation records for this one?

# get the defaults for a site. 
getDef <- function(site, what) {
  if (is.null(defaults[[paste0(site, ",", what)]])) {
    #cat ("missing", site, what, "\n" )
    if (is.null(defaults[[what]])) {
      return(NULL)
    }
    return(defaults[[what]])
  } 
  return(defaults[[paste0(site, ",", what)]])
}

getLevels <- function(site, like) {
  candidates <- names(defaults)[grepl(paste0(site, ",", like), names(defaults))]
  candidates <- sapply(sapply(strsplit(candidates, ","), "[", -1), paste, collapse=",")
  return(candidates)
}

#for(site in unique(harv$site)){cat (site, getLevels(site, "TOS"), "\n")}
for(site in unique(harv$site)){cat (site, getLevels(site, "Irrigated"), "\n")}

# mash up the template into a single experiment simulation
doit<- function(expt, siteName) {
  thisSimln <- jsonClone(expt)
  thisExpt <- harv[harv$site == siteName,]
  
  setValue(thisSimln, "$.Name", siteName) 
  setValue(thisSimln, "$..[?(@.$type=='Models.Core.Simulation, Models')].Name", siteName) 
  
  soilName <- getDef(siteName, "Soil")
  newSoil <- getValue(soils, paste0("$..[?(@.$type=='Models.Soils.Soil, Models' && @.Name=='", soilName, "')]"), result="js")
  stopifnot(getValue(newSoil, "$.Name") == paste0("\"", soilName, "\"")) # Make sure it's there
  setValue(thisSimln, "$..[?(@.Name=='Soil')]", newSoil)

  # Check the levels of the factorial nodes
  TOSLevels <- unique(thisExpt$TOS)
  for (TOS in setdiff(paste0("TOS", 1:4), TOSLevels)) {
    delete.jsonManip(thisSimln, paste0("$..[?(@.Name=='", TOS, "')]")) # Remove unused
  }
  t0 <- as.Date("2099-01-01")
  t1 <- as.Date("1900-01-01")
  for (TOS in TOSLevels) {
    x <- getDef(siteName,TOS)
    t0 <- min(t0, x)
    t1 <- max(t1, x)
    stopifnot(length(x) == 1)
    s <- getValue(thisSimln, paste0("$..[?(@.Name=='", TOS, "')].Specifications"), result="object")
    s <- gsub("StartDateString", 
              format.Date(x - 1, format="%Y-%m-%dT00:00:00"), 
              s, fixed=T)
    s <- gsub("SowDateString", 
              format.Date(x, format="%Y-%m-%dT00:00:00"), 
              s, fixed=T)
    setValue(thisSimln, paste0("$..[?(@.Name=='", TOS, "')].Specifications"), s)
  }
  #setValue(thisSimln, paste0("$..[?(@.$type=='Models.Clock, Models')].Start"), 
  #         format.Date(t0 - 1,  format="%Y-%m-%dT00:00:00"))
  setValue(thisSimln, paste0("$..[?(@.$type=='Models.Clock, Models')].End"), 
           format.Date(t1 + 270,  format="%Y-%m-%dT00:00:00"))
  
  Varieties <- unique(thisExpt$Lentil.SowingData.Cultivar)
  s <- getValue(thisSimln, "$..[?(@.Name=='Cultivar')].Specification", result="object")
  s <- gsub("ReplacementText", paste(Varieties, collapse=","), s, fixed = T)
  setValue(thisSimln, "$..[?(@.Name=='Cultivar')].Specification", s)

  # This sets factor levels, later there's a (separate) irrigation record
  Waters <- unique(thisExpt$Irrigation)
  if (length(Waters) <= 1) {
    delete.jsonManip(thisSimln, "$..[?(@.Name=='Irr')]")
  } else {
    s <- getValue(thisSimln, "$..[?(@.Name=='Irr')].Specification", result="object")
    s <- gsub("ReplacementText", paste(Waters, collapse=","), s, fixed = T)
    setValue(thisSimln, "$..[?(@.Name=='Irr')].Specification", s)
  }
  
  Popns <- unique(thisExpt$Pop)
  if (length(Popns) <= 1) {
    # Set the manager parameter, remove the factor
    delete.jsonManip(thisSimln, "$..[?(@.Name=='Pop')]")
    setValue(thisSimln, "$..[?(@.Key=='Population')].Value", getDef(siteName, "sowing_density"))
  } else {
    # Set the factor levels
    s <- getValue(thisSimln, "$..[?(@.Name=='Pop')].Specification", result="object")
    s <- gsub("ReplacementText", paste(Popns, collapse=","), s, fixed = T)
    setValue(thisSimln, "$..[?(@.Name=='Pop')].Specification", s)
  }
  
  setValue(thisSimln, "$..[?(@.Key=='SiteName')].Value", siteName)
  setValue(thisSimln, "$..[?(@.Key=='Season')].Value", unique(thisExpt$Season))
  setValue(thisSimln, "$..[?(@.Name=='Weather')].FileName", getDef(siteName, "metfile"))
  if (!is.null(getDef(siteName, "patchmetfile"))) {
    setValue(thisSimln, "$..[?(@.Key=='patchFileName')].Value", getDef(siteName, "patchmetfile"))
  } else {
    delete.jsonManip(thisSimln, paste0("$..[?(@.Name=='PatchWeather')]"))
  }
  
  dlayer1 <- cumsum(unlist(getValue(newSoil, "$..[?(@.$type=='Models.Soils.Physical, Models')].Thickness", result="object")))
  bd <- unlist(getValue(newSoil, "$..[?(@.$type=='Models.Soils.Physical, Models')].BD", result="object"))
  bdFn <- approxfun(x = dlayer1, y = bd)
  
  # NB. guessed distribution
  no3DistFn<-approxfun(x = c(150, 300, 600, 900, 1200),
                   y = c(0.7, 0.1, 0.1, 0.1, 0.0), rule=2)
  
  isn <- getDef(siteName, "isn")
  thickness <- unlist(getValue(newSoil, "$..[?(@.Name=='NO3')].Thickness", result="object"))
  dlayer <- cumsum(thickness)
  no3Init.kgha <- isn *  no3DistFn(dlayer)
  
  # convert kg/ha to ppm
  # bd in g/cc
  # dlayer in mm
  kg2ppm <- function(kg.ha, bd, dlayer) {
    return(kg.ha / 1E-7 / (bd * 1000) / ((dlayer/ 1000) * 10e4))
  }
  no3Init.ppm <- kg2ppm(no3Init.kgha , bdFn(dlayer), thickness)
  setValue(newSoil, paste0("$..[?(@.Name=='NO3')].InitialValues"), no3Init.ppm) 

  setValue(thisSimln, "$..[?(@.Key=='csvFile')].Value", "Lentil.operations.csv")
  return(thisSimln)
}

#csv of id, date, command
doInitialConditions <- function (siteName) {
  res <- NULL
  thisExpt <- harv[harv$site == siteName,]
  for (exptName in unique(thisExpt$SimulationName)) {
    # unique ISW for all or for each sowing eg "isw", "isw,Irrigated,TOS1", ...
    thisSimln <- unique(thisExpt[thisExpt$SimulationName == exptName,])
    candidates<- c(paste0("isw,", unique(thisSimln$Irrigation), ",", unique(thisSimln$TOS)), 
                   paste0("isw,", unique(thisSimln$TOS)),
                   "isw")
    for(cand in candidates) {
      x <- getDef(site, cand)
      if (! is.null(x)) {break}
    }
    if (!is.null(x)) {
      res <- c(res, paste0(exptName, ",init,[Soil].Water.InitialValues = \"", x, "\""))
    } else {
      stop(paste("No isw for", paste(thisSimln, collapse=",")))
    }

    # Look for a factor level, and its record
    irrCode <- unique(thisSimln$Irrigation)
    x <- getDef(siteName, paste0(irrCode, ",", unique(thisSimln$TOS)))
    if (is.null(x)) {
      x <- getDef(siteName, irrCode)
    }
    if (is.null(x)) { stop(paste("No irrig for ",irrCode, " - ", paste(thisSimln, collapse=","))) } 
    #cat(exptName, "=", class(x), "\n")
    if (class(x) == "list") {
      for (date in names(x)) {
        res <- c(res, paste0(exptName, ",", date , ",[Irrigation].Apply(", x[[date]], ")"))
      }
    } else if (x == "Auto") {
      res <- c(res, paste0(exptName, ",init,[AutomaticIrrigation].Script.AutoIrrigationOn = true"))
    } else {
      stop("unknown irr ", x, "\n")
    }
  }
  return(res)
}

reset.jsonManip(v8ctx)
simln <- jsonManip("base.apsimx")
expt <- jsonClone(getValue(simln, "$..[?(@.$type=='Models.Factorial.Experiment, Models')]", result="js"))  
delete.jsonManip(simln, "$..[?(@.$type=='Models.Factorial.Experiment, Models')]")
soils <- jsonClone(getValue(simln, "$..[?(@.Name=='Soils')]", result="js"))
delete.jsonManip(simln, "$..[?(@.Name=='Soils')]") 

operationsString <- "id,date,command"
for (site in unique(harv$site)) {
   newExp <- doit(expt, site)  
   appendChild(simln, "$.Children", newExp)
   operationsString <- c(operationsString, doInitialConditions(site))
}

writeLines(getValue(simln, "", result="string"), "Lentil.apsimx")
writeLines(paste(operationsString, collapse="\n"), paste0(getwd(), "/Lentil.operations.csv"))


# (Final, updated) data from Fernanda for Greenthorpe
gtdata <-read_xlsx("./Greenethorpe_data full - values_summary-Pete.xlsx") %>%
  filter(crop_type == "lentil") %>% 
  mutate(variety = gsub("PBA_", "", variety),
         SimulationName = paste0("Greenthorpe19TOS",tos, "Cultivar",variety,
                                 "Irr",irrigation)) %>%
  select(SimulationName, irrigation, tos, variety, harvest, contains("_mean")) %>%
  rename(`Lentil.SowingData.Cultivar` = variety,
         `Lentil.DaysAfterSowing` = das_mean,
         `Lentil.AboveGround.Wt` = tot_biom_m2_mean,
         `Lentil.Grain.Wt` = avg_yield_m2_mean,
         `HarvestIndex` = harvest_index_mean,
         `Lentil.Shell.PodNumber` = pods_per_m2_mean,
         `Lentil.Grain.Number` = seed_per_m2_mean,
          kwt1000 = kwt1000_mean,
          seeds_per_pod = seeds_per_pod_mean,
         `Lentil.LAI` = lai_mean,
         `Lentil.Stem.Wt` = stem_dwt_m2_mean,
         `Lentil.Leaf.Live.Wt` = greenleaf_dwt_m2_mean,
         `Lentil.Leaf.Dead.Wt` = yellowleaf_dwt_m2_mean,
         `Lentil.Shell.Wt` = pod_dwt_m2_mean,
         `TOS` = tos,
         `Irrigation` = irrigation) %>%
  mutate( site = "Greenethorpe",
          `Clock.Today` = as.Date(paste(doy_harvest_mean, "2019"), format="%j %Y"),
          Harvest = case_when(harvest == "Node 7" ~ "N7",
                              harvest == "Node 13" ~ "N13",
                              harvest == "50% Flowering" ~ "Flowering",
                              harvest == "50% Podding" ~ "Podding",
                              harvest == "Maturity" ~ "Maturity")) %>%
  select(- c(harvest, contains("_mean")))


harvestData <- harv %>%
 filter (! grepl("^Greenethorpe19.*", SimulationName ), # final data is separately above
         ! (grepl("^GreenethorpePhen19.*", SimulationName) & Stage == "StartPodding") ) %>%  # GreenethorpePhen has some cut/paste errors here 
 select(- c( `Lentil.Grain.NumberFunction.Value()`, `Lentil.Phenology.FinalNodeNo.Value()`,
              `Lentil.Shell.PodNumber.Value()`, `Leaf.dead.Nconc (g/g)`, 
             `Stem.dead.Nconc`, `Podwall.dead.Nconc`, GrainWtError)) %>%
 rename(`Harvest` = Stage,
        `Lentil.Grain.Wt` = GrainWt,
        branches_m2 = `branches/m2`,
        HarvestIndex = HI) %>%
 bind_rows(gtdata) #%>%
 #select_if( function(x) !(all(is.na(x)) | all(x=="")) ) #scrub empty columns

# How embarrassment...
harvestData$sowDate <-as.Date(NA)
for(i in 1:nrow(harv)) {
  harvestData$sowDate[i] <- defaults[[ paste0(harv$site[i], ",", harv$TOS[i]) ]]
}

# make tables of xxxDAS for all phenology points
#   Budding is a mix of DAS and date serials..
harvestData <- harvestData %>%
   mutate(`BuddingDAS` = ifelse(`50% Budding` < 365,
                                   `50% Budding`,
                                    as.numeric(
                                        as.Date(`50% Budding`, origin="1900-01-01") -
                                        sowDate))) 
  
for (h in unique(harvestData$Harvest)) {
  hDAS <- paste0(h, "DAS")
  hData <- as.numeric(rep(NA, nrow(harvestData)))
  if (hDAS %in% names(harvestData) ) {hData <- as.numeric(harvestData[[hDAS]])}
  for (missing in which(is.na(hData))) {
    key <- harvestData$SimulationName[missing]
    h2 <- which(harvestData$SimulationName == key & harvestData$Harvest == h)
    if (length(h2) == 1) {
      d2 <- harvestData$Clock.Today[h2]
      if (!is.na(d2)) {
        hData[missing] <- 
          as.numeric(d2 - harvestData$sowDate[missing])
      }
    }
  }
  harvestData[[hDAS]] <- hData
}

for (name in names(harvestData)) {
  if (all(is.na(harvestData[[ name ]]))) {harvestData[[ name ]] <- NULL}
}

# Assemble observed daily data
# Timeseries in second sheet 
tsData <- read_xlsx("./Observed for daniel.xlsx", sheet=2) %>%
  mutate(SimulationName = gsub("DR_2001Dooen", "Dooen01TOS1CvDigger", SimulationName, fixed = T),
         IRR = ifelse(grepl("Wet|Irr1", SimulationName), "Irrigated",
                      ifelse(grepl("Dry|Irr0", SimulationName), "Dryland", "")),
         Season = ifelse(grepl("Dooen", SimulationName), "01", "19")) %>%
  rename_with(~ gsub("Soil.SoilWater.", "", .x, fixed = TRUE))

tsData$site <- sapply(tsData$SimulationName, getSite)
tsData$TOS <- sapply(tsData$SimulationName, getTOS)
tsData$Pop <- sapply(tsData$SimulationName, getPop)

tsData$SimulationName <- with(tsData, paste0(site, "TOS", TOS, "Cultivar", Lentil.SowingData.Cultivar, 
                                         ifelse (IRR != "", paste0("Irr", IRR), ""),
                                         ifelse (Pop != "", paste0("Pop", Pop), "")))

tsData <- tsData %>%
  select_if( function(x) !all(is.na(x)) ) %>%
  mutate(Clock.Today = as.Date(Clock.Today)) %>%
  rename(`Lentil.Shell.Wt` = PodWt,
         `Lentil.Shell.WtError` = PodWtError,
         PodNumber_perplant = `Pod n/pl`)

# add greenethorpe soil observations
df0 <- read.xlsx("SWGreenethorpe2019.xlsx", 1) %>%
  filter(MixedCropType == "Lentil") %>% 
  mutate(Clock.Today = as.Date(SampleDate,  origin="1899-12-30"), # Weird...
         TOS = paste0("TOS", TOS),
         Lentil.SowingData.Cultivar = gsub("_2", "2", gsub("PBA_", "", Variety))) %>%
  # convert plot level to treatment means,
  group_by(TOS, WaterTrt, Lentil.SowingData.Cultivar, Clock.Today, LayerDepth_cm) %>%
  summarise(Vol_mm.mm = mean(Vol_mm.mm)) %>%
  group_by(TOS, WaterTrt, Lentil.SowingData.Cultivar, Clock.Today) %>%
  arrange(TOS, WaterTrt, Lentil.SowingData.Cultivar, Clock.Today, LayerDepth_cm) %>%
  # remap from NMM to apsim. Outside -> NA
  reframe(Clock.Today = unique(Clock.Today),
          TOS = unique(TOS),
          WaterTrt = unique(WaterTrt),
          Lentil.SowingData.Cultivar = unique(Lentil.SowingData.Cultivar),
          LayerDepthApsim_cm = c(10, 20, 50, 80, 110, 140, 170, 200),
          LayerNo = seq(1,8),
          Vol_mm.mm = approx(x=LayerDepth_cm, y=Vol_mm.mm, xout=LayerDepthApsim_cm, rule=1)$y) %>%
  rename(LayerDepth_cm = "LayerDepthApsim_cm") %>%
  mutate(layerName = paste0("SW(", LayerNo, ")")) %>% 
  # wide form
  select(-c(LayerDepth_cm, LayerNo)) %>%
  tidyr::pivot_wider (names_from=layerName, values_from=Vol_mm.mm)

df0$SimulationName <- with(df0, paste0("GreenethorpeTOS", TOS, "Cultivar", Lentil.SowingData.Cultivar, 
                                         ifelse (WaterTrt != "", paste0("Irr", WaterTrt), "")))
stopifnot(any(! df0$SimulationName %in% tsData$SimulationName) == FALSE)

# Split into two frames for replace / append
newDates <- as.Date(setdiff(df0$Clock.Today, tsData$Clock.Today[tsData$site =="Greenethorpe"]))

newObsns <- df0[match(newDates, df0$Clock.Today),] %>%
  mutate( DAS = NA,
          Season="2019",
          site= "Greenethorpe")
for(i in 1:nrow(newObsns)) {
  newObsns$DAS[i] <- newObsns$Clock.Today[i] - defaults[[ paste0("Greenethorpe,", newObsns$TOS[i]) ]]
}


tsData1 <- tsData %>% 
  rows_patch(df0[! df0$Clock.Today %in% newDates , ] %>% select(-c(WaterTrt)), 
             by= c("SimulationName", "Clock.Today"), unmatched = "error") %>%
  bind_rows(newObsns)

# final check
stopifnot(any(! tsData$SimulationName %in% harvestData$SimulationName) == FALSE)

sheet1 <- harvestData %>%
  #select(SimulationName, site, TOS, Lentil.SowingData.Cultivar, Irrigation, Season, Pop, contains("DAS")) %>%
  as.data.frame()
write.xlsx2(sheet1, "Lentil Observed.xlsx", sheetName = "ObservedHarvests", 
           col.names = TRUE, row.names = FALSE, showNA = FALSE, append = FALSE)

#sheet2 <- harvestData %>%
#  select(- contains("DAS")) %>%
##  as.data.frame()
#write.xlsx2(sheet2, "Lentil Observed.xlsx", sheetName = "ObservedHarvests", 
#            col.names = TRUE, row.names = FALSE, showNA = FALSE, append = TRUE)

write.xlsx2(as.data.frame(tsData), "Lentil Observed.xlsx", sheetName = "ObservedDaily", 
            col.names = TRUE, row.names = FALSE, showNA = FALSE, append = TRUE)
