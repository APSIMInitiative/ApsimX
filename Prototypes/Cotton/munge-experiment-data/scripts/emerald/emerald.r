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

sheets <- list(Phenology = phenology, Yield = yield, Fruit = fruit, SoilWater = soilwater)  
write_xlsx(x = sheets, path = "./output/emerald.xlsx")



# PLOT THE DATA 
# -------------



ggplot(data = fruit, mapping = aes(x=das, y=LAI, size=FruitNum)) + geom_point() + facet_grid(sowing ~ year) 

ggplot(data = fruit, mapping = aes(x=das, y=LAI, size=FruitNum, color=sowing)) + geom_point() + geom_smooth() + facet_grid(sowing ~ year)
