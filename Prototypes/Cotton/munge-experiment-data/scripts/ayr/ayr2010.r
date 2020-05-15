#! /bin/bash


library(tidyverse)

library(readxl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Ayr","Observed")
path <- file.path(dir_sourcedata, "Burdekin Data for OZCOT.xls")


#nb. s1, s2_s3 refers to the different sowing dates


# Phenology 
# ---------
s1_table1 <- read_xls(path, sheet = "2010", range = "A21:Q25")
s2_table1 <- read_xls(path, sheet = "2010", range = "A38:Q42")
s3_table1 <- read_xls(path, sheet = "2010", range = "A55:Q59")

s1_table1 <- s1_table1 %>% mutate(sowing = "s1")
s2_table1 <- s2_table1 %>% mutate(sowing = "s2")
s3_table1 <- s3_table1 %>% mutate(sowing = "s3")

table1 <- bind_rows(s1_table1, s2_table1, s3_table1)
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

phenology <- phenology %>% mutate(year ="2010")
phenology_2010 <- phenology %>% select(year, variety, sowing, event_date, DAS, event_name) 



# Harvest Results
# ---------------

#filter out phenology columns
harvest <- table1 %>%  select(variety, sowing, colnames(table1)[9:14])
harvest <- harvest %>% rename(date = `Harvest Maturity`)
harvest <- harvest %>% mutate(year = "2010")
harvest_2010 <- harvest %>% select(year, variety, sowing, date, everything())



# Weather
# -------
weather <- read_xls(path, sheet = "2010", range = "A67:E310")



# Plant Information
# -----------------

# first section

s1_square_no <- read_xls(path, sheet = "2010", range = "S7:X14") #missing a row
s2_square_no <- read_xls(path, sheet = "2010", range = "S18:X27")
s3_square_no <- read_xls(path, sheet = "2010", range = "S30:X38")

s1_square_mass <- read_xls(path, sheet = "2010", range = "Z7:AE15")
s2_square_mass <- read_xls(path, sheet = "2010", range = "Z18:AE27")
s3_square_mass <- read_xls(path, sheet = "2010", range = "Z30:AE38")

s1_green_boll_no <- read_xls(path, sheet = "2010", range = "AH7:AM15")
s2_green_boll_no <- read_xls(path, sheet = "2010", range = "AH18:AM27")
s3_green_boll_no <- read_xls(path, sheet = "2010", range = "AH30:AM38")

s1_green_boll_mass <- read_xls(path, sheet = "2010", range = "AP7:AU15")
s2_green_boll_mass <- read_xls(path, sheet = "2010", range = "AP18:AU27")
s3_green_boll_mass <- read_xls(path, sheet = "2010", range = "AP30:AU38")

s1_open_boll_no <- read_xls(path, sheet = "2010", range = "AX7:BC15")
s2_open_boll_no <- read_xls(path, sheet = "2010", range = "AX18:BC27")
s3_open_boll_no <- read_xls(path, sheet = "2010", range = "AX30:BC38")

s1_open_boll_mass <- read_xls(path, sheet = "2010", range = "BF7:BK15")
s2_open_boll_mass <- read_xls(path, sheet = "2010", range = "BF18:BK27")
s3_open_boll_mass <- read_xls(path, sheet = "2010", range = "BF30:BK38")

s1_unharvest_boll_no <- read_xls(path, sheet = "2010", range = "BN7:BS15")
s2_unharvest_boll_no <- read_xls(path, sheet = "2010", range = "BN18:BS27")
s3_unharvest_boll_no <- read_xls(path, sheet = "2010", range = "BN30:BS38")

s1_unharvest_boll_mass <- read_xls(path, sheet = "2010", range = "BV7:CA15")
s2_unharvest_boll_mass <- read_xls(path, sheet = "2010", range = "BV18:CA27")
s3_unharvest_boll_mass <- read_xls(path, sheet = "2010", range = "BV30:CA38")

s1_total_retention <- read_xls(path, sheet = "2010", range = "CD7:CI15")
s2_total_retention <- read_xls(path, sheet = "2010", range = "CD18:CI27")
s3_total_retention <- read_xls(path, sheet = "2010", range = "CD30:CI38")


# below first section

s1_leaf_area <- read_xls(path, sheet = "2010", range = "S46:X54")
s2_leaf_area <- read_xls(path, sheet = "2010", range = "S60:X69")
s3_leaf_area <- read_xls(path, sheet = "2010", range = "S72:X80")

s1_leaf_mass <- read_xls(path, sheet = "2010", range = "Z46:AE54")
s2_leaf_mass <- read_xls(path, sheet = "2010", range = "Z60:AE69")
s3_leaf_mass <- read_xls(path, sheet = "2010", range = "Z72:AE80")

s1_stem_mass <- read_xls(path, sheet = "2010", range = "AH46:AM54")
s2_stem_mass <- read_xls(path, sheet = "2010", range = "AH60:AM69")
s3_stem_mass <- read_xls(path, sheet = "2010", range = "AH72:AM80")

# this table has problems with its dates column. Also s2 is missing a date.
s1_lai <- read_xls(path, sheet = "2010", range = "AP46:AU54")
s2_lai <- read_xls(path, sheet = "2010", range = "AP60:AU68")
s3_lai <- read_xls(path, sheet = "2010", range = "AP72:AU80")


# DO NOT JOIN THESE

s1_light_interception <- read_xls(path, sheet = "2010", range = "AX46:BC50")
s2_light_interception <- read_xls(path, sheet = "2010", range = "AX60:BC65")
s3_light_interception <- read_xls(path, sheet = "2010", range = "AX72:BC76")

#this isn't in 2008 dataset
s1_open_boll_percent <- read_xls(path, sheet = "2010", range = "BF46:BK55")
s2_open_boll_percent <- read_xls(path, sheet = "2010", range = "BF59:BK65")
s3_open_boll_percent <- read_xls(path, sheet = "2010", range = "BF72:BK77")

#this isn't in 2008 dataset
s1_mean_boll_weight <- read_xls(path, sheet = "2010", range = "BN46:BS55")
s2_mean_boll_weight <- read_xls(path, sheet = "2010", range = "BN59:BS65")
s3_mean_boll_weight <- read_xls(path, sheet = "2010", range = "BN72:BS77")

s1_nodes_over_time <- read_xls(path, sheet = "2010", range = "BV46:CA58")
s2_nodes_over_time <- read_xls(path, sheet = "2010", range = "BV61:CA75")
s3_nodes_over_time <- read_xls(path, sheet = "2010", range = "BV78:CA90")





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


#first row for every variable in date column has a bad date, 
#the year is set to 1009 instead of 2009.
#this then buggers up the format of the column that then has problems down the line.

s1_square_mass[1] <- s1_green_boll_no[1]
s1_green_boll_mass[1] <- s1_green_boll_no[1]
s1_open_boll_no[1] <- s1_green_boll_no[1]
s1_open_boll_mass[1] <- s1_green_boll_no[1]
s1_unharvest_boll_no[1] <- s1_green_boll_no[1]
s1_unharvest_boll_mass[1] <- s1_green_boll_no[1]
s1_total_retention[1] <- s1_green_boll_no[1]
s1_leaf_area[1] <- s1_green_boll_no[1]
s1_leaf_mass[1] <- s1_green_boll_no[1]
s1_stem_mass[1] <- s1_green_boll_no[1]

#LAI dates are set to 1900
s1_lai[1] <- s1_green_boll_no[1]
s2_lai[1] <- s2_stem_mass[1] %>% slice(1:8)
s3_lai[1] <- s3_stem_mass[1]


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



plant_2010 <- full_join(square_no, square_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
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


plant_2010 <- plant_2010 %>% mutate(year = "2010")

plant_2010 <- plant_2010 %>% select(year, variety, sowing, date, DAS, everything())




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

light_interception <- light_interception %>% mutate(year = "2010")

light_interception_2010 <- light_interception %>% select(year, variety, sowing, date, DAS, everything())




colnames(s1_nodes_over_time)[1] <- "date"
colnames(s2_nodes_over_time)[1] <- "date"
colnames(s3_nodes_over_time)[1] <- "date"

s1_nodes_over_time <- s1_nodes_over_time %>% mutate(sowing = "s1")
s2_nodes_over_time <- s2_nodes_over_time %>% mutate(sowing = "s2")
s3_nodes_over_time <- s3_nodes_over_time %>% mutate(sowing = "s3")

nodes_over_time <- bind_rows(s1_nodes_over_time, s2_nodes_over_time, s3_nodes_over_time)

nodes_over_time <- nodes_over_time %>% gather("variety", "nodes_over_time", colnames(nodes_over_time)[3:6])

nodes_over_time <- nodes_over_time %>% mutate(year = "2010")

nodes_over_time_2010 <- nodes_over_time %>% select(year, variety, sowing, date, DAS, everything())




# BOLLS (Open Boll % and Mean Boll Weight)
# ----------------------------------------


colnames(s1_open_boll_percent)[1] <- "date"
colnames(s2_open_boll_percent)[1] <- "date"
colnames(s3_open_boll_percent)[1] <- "date"

s1_open_boll_percent <- s1_open_boll_percent %>% mutate(sowing = "s1")
s2_open_boll_percent <- s2_open_boll_percent %>% mutate(sowing = "s2")
s3_open_boll_percent <- s3_open_boll_percent %>% mutate(sowing = "s3")

open_boll_percent <- bind_rows(s1_open_boll_percent, s2_open_boll_percent, s3_open_boll_percent)

open_boll_percent <- open_boll_percent %>% gather("variety", "open_boll_percent", colnames(open_boll_percent)[3:6])



colnames(s1_mean_boll_weight)[1] <- "date"
colnames(s2_mean_boll_weight)[1] <- "date"
colnames(s3_mean_boll_weight)[1] <- "date"

s1_mean_boll_weight <- s1_mean_boll_weight %>% mutate(sowing = "s1")
s2_mean_boll_weight <- s2_mean_boll_weight %>% mutate(sowing = "s2")
s3_mean_boll_weight <- s3_mean_boll_weight %>% mutate(sowing = "s3")

mean_boll_weight <- bind_rows(s1_mean_boll_weight, s2_mean_boll_weight, s3_mean_boll_weight)

mean_boll_weight <- mean_boll_weight %>% gather("variety", "mean_boll_weight", colnames(mean_boll_weight)[3:6])



bolls <- full_join(open_boll_percent, mean_boll_weight, by = c("sowing", "variety", "date", "DAS"))

bolls <- bolls %>% mutate(year = "2010")

bolls_2010 <- bolls %>% select(year, variety, sowing, date, DAS, everything())

