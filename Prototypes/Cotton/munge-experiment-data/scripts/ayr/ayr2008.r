#! /bin/bash


library(tidyverse)

library(readxl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Ayr","Observed")
path <- file.path(dir_sourcedata, "Burdekin Data for OZCOT.xls")


#nb. s1, s2_s3 refers to the different sowing dates


# Phenology 
# ---------
s1_table1 <- read_xls(path, sheet = "2008", range = "A17:Q21")
s2_table1 <- read_xls(path, sheet = "2008", range = "A34:Q38")
s3_table1 <- read_xls(path, sheet = "2008", range = "A51:Q55")

s1_table1 <- s1_table1 %>% mutate(sowing = "s1")
s2_table1 <- s2_table1 %>% mutate(sowing = "s2")
s3_table1 <- s3_table1 %>% mutate(sowing = "s3")

table1 <- bind_rows(s1_table1, s2_table1, s3_table1)
#nb. remove -10 because "60% OB" column is garbage. 
# Columns become rows later so no need to replace the data with na's. 
# All that will happen is the rows for "60% OB" will be missing.
table1 <- table1 %>%  select(-3,-4,-5,-10) 
table1 <- table1 %>% rename("variety" = Varieties)

#filter out non phenology columns
phenology <- table1 %>%  select(variety, sowing, colnames(table1)[3:8])

phenology <- phenology %>% gather("event_name", "event_date", colnames(phenology)[3:8])

phenology <- phenology %>% select(variety, sowing, event_date, event_name)
phenology <- phenology %>% arrange(variety, sowing, event_date)

#calculate DAS
phenology_grouped <- phenology %>% group_by(variety, sowing)
phenology_DAS <- phenology_grouped %>% mutate(DAS = difftime(event_date, min(event_date), unit="days"))
phenology_DAS <- phenology_DAS %>% arrange(variety, sowing, event_date)
phenology <- phenology_DAS

phenology <- phenology %>% mutate(year ="2008")
phenology_2008 <- phenology %>% select(year, variety, sowing, event_date, DAS, event_name) 



# Harvest Results
# ---------------

#filter out phenology columns
harvest <- table1 %>%  select(variety, sowing, colnames(table1)[8:13])
harvest <- harvest %>% rename(date = `Harvest Maturity`)
harvest <- harvest %>% mutate(year = "2008")
harvest_2008 <- harvest %>% select(year, variety, sowing, date, everything())



# Weather
# -------
weather <- read_xls(path, sheet = "2008", range = "A61:E274")



# Plant Information
# -----------------

# first section

s1_square_no <- read_xls(path, sheet = "2008", range = "T9:Y17")
s2_square_no <- read_xls(path, sheet = "2008", range = "T20:Y31")
s3_square_no <- read_xls(path, sheet = "2008", range = "T35:Y45")

s1_square_mass <- read_xls(path, sheet = "2008", range = "AA9:AF17")
s2_square_mass <- read_xls(path, sheet = "2008", range = "AA20:AF31")
s3_square_mass <- read_xls(path, sheet = "2008", range = "AA35:AF45")

s1_green_boll_no <- read_xls(path, sheet = "2008", range = "AI9:AN17")
s2_green_boll_no <- read_xls(path, sheet = "2008", range = "AI20:AN31")
s3_green_boll_no <- read_xls(path, sheet = "2008", range = "AI35:AN45")

s1_green_boll_mass <- read_xls(path, sheet = "2008", range = "AQ9:AV17")
s2_green_boll_mass <- read_xls(path, sheet = "2008", range = "AQ20:AV31")
s3_green_boll_mass <- read_xls(path, sheet = "2008", range = "AQ35:AV45")

s1_open_boll_no <- read_xls(path, sheet = "2008", range = "AY9:BD17")
s2_open_boll_no <- read_xls(path, sheet = "2008", range = "AY20:BD31")
s3_open_boll_no <- read_xls(path, sheet = "2008", range = "AY35:BD45")

s1_open_boll_mass <- read_xls(path, sheet = "2008", range = "BF9:BK17")
s2_open_boll_mass <- read_xls(path, sheet = "2008", range = "BF20:BK31")
s3_open_boll_mass <- read_xls(path, sheet = "2008", range = "BF35:BK45")

s1_unharvest_boll_no <- read_xls(path, sheet = "2008", range = "BN9:BS17")
s2_unharvest_boll_no <- read_xls(path, sheet = "2008", range = "BN20:BS31")
s3_unharvest_boll_no <- read_xls(path, sheet = "2008", range = "BN35:BS45")

s1_unharvest_boll_mass <- read_xls(path, sheet = "2008", range = "BV9:CA17")
s2_unharvest_boll_mass <- read_xls(path, sheet = "2008", range = "BV20:CA31")
s3_unharvest_boll_mass <- read_xls(path, sheet = "2008", range = "BV35:CA45")

s1_total_retention <- read_xls(path, sheet = "2008", range = "CD9:CI17")
s2_total_retention <- read_xls(path, sheet = "2008", range = "CD20:CI31")
s3_total_retention <- read_xls(path, sheet = "2008", range = "CD35:CI45")


# below first section

s1_leaf_area <- read_xls(path, sheet = "2008", range = "T54:Y62")
s2_leaf_area <- read_xls(path, sheet = "2008", range = "T65:Y76")
s3_leaf_area <- read_xls(path, sheet = "2008", range = "T80:Y90")

s1_leaf_mass <- read_xls(path, sheet = "2008", range = "AA54:AF62")
s2_leaf_mass <- read_xls(path, sheet = "2008", range = "AA65:AF76")
s3_leaf_mass <- read_xls(path, sheet = "2008", range = "AA80:AF90")

s1_stem_mass <- read_xls(path, sheet = "2008", range = "AI54:AN62")
s2_stem_mass <- read_xls(path, sheet = "2008", range = "AI65:AN76")
s3_stem_mass <- read_xls(path, sheet = "2008", range = "AI80:AN90")

s1_lai <- read_xls(path, sheet = "2008", range = "AQ54:AV62")
s2_lai <- read_xls(path, sheet = "2008", range = "AQ65:AV76")
s3_lai <- read_xls(path, sheet = "2008", range = "AQ80:AV90")

# DO NOT JOIN THESE

s1_light_interception <- read_xls(path, sheet = "2008", range = "AY54:BD58")
s2_light_interception <- read_xls(path, sheet = "2008", range = "AY65:BD69")
s3_light_interception <- read_xls(path, sheet = "2008", range = "AY80:BD85")

s1_nodes_over_time <- read_xls(path, sheet = "2008", range = "BF54:BK67")
s2_nodes_over_time <- read_xls(path, sheet = "2008", range = "BF70:BK83")
s3_nodes_over_time <- read_xls(path, sheet = "2008", range = "BF86:BK100")





# Start Munging the data
# **********************

colnames(s1_square_no)[1] <- "date"
colnames(s2_square_no)[1] <- "date"
colnames(s3_square_no)[1] <- "date"

colnames(s1_square_mass)[1] <- "date"
colnames(s2_square_mass)[1] <- "date"
colnames(s3_square_mass)[1] <- "date"

colnames(s1_green_boll_no )[1] <- "date"
colnames(s2_green_boll_no )[1] <- "date"
colnames(s3_green_boll_no )[1] <- "date"

colnames(s1_green_boll_mass)[1] <- "date"
colnames(s2_green_boll_mass)[1] <- "date"
colnames(s3_green_boll_mass)[1] <- "date"

colnames(s1_open_boll_no)[1] <- "date"
colnames(s2_open_boll_no)[1] <- "date"
colnames(s3_open_boll_no)[1] <- "date"

colnames(s1_open_boll_mass)[1] <- "date"
colnames(s2_open_boll_mass)[1] <- "date"
colnames(s3_open_boll_mass)[1] <- "date"

colnames(s1_unharvest_boll_no)[1] <- "date"
colnames(s2_unharvest_boll_no)[1] <- "date"
colnames(s3_unharvest_boll_no)[1] <- "date"

colnames(s1_unharvest_boll_mass)[1] <- "date"
colnames(s2_unharvest_boll_mass)[1] <- "date"
colnames(s3_unharvest_boll_mass)[1] <- "date"

colnames(s1_total_retention)[1] <- "date"
colnames(s2_total_retention)[1] <- "date"
colnames(s3_total_retention)[1] <- "date"

colnames(s1_leaf_area)[1] <- "date"
colnames(s2_leaf_area)[1] <- "date"
colnames(s3_leaf_area)[1] <- "date"

colnames(s1_leaf_mass)[1] <- "date"
colnames(s2_leaf_mass)[1] <- "date"
colnames(s3_leaf_mass)[1] <- "date"

colnames(s1_stem_mass)[1] <- "date"
colnames(s2_stem_mass)[1] <- "date"
colnames(s3_stem_mass)[1] <- "date"

colnames(s1_lai)[1] <- "date"
colnames(s2_lai)[1] <- "date"
colnames(s3_lai)[1] <- "date"

#I think there are typos in the spreadsheet but do this as a fix for now
colnames(s1_unharvest_boll_mass)[3:6] <- colnames(s1_unharvest_boll_no)[3:6]
colnames(s1_total_retention)[3:6] <- colnames(s1_unharvest_boll_no)[3:6]
colnames(s2_total_retention)[3:6] <- colnames(s1_unharvest_boll_no)[3:6]
colnames(s3_total_retention)[3:6] <- colnames(s1_unharvest_boll_no)[3:6]


s1_square_no <- s1_square_no %>% mutate(sowing = "s1")
s2_square_no <- s2_square_no %>% mutate(sowing = "s2")
s3_square_no <- s3_square_no %>% mutate(sowing = "s3")

s1_square_mass <- s1_square_mass %>% mutate(sowing = "s1")
s2_square_mass <- s2_square_mass %>% mutate(sowing = "s2")
s3_square_mass <- s3_square_mass %>% mutate(sowing = "s3")

s1_green_boll_no <- s1_green_boll_no %>% mutate(sowing = "s1")
s2_green_boll_no <- s2_green_boll_no %>% mutate(sowing = "s2")
s3_green_boll_no <- s3_green_boll_no %>% mutate(sowing = "s3")

s1_green_boll_mass <- s1_green_boll_mass %>% mutate(sowing = "s1")
s2_green_boll_mass <- s2_green_boll_mass %>% mutate(sowing = "s2")
s3_green_boll_mass <- s3_green_boll_mass %>% mutate(sowing = "s3")

s1_open_boll_no <- s1_open_boll_no %>% mutate(sowing = "s1")
s2_open_boll_no <- s2_open_boll_no %>% mutate(sowing = "s2")
s3_open_boll_no <- s3_open_boll_no %>% mutate(sowing = "s3")

s1_open_boll_mass <- s1_open_boll_mass %>% mutate(sowing = "s1")
s2_open_boll_mass <- s2_open_boll_mass %>% mutate(sowing = "s2")
s3_open_boll_mass <- s3_open_boll_mass %>% mutate(sowing = "s3")

s1_unharvest_boll_no <- s1_unharvest_boll_no %>% mutate(sowing = "s1")
s2_unharvest_boll_no <- s2_unharvest_boll_no %>% mutate(sowing = "s2")
s3_unharvest_boll_no <- s3_unharvest_boll_no %>% mutate(sowing = "s3")

s1_unharvest_boll_mass <- s1_unharvest_boll_mass %>% mutate(sowing = "s1")
s2_unharvest_boll_mass <- s2_unharvest_boll_mass %>% mutate(sowing = "s2")
s3_unharvest_boll_mass <- s3_unharvest_boll_mass %>% mutate(sowing = "s3")

s1_total_retention <- s1_total_retention %>% mutate(sowing = "s1")
s2_total_retention <- s2_total_retention %>% mutate(sowing = "s2")
s3_total_retention <- s3_total_retention %>% mutate(sowing = "s3")

s1_leaf_area <- s1_leaf_area %>% mutate(sowing = "s1")
s2_leaf_area <- s2_leaf_area %>% mutate(sowing = "s2")
s3_leaf_area <- s3_leaf_area %>% mutate(sowing = "s3")

s1_leaf_mass <- s1_leaf_mass %>% mutate(sowing = "s1")
s2_leaf_mass <- s2_leaf_mass %>% mutate(sowing = "s2")
s3_leaf_mass <- s3_leaf_mass %>% mutate(sowing = "s3")

s1_stem_mass <- s1_stem_mass %>% mutate(sowing = "s1")
s2_stem_mass <- s2_stem_mass %>% mutate(sowing = "s2")
s3_stem_mass <- s3_stem_mass %>% mutate(sowing = "s3")

s1_lai <- s1_lai %>% mutate(sowing = "s1")
s2_lai <- s2_lai %>% mutate(sowing = "s2")
s3_lai <- s3_lai %>% mutate(sowing = "s3")



square_no <- bind_rows(s1_square_no, s2_square_no, s3_square_no)
square_mass <- bind_rows(s1_square_mass, s2_square_mass, s3_square_mass)
green_boll_no <- bind_rows(s1_green_boll_no, s2_green_boll_no, s3_green_boll_no)
green_boll_mass <- bind_rows(s1_green_boll_mass, s2_green_boll_mass, s3_green_boll_mass)
open_boll_no <- bind_rows(s1_open_boll_no, s2_open_boll_no, s3_open_boll_no)
open_boll_mass <- bind_rows(s1_open_boll_mass, s2_open_boll_mass, s3_open_boll_mass)
unharvest_boll_no <- bind_rows(s1_unharvest_boll_no, s2_unharvest_boll_no, s3_unharvest_boll_no)
unharvest_boll_mass <- bind_rows(s1_unharvest_boll_mass, s2_unharvest_boll_mass, s3_unharvest_boll_mass)
total_retention <- bind_rows(s1_total_retention, s2_total_retention, s3_total_retention)
leaf_area <- bind_rows(s1_leaf_area, s2_leaf_area, s3_leaf_area)
leaf_mass <- bind_rows(s1_leaf_mass, s2_leaf_mass, s3_leaf_mass)
stem_mass <- bind_rows(s1_stem_mass, s2_stem_mass, s3_stem_mass)
lai <- bind_rows(s1_lai, s2_lai, s3_lai)



square_no <- square_no %>% gather("variety", "square_no", colnames(square_no)[3:6])
square_mass <- square_mass %>% gather("variety", "square_mass", colnames(square_mass)[3:6])
green_boll_no <- green_boll_no %>% gather("variety", "green_boll_no", colnames(green_boll_no)[3:6])
green_boll_mass <- green_boll_mass %>% gather("variety", "green_boll_mass", colnames(green_boll_mass)[3:6])
open_boll_no <- open_boll_no %>% gather("variety", "open_boll_no", colnames(open_boll_no)[3:6])
open_boll_mass <- open_boll_mass %>% gather("variety", "open_boll_mass", colnames(open_boll_mass)[3:6])
unharvest_boll_no <- unharvest_boll_no %>% gather("variety", "unharvest_boll_no", colnames(unharvest_boll_no)[3:6])
unharvest_boll_mass <- unharvest_boll_mass %>% gather("variety", "unharvest_boll_mass", colnames(unharvest_boll_mass)[3:6])
total_retention <- total_retention %>% gather("variety", "total_retention", colnames(total_retention)[3:6])
leaf_area <- leaf_area %>% gather("variety", "leaf_area", colnames(leaf_area)[3:6])
leaf_mass <- leaf_mass %>% gather("variety", "leaf_mass", colnames(leaf_mass)[3:6])
stem_mass <- stem_mass %>% gather("variety", "stem_mass", colnames(stem_mass)[3:6])
lai <- lai %>% gather("variety", "lai", colnames(lai)[3:6])



plant_2008 <- full_join(square_no, square_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(green_boll_no, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(green_boll_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(open_boll_no, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(open_boll_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(unharvest_boll_no, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(unharvest_boll_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(total_retention, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(leaf_area, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(leaf_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(stem_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
              full_join(lai, by = c("sowing", "variety", "date", "DAS"))  


plant_2008 <- plant_2008 %>% mutate(year = "2008")

plant_2008 <- plant_2008 %>% select(year, variety, sowing, date, DAS, everything())





# UNIQUE VARIABLES MEASURED ON RANDOM DATES
# -----------------------------------------


colnames(s1_light_interception)[1] <- "date"
colnames(s2_light_interception)[1] <- "date"
colnames(s3_light_interception)[1] <- "date"

s1_light_interception <- s1_light_interception %>% mutate(sowing = "s1")
s2_light_interception <- s2_light_interception %>% mutate(sowing = "s2")
s3_light_interception <- s3_light_interception %>% mutate(sowing = "s3")

light_interception <- bind_rows(s1_light_interception, s2_light_interception, s3_light_interception)

light_interception <- light_interception %>% gather("variety", "light_interception", colnames(light_interception)[3:6])

light_interception <- light_interception %>% mutate(year = "2008")

light_interception_2008 <- light_interception %>% select(year, variety, sowing, date, DAS, everything())




colnames(s1_nodes_over_time)[1] <- "date"
colnames(s2_nodes_over_time)[1] <- "date"
colnames(s3_nodes_over_time)[1] <- "date"

s1_nodes_over_time <- s1_nodes_over_time %>% mutate(sowing = "s1")
s2_nodes_over_time <- s2_nodes_over_time %>% mutate(sowing = "s2")
s3_nodes_over_time <- s3_nodes_over_time %>% mutate(sowing = "s3")

nodes_over_time <- bind_rows(s1_nodes_over_time, s2_nodes_over_time, s3_nodes_over_time)

nodes_over_time <- nodes_over_time %>% gather("variety", "nodes_over_time", colnames(nodes_over_time)[3:6])

nodes_over_time <- nodes_over_time %>% mutate(year = "2008")

nodes_over_time_2008 <- nodes_over_time %>% select(year, variety, sowing, date, DAS, everything())




# BOLLS (Open Boll % and Mean Boll Weight)
# ----------------------------------------

# These are not present in the 2008 dataset.





