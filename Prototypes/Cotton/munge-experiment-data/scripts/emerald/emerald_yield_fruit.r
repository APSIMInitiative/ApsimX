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
yield <- read_xlsx(path, sheet = "EmeraldObs", range = "A3:Q16", col_names = colnames(heading),  na = "*") 

# remove the empty columns
yield <- yield %>% select( -bolls_sc, -lai_max, -dw_total, -nuptake_total, -esw)

# split the title column to create a year and sowing column. 
yield <- yield %>% separate(col= "title", into = c("year", "sowing"), sep = "_", remove = TRUE)

# clean up values in year to be just one year.
# nb. year goes by the year of sowing (not the year of harvesting) 
# eg. 2015-2016 year would be called 2015.  

yield$year <- case_when( 
                yield$year == "Emer1314" ~ "2013",
                yield$year == "Emer1415" ~ "2014",
                yield$year == "Emer1516" ~ "2015",
                yield$year == "Emer1617" ~ "2016",
                )






# FRUIT
# *****


heading <- read_xlsx(path, sheet =  "Emerald_TimeSeries", range = "A1:E1") 
fruit <- read_xlsx(path, sheet = "Emerald_TimeSeries", range = "A3:E72", col_names = colnames(heading))



# split the title column to create a year and sowing column. 
fruit <- fruit %>% separate(col= "title", into = c("year", "sowing"), sep = "_", remove = TRUE)

# clean up values in year to be just one year.
# nb. year goes by the year of sowing (not the year of harvesting) 
# eg. 2015-2016 year would be called 2015.  

fruit$year <- case_when( 
  fruit$year == "Emer1314" ~ "2013",
  fruit$year == "Emer1415" ~ "2014",
  fruit$year == "Emer1516" ~ "2015",
  fruit$year == "Emer1617" ~ "2016",
)



