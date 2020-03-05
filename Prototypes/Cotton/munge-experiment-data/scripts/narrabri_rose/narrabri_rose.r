#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


# nb. years are refered to as the sowing year eg. 2009/2010 year is refered to as 2009.


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Narrabri(Rose)","Dynamic Deficit studies","trial data")





sheets <- list(NarrabriRose_Phenology = phenology, NarrabriRose_Biomass = biomass, NarrabriRose_SoilWater = soilwater)  
write_xlsx(x = sheets, path = "./output/narrabri_rose.xlsx")





