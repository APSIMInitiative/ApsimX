#! /bin/bash


library(tidyverse)

library(readxl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Ayr","Observed")
path <- file.path(dir_sourcedata, "Burdekin Data for OZCOT.xls")


#nb. s1, s2_s3 refers to the different sowing dates


# Phenology 
# ---------
s1_table1 <- read_xls(path, sheet = "2012", range = "A21:Q24")
s2_table1 <- read_xls(path, sheet = "2012", range = "A37:Q40")

s1_table1 <- s1_table1 %>% mutate(sowing = "s1")
s2_table1 <- s2_table1 %>% mutate(sowing = "s2")

table1 <- bind_rows(s1_table1, s2_table1)
table1 <- table1 %>%  select(-3,-4,-5) 
table1 <- table1 %>% rename("variety" = Varieties)

#filter out non phenology columns
phenology <- table1 %>%  select(variety, sowing, colnames(table1)[3:9])

phenology <- phenology %>% gather("event_name", "event_date", colnames(phenology)[3:9])

phenology <- phenology %>% select(variety, sowing, event_date, event_name)
phenology <- phenology %>% arrange(variety, sowing, event_date)

#calculate DAS
phenology_grouped <- phenology %>% group_by(variety, sowing)
phenology_DAS <- phenology_grouped %>% mutate(DAS = difftime(event_date, min(event_date), unit="days"))
phenology_DAS <- phenology_DAS %>% arrange(variety, sowing, event_date)
phenology <- phenology_DAS

phenology <- phenology %>% mutate(year ="2012")
phenology_2012 <- phenology %>% select(year, variety, sowing, event_date, DAS, event_name) 



# Harvest Results
# ---------------

#filter out phenology columns
harvest <- table1 %>%  select(variety, sowing, colnames(table1)[9:14])
harvest <- harvest %>% rename(date = `Harvest Maturity`)
harvest <- harvest %>% mutate(year = "2012")
harvest_2012 <- harvest %>% select(year, variety, sowing, date, everything())



# Weather
# -------
weather <- read_xls(path, sheet = "2012", range = "A85:E297")



# Plant Information
# -----------------

# first section

s1_square_no <- read_xls(path, sheet = "2012", range = "S10:W18")
s2_square_no <- read_xls(path, sheet = "2012", range = "S21:W29")

s1_square_mass <- read_xls(path, sheet = "2012", range = "Z10:AD18")
s2_square_mass <- read_xls(path, sheet = "2012", range = "Z21:AD29")

s1_green_boll_no <- read_xls(path, sheet = "2012", range = "AG10:AK18")
s2_green_boll_no <- read_xls(path, sheet = "2012", range = "AG21:AK29")

s1_green_boll_mass <- read_xls(path, sheet = "2012", range = "AN10:AR18")
s2_green_boll_mass <- read_xls(path, sheet = "2012", range = "AN21:AR29")

s1_open_boll_no <- read_xls(path, sheet = "2012", range = "AU10:AY18")
s2_open_boll_no <- read_xls(path, sheet = "2012", range = "AU21:AY29")

s1_open_boll_mass <- read_xls(path, sheet = "2012", range = "BB10:BF18")
s2_open_boll_mass <- read_xls(path, sheet = "2012", range = "BB21:BF29")

s1_unharvest_boll_no <- read_xls(path, sheet = "2012", range = "BI10:BM18")
s2_unharvest_boll_no <- read_xls(path, sheet = "2012", range = "BI21:BM29")

s1_unharvest_boll_mass <- read_xls(path, sheet = "2012", range = "BP10:BT18")
s2_unharvest_boll_mass <- read_xls(path, sheet = "2012", range = "BP21:BT29")

s1_total_retention <- read_xls(path, sheet = "2012", range = "BW10:CA18")
s2_total_retention <- read_xls(path, sheet = "2012", range = "BW21:CA29")



# below first section

s1_leaf_area <- read_xls(path, sheet = "2012", range = "S34:W42")
s2_leaf_area <- read_xls(path, sheet = "2012", range = "S45:W53")

s1_leaf_mass <- read_xls(path, sheet = "2012", range = "Z34:AD42")
s2_leaf_mass <- read_xls(path, sheet = "2012", range = "Z45:AD53")

s1_stem_mass <- read_xls(path, sheet = "2012", range = "AG34:AK42")
s2_stem_mass <- read_xls(path, sheet = "2012", range = "AG45:AK53")

s1_lai <- read_xls(path, sheet = "2012", range = "AN34:AR42")
s2_lai <- read_xls(path, sheet = "2012", range = "AN45:AR53")


# DO NOT JOIN THESE

s1_light_interception <- read_xls(path, sheet = "2012", range = "AU34:AY41") 
s2_light_interception <- read_xls(path, sheet = "2012", range = "AU45:AY51")

#this isn't in 2008 dataset
s1_open_boll_percent <- read_xls(path, sheet = "2012", range = "BB34:BF42")
s2_open_boll_percent <- read_xls(path, sheet = "2012", range = "BB46:BF52")

#this isn't in 2008 dataset
s1_mean_boll_weight <- read_xls(path, sheet = "2012", range = "BI34:BM42")
s2_mean_boll_weight <- read_xls(path, sheet = "2012", range = "BI46:BM52")

s1_nodes_over_time <- read_xls(path, sheet = "2012", range = "BP34:BT43")
s2_nodes_over_time <- read_xls(path, sheet = "2012", range = "BP46:BT53")





# Start Munging the data
# **********************


colnames(s1_square_no)[1] <- "date"
colnames(s2_square_no)[1] <- "date"

colnames(s1_square_mass)[1] <- "date"
colnames(s2_square_mass)[1] <- "date"

colnames(s1_green_boll_no )[1] <- "date"
colnames(s2_green_boll_no )[1] <- "date"

colnames(s1_green_boll_mass)[1] <- "date"
colnames(s2_green_boll_mass)[1] <- "date"

colnames(s1_open_boll_no)[1] <- "date"
colnames(s2_open_boll_no)[1] <- "date"

colnames(s1_open_boll_mass)[1] <- "date"
colnames(s2_open_boll_mass)[1] <- "date"

colnames(s1_unharvest_boll_no)[1] <- "date"
colnames(s2_unharvest_boll_no)[1] <- "date"

colnames(s1_unharvest_boll_mass)[1] <- "date"
colnames(s2_unharvest_boll_mass)[1] <- "date"

colnames(s1_total_retention)[1] <- "date"
colnames(s2_total_retention)[1] <- "date"

colnames(s1_leaf_area)[1] <- "date"
colnames(s2_leaf_area)[1] <- "date"

colnames(s1_leaf_mass)[1] <- "date"
colnames(s2_leaf_mass)[1] <- "date"

colnames(s1_stem_mass)[1] <- "date"
colnames(s2_stem_mass)[1] <- "date"

colnames(s1_lai)[1] <- "date"
colnames(s2_lai)[1] <- "date"



s1_square_no <- s1_square_no %>% mutate(sowing = "s1")
s2_square_no <- s2_square_no %>% mutate(sowing = "s2")

s1_square_mass <- s1_square_mass %>% mutate(sowing = "s1")
s2_square_mass <- s2_square_mass %>% mutate(sowing = "s2")

s1_green_boll_no <- s1_green_boll_no %>% mutate(sowing = "s1")
s2_green_boll_no <- s2_green_boll_no %>% mutate(sowing = "s2")

s1_green_boll_mass <- s1_green_boll_mass %>% mutate(sowing = "s1")
s2_green_boll_mass <- s2_green_boll_mass %>% mutate(sowing = "s2")

s1_open_boll_no <- s1_open_boll_no %>% mutate(sowing = "s1")
s2_open_boll_no <- s2_open_boll_no %>% mutate(sowing = "s2")

s1_open_boll_mass <- s1_open_boll_mass %>% mutate(sowing = "s1")
s2_open_boll_mass <- s2_open_boll_mass %>% mutate(sowing = "s2")

s1_unharvest_boll_no <- s1_unharvest_boll_no %>% mutate(sowing = "s1")
s2_unharvest_boll_no <- s2_unharvest_boll_no %>% mutate(sowing = "s2")

s1_unharvest_boll_mass <- s1_unharvest_boll_mass %>% mutate(sowing = "s1")
s2_unharvest_boll_mass <- s2_unharvest_boll_mass %>% mutate(sowing = "s2")

s1_total_retention <- s1_total_retention %>% mutate(sowing = "s1")
s2_total_retention <- s2_total_retention %>% mutate(sowing = "s2")

s1_leaf_area <- s1_leaf_area %>% mutate(sowing = "s1")
s2_leaf_area <- s2_leaf_area %>% mutate(sowing = "s2")

s1_leaf_mass <- s1_leaf_mass %>% mutate(sowing = "s1")
s2_leaf_mass <- s2_leaf_mass %>% mutate(sowing = "s2")

s1_stem_mass <- s1_stem_mass %>% mutate(sowing = "s1")
s2_stem_mass <- s2_stem_mass %>% mutate(sowing = "s2")

s1_lai <- s1_lai %>% mutate(sowing = "s1")
s2_lai <- s2_lai %>% mutate(sowing = "s2")



square_no <- bind_rows(s1_square_no, s2_square_no)
square_mass <- bind_rows(s1_square_mass, s2_square_mass)
green_boll_no <- bind_rows(s1_green_boll_no, s2_green_boll_no)
green_boll_mass <- bind_rows(s1_green_boll_mass, s2_green_boll_mass)
open_boll_no <- bind_rows(s1_open_boll_no, s2_open_boll_no)
open_boll_mass <- bind_rows(s1_open_boll_mass, s2_open_boll_mass)
unharvest_boll_no <- bind_rows(s1_unharvest_boll_no, s2_unharvest_boll_no)
unharvest_boll_mass <- bind_rows(s1_unharvest_boll_mass, s2_unharvest_boll_mass)
total_retention <- bind_rows(s1_total_retention, s2_total_retention)
leaf_area <- bind_rows(s1_leaf_area, s2_leaf_area)
leaf_mass <- bind_rows(s1_leaf_mass, s2_leaf_mass)
stem_mass <- bind_rows(s1_stem_mass, s2_stem_mass)
lai <- bind_rows(s1_lai, s2_lai)



square_no <- square_no %>% gather("variety", "square_no", colnames(square_no)[3:5])
square_mass <- square_mass %>% gather("variety", "square_mass", colnames(square_mass)[3:5])
green_boll_no <- green_boll_no %>% gather("variety", "green_boll_no", colnames(green_boll_no)[3:5])
green_boll_mass <- green_boll_mass %>% gather("variety", "green_boll_mass", colnames(green_boll_mass)[3:5])
open_boll_no <- open_boll_no %>% gather("variety", "open_boll_no", colnames(open_boll_no)[3:5])
open_boll_mass <- open_boll_mass %>% gather("variety", "open_boll_mass", colnames(open_boll_mass)[3:5])
unharvest_boll_no <- unharvest_boll_no %>% gather("variety", "unharvest_boll_no", colnames(unharvest_boll_no)[3:5])
unharvest_boll_mass <- unharvest_boll_mass %>% gather("variety", "unharvest_boll_mass", colnames(unharvest_boll_mass)[3:5])
total_retention <- total_retention %>% gather("variety", "total_retention", colnames(total_retention)[3:5])
leaf_area <- leaf_area %>% gather("variety", "leaf_area", colnames(leaf_area)[3:5])
leaf_mass <- leaf_mass %>% gather("variety", "leaf_mass", colnames(leaf_mass)[3:5])
stem_mass <- stem_mass %>% gather("variety", "stem_mass", colnames(stem_mass)[3:5])
lai <- lai %>% gather("variety", "lai", colnames(lai)[3:5])



plant_2012 <- full_join(square_no, square_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
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


plant_2012 <- plant_2012 %>% mutate(year = "2012")

plant_2012 <- plant_2012 %>% select(year, variety, sowing, date, DAS, everything())




# UNIQUE VARIABLES MEASURED ON RANDOM DATES
# -----------------------------------------


colnames(s1_light_interception)[1] <- "date"
colnames(s2_light_interception)[1] <- "date"

s1_light_interception <- s1_light_interception %>% mutate(sowing = "s1")
s2_light_interception <- s2_light_interception %>% mutate(sowing = "s2")

light_interception <- bind_rows(s1_light_interception, s2_light_interception)

light_interception <- light_interception %>% gather("variety", "light_interception", colnames(light_interception)[3:5])

light_interception <- light_interception %>% mutate(year = "2012")

light_interception_2012 <- light_interception %>% select(year, variety, sowing, date, DAS, everything())




colnames(s1_nodes_over_time)[1] <- "date"
colnames(s2_nodes_over_time)[1] <- "date"

s1_nodes_over_time <- s1_nodes_over_time %>% mutate(sowing = "s1")
s2_nodes_over_time <- s2_nodes_over_time %>% mutate(sowing = "s2")

nodes_over_time <- bind_rows(s1_nodes_over_time, s2_nodes_over_time)

nodes_over_time <- nodes_over_time %>% gather("variety", "nodes_over_time", colnames(nodes_over_time)[3:5])

nodes_over_time <- nodes_over_time %>% mutate(year = "2012")

nodes_over_time_2012 <- nodes_over_time %>% select(year, variety, sowing, date, DAS, everything())




# BOLLS (Open Boll % and Mean Boll Weight)
# ----------------------------------------


colnames(s1_open_boll_percent)[1] <- "date"
colnames(s2_open_boll_percent)[1] <- "date"

s1_open_boll_percent <- s1_open_boll_percent %>% mutate(sowing = "s1")
s2_open_boll_percent <- s2_open_boll_percent %>% mutate(sowing = "s2")

open_boll_percent <- bind_rows(s1_open_boll_percent, s2_open_boll_percent)

open_boll_percent <- open_boll_percent %>% gather("variety", "open_boll_percent", colnames(open_boll_percent)[3:5])



colnames(s1_mean_boll_weight)[1] <- "date"
colnames(s2_mean_boll_weight)[1] <- "date"

s1_mean_boll_weight <- s1_mean_boll_weight %>% mutate(sowing = "s1")
s2_mean_boll_weight <- s2_mean_boll_weight %>% mutate(sowing = "s2")

mean_boll_weight <- bind_rows(s1_mean_boll_weight, s2_mean_boll_weight)

mean_boll_weight <- mean_boll_weight %>% gather("variety", "mean_boll_weight", colnames(mean_boll_weight)[3:5])



bolls <- full_join(open_boll_percent, mean_boll_weight, by = c("sowing", "variety", "date", "DAS"))

bolls <- bolls %>% mutate(year = "2012")

bolls_2012 <- bolls %>% select(year, variety, sowing, date, DAS, everything())

