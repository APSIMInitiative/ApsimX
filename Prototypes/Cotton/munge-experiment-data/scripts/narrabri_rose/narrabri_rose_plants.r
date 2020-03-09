#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


# nb. years are refered to as the sowing year eg. 2009/2010 year is refered to as 2009.


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Narrabri(Rose)","Dynamic Deficit studies","trial data")


# 2009 Biomass
# ------------

path <- file.path(dir_sourcedata, "0910 A2 DD Biomass Means.xlsx")
biomass <- read_xlsx(path = path, sheet = "Sheet1", range = "A1:J93")
biomass_longer <- biomass %>% pivot_longer(cols = 3:10, names_to = "treatment", values_to = "values") 
biomass_wider <- biomass_longer %>% pivot_wider(names_from = Variate, values_from = values)
biomass_2009 <- biomass_wider %>% mutate(year = "2009") %>% select(year, DAS, everything())


# 2010 Biomass
# ------------

path <- file.path(dir_sourcedata, "1011 A2 DD Biomass Means.xlsx")
biomass <- read_xlsx(path = path, sheet = "Sheet1", range = "A1:J73")
biomass_longer <- biomass %>% pivot_longer(cols = 3:10, names_to = "treatment", values_to = "values") 
biomass_wider <- biomass_longer %>% pivot_wider(names_from = Variate, values_from = values)
biomass_2010 <- biomass_wider %>% mutate(year = "2010") %>% select(year, DAS, everything())


# 2009 First Square & First Flower
# --------------------------------

path <- file.path(dir_sourcedata, "0910 A2 DD First Square & First Flower Means.xlsx")
firstsquare <- read_xlsx(path = path, sheet = "First Square", range = "A1:I2") %>% mutate(year = 2009) %>% mutate(variable = "firstsquare_DAS_50pc")
firstflower <- read_xlsx(path = path, sheet = "First Flower", range = "A1:I2") %>% mutate(year = 2009) %>% mutate(variable = "firstflower_DAS_50pc")
phenology <- bind_rows(firstsquare, firstflower) %>% select(-Variate) %>% select(year, variable, everything())
phenology_longer <- phenology %>% pivot_longer(c(3:10), names_to = "treatment", values_to = "values")
phenology_wider <- phenology_longer %>% pivot_wider(names_from = variable, values_from = values)
phenology_2009 <- phenology_wider



# 2010 First Square & First Flower
# --------------------------------

path <- file.path(dir_sourcedata, "1011 A2 DD First Square & First Flower Means.xlsx")
firstsquare <- read_xlsx(path = path, sheet = "1st Square", range = "A1:I2") %>% mutate(year = 2010) %>% mutate(variable = "firstsquare_DAS_50pc")
firstflower <- read_xlsx(path = path, sheet = "1st Flower", range = "A1:I2") %>% mutate(year = 2010) %>% mutate(variable = "firstflower_DAS_50pc")
phenology <- bind_rows(firstsquare, firstflower) %>% select(-Variate) %>% select(year, variable, everything())
phenology_longer <- phenology %>% pivot_longer(c(3:10), names_to = "treatment", values_to = "values")
phenology_wider <- phenology_longer %>% pivot_wider(names_from = variable, values_from = values)
phenology_2010 <- phenology_wider



# COMBINE YEARS
# -------------

biomass <- bind_rows(biomass_2009, biomass_2010)
phenology <- bind_rows(phenology_2009, phenology_2010)






