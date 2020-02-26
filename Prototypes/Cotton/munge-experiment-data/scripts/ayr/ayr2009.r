#! /bin/bash


library(tidyverse)

library(readxl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Ayr","Observed")
path <- file.path(dir_sourcedata, "Burdekin Data for OZCOT.xls")


#nb. s1, s2_s3 refers to the different sowing dates


# Phenology 
# ---------

s1_table1 <- read_xls(path, sheet = "2009", range = "A21:Q25")
s2_table1 <- read_xls(path, sheet = "2009", range = "A38:Q42")
s3_table1 <- read_xls(path, sheet = "2009", range = "A55:Q59")

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

phenology <- phenology %>% mutate(year ="2009")
phenology_2009 <- phenology %>% select(year, variety, sowing, event_date, DAS, event_name) 



# Harvest Results
# ---------------

#filter out phenology columns
harvest <- table1 %>%  select(variety, sowing, colnames(table1)[9:14])
harvest <- harvest %>% rename(date = `Harvest Maturity`)
harvest <- harvest %>% mutate(year = "2009")
harvest_2009 <- harvest %>% select(year, variety, sowing, date, everything())



# Weather
# -------

weather <- read_xls(path, sheet = "2009", range = "A67:E310")



# Plant Information
# -----------------

# first section

s1_square_no <- read_xls(path, sheet = "2009", range = "T19:Y27")
s2_square_no <- read_xls(path, sheet = "2009", range = "T30:Y39")
s3_square_no <- read_xls(path, sheet = "2009", range = "T42:Y51")

s1_square_mass <- read_xls(path, sheet = "2009", range = "AB19:AG27")
s2_square_mass <- read_xls(path, sheet = "2009", range = "AB30:AG39")
s3_square_mass <- read_xls(path, sheet = "2009", range = "AB42:AG51")

s1_green_boll_no <- read_xls(path, sheet = "2009", range = "AJ19:AO27")
s2_green_boll_no <- read_xls(path, sheet = "2009", range = "AJ30:AO39")
s3_green_boll_no <- read_xls(path, sheet = "2009", range = "AJ42:AO51")

s1_green_boll_mass <- read_xls(path, sheet = "2009", range = "AR19:AW27")
s2_green_boll_mass <- read_xls(path, sheet = "2009", range = "AR30:AW39")
s3_green_boll_mass <- read_xls(path, sheet = "2009", range = "AR42:AW51")

s1_open_boll_no <- read_xls(path, sheet = "2009", range = "AZ19:BE27")
s2_open_boll_no <- read_xls(path, sheet = "2009", range = "AZ30:BE39")
s3_open_boll_no <- read_xls(path, sheet = "2009", range = "AZ42:BE51")

s1_open_boll_mass <- read_xls(path, sheet = "2009", range = "BH19:BM27")
s2_open_boll_mass <- read_xls(path, sheet = "2009", range = "BH30:BM39")
s3_open_boll_mass <- read_xls(path, sheet = "2009", range = "BH42:BM51")

s1_unharvest_boll_no <- read_xls(path, sheet = "2009", range = "BP19:BU27")
s2_unharvest_boll_no <- read_xls(path, sheet = "2009", range = "BP30:BU39")
s3_unharvest_boll_no <- read_xls(path, sheet = "2009", range = "BP42:BU51")

s1_unharvest_boll_mass <- read_xls(path, sheet = "2009", range = "BX19:CC27")
s2_unharvest_boll_mass <- read_xls(path, sheet = "2009", range = "BX30:CC39")
s3_unharvest_boll_mass <- read_xls(path, sheet = "2009", range = "BX42:CC51")

s1_total_retention <- read_xls(path, sheet = "2009", range = "CF19:CK27")
s2_total_retention <- read_xls(path, sheet = "2009", range = "CF30:CK39")
s3_total_retention <- read_xls(path, sheet = "2009", range = "CF42:CK51")

#this isn't in 2008 dataset
s1_total_reprod_biom <- read_xls(path, sheet = "2009", range = "CN19:CS27")
s2_total_reprod_biom <- read_xls(path, sheet = "2009", range = "CN30:CS39")
s3_total_reprod_biom <- read_xls(path, sheet = "2009", range = "CN42:CS51")


# below first section

s1_leaf_area <- read_xls(path, sheet = "2009", range = "T60:Y68")
s2_leaf_area <- read_xls(path, sheet = "2009", range = "T71:Y80")
s3_leaf_area <- read_xls(path, sheet = "2009", range = "T83:Y92")

s1_leaf_mass <- read_xls(path, sheet = "2009", range = "AB60:AG68")
s2_leaf_mass <- read_xls(path, sheet = "2009", range = "AB71:AG80")
s3_leaf_mass <- read_xls(path, sheet = "2009", range = "AB83:AG92")

s1_stem_mass <- read_xls(path, sheet = "2009", range = "AJ60:AO68")
s2_stem_mass <- read_xls(path, sheet = "2009", range = "AJ71:AO80")
s3_stem_mass <- read_xls(path, sheet = "2009", range = "AJ83:AO92")

s1_lai <- read_xls(path, sheet = "2009", range = "AR60:AW68")
s2_lai <- read_xls(path, sheet = "2009", range = "AR71:AW80")
s3_lai <- read_xls(path, sheet = "2009", range = "AR83:AW92")


# DO NOT JOIN THESE

s1_light_interception <- read_xls(path, sheet = "2009", range = "AZ60:BE65")
s2_light_interception <- read_xls(path, sheet = "2009", range = "AZ71:BE76")
s3_light_interception <- read_xls(path, sheet = "2009", range = "AZ83:BE88")

#this isn't in 2008 dataset
s1_open_boll_percent <- read_xls(path, sheet = "2009", range = "BH60:BM69")
s2_open_boll_percent <- read_xls(path, sheet = "2009", range = "BH71:BM79")
s3_open_boll_percent <- read_xls(path, sheet = "2009", range = "BH83:BM90")

#this isn't in 2008 dataset
s1_mean_boll_weight <- read_xls(path, sheet = "2009", range = "BP60:BU69")
s2_mean_boll_weight <- read_xls(path, sheet = "2009", range = "BP72:BU80")
s3_mean_boll_weight <- read_xls(path, sheet = "2009", range = "BP84:BU91")

s1_nodes_over_time <- read_xls(path, sheet = "2009", range = "BX60:CC74")
s2_nodes_over_time <- read_xls(path, sheet = "2009", range = "BX77:CC90")
s3_nodes_over_time <- read_xls(path, sheet = "2009", range = "BX93:CC105")





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

#Total Reproductive Biomass "DAS" column has dates instead of integers
s1_total_reprod_biom["DAS"] <- s1_total_retention["DAS"]
s2_total_reprod_biom["DAS"] <- s2_total_retention["DAS"]
s3_total_reprod_biom["DAS"] <- s3_total_retention["DAS"]


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



plant_2009 <- full_join(square_no, square_mass, by = c("sowing", "variety", "date", "DAS")) %>% 
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


plant_2009 <- plant_2009 %>% mutate(year = "2009")

plant_2009 <- plant_2009 %>% select(year, variety, sowing, date, DAS, everything())





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

light_interception <- light_interception %>% mutate(year = "2009")

light_interception_2009 <- light_interception %>% select(year, variety, sowing, date, DAS, everything())



colnames(s1_nodes_over_time)[1] <- "date"
colnames(s2_nodes_over_time)[1] <- "date"
colnames(s3_nodes_over_time)[1] <- "date"

s1_nodes_over_time <- s1_nodes_over_time %>% mutate(sowing = "s1")
s2_nodes_over_time <- s2_nodes_over_time %>% mutate(sowing = "s2")
s3_nodes_over_time <- s3_nodes_over_time %>% mutate(sowing = "s3")

nodes_over_time <- bind_rows(s1_nodes_over_time, s2_nodes_over_time, s3_nodes_over_time)

nodes_over_time <- nodes_over_time %>% gather("variety", "nodes_over_time", colnames(nodes_over_time)[3:6])

nodes_over_time <- nodes_over_time %>% mutate(year = "2009")

nodes_over_time_2009 <- nodes_over_time %>% select(year, variety, sowing, date, DAS, everything())




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

bolls <- bolls %>% mutate(year = "2009")

bolls_2009 <- bolls %>% select(year, variety, sowing, date, DAS, everything())

 












