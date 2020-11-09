#! /bin/bash


library(tidyverse)

library(readxl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Emerald","Observed","Emerald data")
path <- file.path(dir_sourcedata, "Soil Water Measurements 15-16 and 16-17sjy.xlsx")



# nb. year goes by the year of sowing (not the year of harvesting) 
# eg. 2015-2016 year would be called 2015.  



raw1 <- read_xlsx(path, sheet = "2016-17", range = "A4:M76")
raw2 <- read_xlsx(path, sheet = "2016-17", range = "P4:AB52")

raw <- bind_rows(raw1,raw2)


# remove the calculations for volumetric water and gravametric as the excel spreadsheet formulas are a bit suspect.
data <- raw %>% select(-10,-12,-13)


# rename variable to get rid of spaces in column names
data <- data %>% rename("Date" = DATE, "soil_core_cm" = `Soil Core cm`, "PD" = `Planting Date`, "depth" = DEPTH,
                        "wet_weight_g" = `WET WEIGHT`, "dry_weight_g" = `DRY WEIGHT`, "water_weight_g" = DIFFERENCE, 
                        "water_gravimetric" = `%` , "bulk_density" = `Bulk Density`)


# do the volumetric calculations yourself.
data <- data %>%  mutate(sw_volumetric = water_gravimetric * bulk_density)


# now get rid of columns we don't need
data <- data %>% select(-wet_weight_g, -dry_weight_g, -water_weight_g, -water_gravimetric)


# change the PD column (Planting Date) to sowing.
data <- data %>% rename("sowing" = PD)
data$sowing <- paste("S", data$sowing, sep = "")


# turn "depth" column into layer_no
data <- data %>% separate(col = depth, into = c("top", "bottom"), sep = "-", remove = TRUE, convert = TRUE)



# create a column with the layer number for each layer

group_rep <-  data %>% group_by(Date, sowing, Rep)

group_rep <- group_rep %>% mutate(layer_no = row_number())

data <- ungroup(group_rep)



# take the average of all the replicates.

group_layer_no <-  data %>% group_by(Date, sowing, layer_no)

group_layer_no <- group_layer_no %>% summarise(depth = first(soil_core_cm), sw_volumetric = mean(sw_volumetric), bulk_density = mean(bulk_density)  )

data <- ungroup(group_layer_no)



# add year column
data <- data %>%  rename("date" = Date)
data <- data %>%  mutate(year = 2016)
data <- data %>%  select(year, sowing, date, everything())
data <- data %>% rename("sw" = sw_volumetric, "bd" = bulk_density)



# turn (depth, sw, bd) rows for each layer number into columns so we can compare with simulation results.

sw2016 <- data %>% pivot_wider(names_from = layer_no, values_from = c(depth, sw, bd))

sw2016 <- sw2016 %>% arrange(year, sowing, date)








