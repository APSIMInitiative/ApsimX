#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Emerald","Observed","Emerald data")
path <- file.path(dir_sourcedata, "EmeraldObs.xlsx")



# YIELD
# *****


heading <- read_xlsx(path, sheet = "EmeraldObs", range = "A1:Q1") 
raw <- read_xlsx(path, sheet = "EmeraldObs", range = "A3:Q16", col_names = colnames(heading),  na = "*") 

# remove the empty columns
raw <- raw %>% select( -bolls_sc, -lai_max, -dw_total, -nuptake_total, -esw)

# split the title column to create a year and sowing column. 
raw <- raw %>% separate(col= "title", into = c("year", "sowing"), sep = "_", remove = TRUE)

# clean up values in year to be just one year.
# nb. year goes by the year of sowing (not the year of harvesting) 
# eg. 2015-2016 year would be called 2015.  

raw$year <- case_when( 
                raw$year == "Emer1314" ~ "2013",
                raw$year == "Emer1415" ~ "2014",
                raw$year == "Emer1516" ~ "2015",
                raw$year == "Emer1617" ~ "2016"
                )

yield <- raw
#yield <- raw %>% select(-fs_DAS, -ff_DAS, -fob_DAS, -cutOut_DAS, -mat_DAS, -defol_DAS)

phenology <- raw %>% select(year, sowing, Variety, first_square = fs_DAS, first_flower = ff_DAS, first_open_boll = fob_DAS, cut_out = cutOut_DAS, maturity = mat_DAS, defoliation = defol_DAS)

phenology <- phenology %>% pivot_longer(cols = c(first_square, first_flower, first_open_boll, cut_out, maturity, defoliation), names_to = "stage_name", values_to = "das")



# FRUIT
# *****


heading <- read_xlsx(path, sheet =  "Emerald_TimeSeries", range = "A1:E1") 
fruit <- read_xlsx(path, sheet = "Emerald_TimeSeries", range = "A3:E72", col_names = colnames(heading), na = "*")



# split the title column to create a year and sowing column. 
fruit <- fruit %>% separate(col= "title", into = c("year", "sowing"), sep = "_", remove = TRUE)

# clean up values in year to be just one year.
# nb. year goes by the year of sowing (not the year of harvesting) 
# eg. 2015-2016 year would be called 2015.  

fruit$year <- case_when( 
  fruit$year == "Emer1314" ~ "2013",
  fruit$year == "Emer1415" ~ "2014",
  fruit$year == "Emer1516" ~ "2015",
  fruit$year == "Emer1617" ~ "2016"
)


                     
                          
                          
