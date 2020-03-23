#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


# nb. years are refered to as the sowing year eg. 2009/2010 year is refered to as 2009.




# ADD SimulationName Column and Clock.Today column 
# ------------------------------------------------
# nb. this is needed to do predicted vs observed in ApsimX

biomass <- biomass %>% mutate(date = dmy("21/10/2009") + days(DAS))
biomass <- biomass %>% mutate("Clock.Today" = format(date, "%d/%m/%Y"))
biomass <- biomass %>% select(-date)

biomass <- biomass %>%mutate(treatment2 = case_when(
        treatment == "Trt1 Means" ~ "Trt1",
        treatment == "Trt2 Means" ~ "Trt2",
        treatment == "Trt3 Means" ~ "Trt3",
        treatment == "Trt4 Means" ~ "Trt4",
        TRUE ~ treatment
))

biomass <- biomass %>% mutate(SimulationName = paste("Narrabri_", year, "Sow", treatment2, sep = ""))
biomass <- biomass %>% select(Clock.Today, SimulationName, treatment, treatment2, year, DAS, everything())




phenology <- phenology %>%mutate(treatment2 = case_when(
  treatment == "Trt 1 Means" ~ "Trt1",
  treatment == "Trt 2 Means" ~ "Trt2",
  treatment == "Trt 3 Means" ~ "Trt3",
  treatment == "Trt 4 Means" ~ "Trt4",
  TRUE ~ treatment
))

phenology <- phenology %>% mutate(SimulationName = paste("Narrabri_", year, "Sow", treatment2, sep = ""))
phenology <- phenology %>% select(SimulationName, treatment, treatment2, everything())


soilwater <- soilwater %>% mutate(SimulationName = paste("Narrabri_", year, "Sow", treatment, sep = ""))
soilwater <- soilwater %>% select(Clock.Today, SimulationName, treatment, everything())






sheets <- list(NarrabriRose_Phenology = phenology, NarrabriRose_Biomass = biomass, NarrabriRose_SoilWater = soilwater)  
write_xlsx(x = sheets, path = "./output/narrabri_rose.xlsx")





