#! /bin/bash


library(tidyverse)

library(lubridate)

#writes .xlsx files. 
#(you need to install the package from CRAN)
#https://cran.r-project.org/web/packages/writexl/writexl.pdf
library(writexl)         




# JOIN DATASETS

soilwater <- bind_rows(sw2015, sw2016)




# ADD SimulationName Column and Clock.Today column 
# ------------------------------------------------
# nb. this is needed to do predicted vs observed in ApsimX

yield <- yield %>% mutate("Clock.Today" = format(HarvestDate, "%d/%m/%Y"))
yield <- yield %>% mutate(SimulationName = paste("Emerald_", year, "Sow", sowing, sep = ""))
yield <- yield %>% select(Clock.Today, SimulationName, everything())

#need to create a date column using sowdate and das.
phenology <- phenology %>% mutate("Clock.Today" = format(HarvestDate, "%d/%m/%Y"))
phenology <- phenology %>% mutate(SimulationName = paste("Emerald_", year, "Sow", sowing, sep = ""))
phenology <- phenology %>% select(Clock.Today, SimulationName, everything())

fruit <- fruit %>% mutate("Clock.Today" = format(Date, "%d/%m/%Y"))
fruit <- fruit %>% mutate(SimulationName = paste("Emerald_", year, "Sow", sowing, sep = ""))
fruit <- fruit %>% select(Clock.Today, SimulationName, everything())

soilwater <- soilwater %>% mutate("Clock.Today" = format(date, "%d/%m/%Y"))
soilwater <- soilwater %>% mutate(SimulationName = paste("Emerald_", year, "Sow", sowing, sep = ""))
soilwater <- soilwater %>% select(Clock.Today, SimulationName, everything())





# WRITE TO SPREADSHEET
# --------------------

sheets <- list(Emerald_Phenology = phenology, Emerald_Yield = yield, Emerald_Fruit = fruit, Emerald_SoilWater = soilwater)  
write_xlsx(x = sheets, path = "./output/emerald.xlsx")



# PLOT THE DATA 
# -------------



ggplot(data = fruit, mapping = aes(x=das, y=LAI, size=FruitNum)) + geom_point() + facet_grid(sowing ~ year) 

ggplot(data = fruit, mapping = aes(x=das, y=LAI, size=FruitNum, color=sowing)) + geom_point() + geom_smooth() + facet_grid(sowing ~ year)
