#! /bin/bash


library(tidyverse)


#writes .xlsx files. 
#(you need to install the package from CRAN)
#https://cran.r-project.org/web/packages/writexl/writexl.pdf
library(writexl)         




# Phenology 
# ---------

phenology <- bind_rows(phenology_2008, phenology_2009, phenology_2010, phenology_2011, phenology_2012)



# Harvest Results
# ---------------

harvest <- bind_rows(harvest_2008, harvest_2009, harvest_2010, harvest_2011, harvest_2012)



# Plant Information
# -----------------

plant <- bind_rows(plant_2008, plant_2009, plant_2010, plant_2011, plant_2012)




# UNIQUE VARIABLES MEASURED ON RANDOM DATES
# -----------------------------------------


light_interception <- bind_rows(light_interception_2008, light_interception_2009, light_interception_2010, light_interception_2011, light_interception_2012)

nodes_over_time <- bind_rows(nodes_over_time_2008, nodes_over_time_2009, nodes_over_time_2010, nodes_over_time_2011, nodes_over_time_2012)




# BOLLS (Open Boll % and Mean Boll Weight)
# ----------------------------------------

bolls <- bind_rows(bolls_2009, bolls_2010, bolls_2011, bolls_2012) #nb. 2008 data is missing.





# WRITE TO SPREADSHEET
# --------------------

sheets <- list(Ayr_Phenology = phenology, Ayr_Harvest = harvest, Ayr_Plant = plant, Ayr_Light = light_interception, Ayr_Nodes = nodes_over_time, Ayr_Bolls = bolls)  
write_xlsx(x = sheets, path = "./output/ayr.xlsx")


