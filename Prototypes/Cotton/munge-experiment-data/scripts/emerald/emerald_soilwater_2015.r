#! /bin/bash


library(tidyverse)

library(readxl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Emerald","Observed","Emerald data")
path <- file.path(dir_sourcedata, "Soil Water Measurements 15-16 and 16-17sjy.xlsx")



# nb. year goes by the year of sowing (not the year of harvesting) 
# eg. 2015-2016 year would be called 2015.  



raw <- read_xlsx(path, sheet = "2015-16", range = "A3:P179")


# remove the calculations for volumetric water and gravametric as the excel spreadsheet formulas are a bit suspect.
data <- raw %>% select(-13,-15,-16)


# rename variable to get rid of spaces in column names
data <- data %>% rename("hill_furrow" = `Hill/Furrow`, "sample_no" = `sample no`, "soil_core_cm" = `Soil core cm`, 
                                "wet_weight_g" = `wet weight (g)`, "dry_weight_g" = `dry weight (g)`, "water_weight_g" = `weight difference`, 
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

group_sample_no <-  data %>% group_by(Date, sowing, Rep, hill_furrow, sample_no)

group_sample_no <- group_sample_no %>% mutate(layer_no = row_number())

data <- ungroup(group_sample_no)




# take the average of all the samples 

group_layer_no <-  data %>% group_by(Date, sowing, Rep, hill_furrow, layer_no)
                                                        
group_layer_no <- group_layer_no %>% summarise(depth = first(soil_core_cm), sw_volumetric = mean(sw_volumetric), bulk_density = mean(bulk_density)  )

data <- ungroup(group_layer_no)



# take the average of all the replicates.

group_layer_no <-  data %>% group_by(Date, sowing, hill_furrow, layer_no)

group_layer_no <- group_layer_no %>% summarise(depth = first(depth), sw_volumetric = mean(sw_volumetric), bulk_density = mean(bulk_density)  )

data <- ungroup(group_layer_no)



# add year column
data <- data %>%  rename("date" = Date)
data <- data %>%  mutate(year = 2015)
data <- data %>%  select(year, sowing, date, everything())
data <- data %>% rename("sw" = sw_volumetric, "bd" = bulk_density)



# save current state with new variable because we want add this to a worksheet on its own,
# in case we want to simulate hills vs furrows



# turn (depth, sw, bd) rows for each layer number into columns so we can compare with simulation results.

sw2015_hill_furrow <- data %>% pivot_wider(id_cols = c(year, sowing, hill_furrow, date), names_from = layer_no, values_from = c(depth, sw, bd))

# long <- data %>% gather(key = "variable", value = "value", c(depth, sw, bd))
# newcol <- long %>% unite(newname, variable, layer_no, sep = "_")  
# sw2015_hill_furrow <- newcol %>% spread(key = newname, value = value)
# sw2015_hill_furrow <- sw2015_hill_furrow %>% arrange(year, sowing, date)





# take average of hill and furrow

group_layer_no <-  data %>% group_by(year, sowing, date, layer_no)

group_layer_no <- group_layer_no %>% summarise(depth = first(depth), sw = mean(sw), bd = mean(bd)  )

data <- ungroup(group_layer_no)


# turn (depth, sw, bd) rows for each layer number into columns so we can compare with simulation results.

#sw2015 <- data %>% pivot_wider(id_cols = c(year, sowing, date), names_from = layer_no, values_from = c(depth, sw, bd))
sw2015 <- data %>% pivot_wider(names_from = layer_no, values_from = c(depth, sw, bd))

# long <- data %>% gather(key = "variable", value = "value", c(depth, sw, bd))
# newcol <- long %>% unite(newname, variable, layer_no, sep = "_")  
# sw2015 <- newcol %>% spread(key = newname, value = value)
# sw2015 <- sw2015 %>% arrange(year, sowing, date)


help("pivot_wider")
help("pivot_longer")






