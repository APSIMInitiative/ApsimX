#! /bin/bash


library(tidyverse)


#writes .xlsx files. 
#(you need to install the package from CRAN)
#https://cran.r-project.org/web/packages/writexl/writexl.pdf
library(writexl)         




# Phenology 
# ---------

soilwater <- bind_rows(sw2015, sw2016)



# WRITE TO SPREADSHEET
# --------------------

sheets <- list(Yield = yield, Fruit = fruit, SoilWater = soilwater)  
write_xlsx(x = sheets, path = "./output/emerald.xlsx")


