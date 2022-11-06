#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Narrabri(Stephen)", "Soil Probe Data")
path <- file.path(dir_sourcedata, "ProbeReading Summary.xls")


#nb. the probes only went down to 120cm. So all the sw measurments are the cumulative sw down to 120cm





# ADD SimulationName Column and Clock.Today column 
# ------------------------------------------------
# nb. this is needed to do predicted vs observed in ApsimX

yield <- yield %>% mutate("Clock.Today" = format(date, "%d/%m/%Y"))
yield <- yield %>% mutate(SimulationName = paste("Narrabri_", year, "Sow", Treatment, sep = ""))
yield <- yield %>% select(Clock.Today, SimulationName, everything())

plant <- plant %>% mutate("Clock.Today" = format(date, "%d/%m/%Y"))
plant <- plant %>% mutate(SimulationName = paste("Narrabri_", year, "Sow", Treatment, sep = ""))
plant <- plant %>% select(Clock.Today, SimulationName, everything())

soilwater <- soilwater %>% mutate("Clock.Today" = format(date, "%d/%m/%Y"))
soilwater <- soilwater %>% mutate(SimulationName = paste("Narrabri_", year, "Sow", treatment, sep = ""))
soilwater <- soilwater %>% select(Clock.Today, SimulationName, everything())








# WRITE TO SPREADSHEET
# --------------------

sheets <- list(Narrabri_Yield = yield, Narrabri_Plant = plant, Narrabri_SoilWater = soilwater)  
write_xlsx(x = sheets, path = "./output/narrabri.xlsx")


