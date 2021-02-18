#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


# nb. years are refered to as the sowing year eg. 2009/2010 year is refered to as 2009.


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Narrabri(Rose)","Dynamic Deficit studies","Trials", "A2") 
path <- file.path(dir_sourcedata, "Summary Probe Readings 0910 and 1011.xlsx")


trt1_09 <- read_xlsx(path = path, sheet = "0910 Trt1", range = "B2:E35") %>% mutate(year = 2009, treatment = "Trt1") %>% select(year, treatment, everything())
trt2_09 <- read_xlsx(path = path, sheet = "0910 Trt2", range = "B2:E27") %>% mutate(year = 2009, treatment = "Trt2") %>% select(year, treatment, everything())
trt3_09 <- read_xlsx(path = path, sheet = "0910 Trt3", range = "B2:E28") %>% mutate(year = 2009, treatment = "Trt3") %>% select(year, treatment, everything())
trt4_09 <- read_xlsx(path = path, sheet = "0910 Trt4", range = "B2:E31") %>% mutate(year = 2009, treatment = "Trt4") %>% select(year, treatment, everything())

trt1_10 <- read_xlsx(path = path, sheet = "1011 Trt1", range = "B2:E35") %>% mutate(year = 2010, treatment = "Trt1") %>% select(year, treatment, everything())
trt2_10 <- read_xlsx(path = path, sheet = "1011 Trt2", range = "B2:E25") %>% mutate(year = 2010, treatment = "Trt2") %>% select(year, treatment, everything())
trt3_10 <- read_xlsx(path = path, sheet = "1011 Trt3", range = "B2:E23") %>% mutate(year = 2010, treatment = "Trt3") %>% select(year, treatment, everything())


trt1_09 <- trt1_09 %>% rename(Clock.Today = "0910 Trt1 Cntrl")
trt2_09 <- trt2_09 %>% rename(Clock.Today = "0910 Trt2 Early")
trt3_09 <- trt3_09 %>% rename(Clock.Today = "0910 Trt3 LateFlw")
trt4_09 <- trt4_09 %>% rename(Clock.Today = "0910 Trt4 Dynamic Deficit")

trt1_10 <- trt1_10 %>% rename(Clock.Today = "1011 Trt1 Control")
trt2_10 <- trt2_10 %>% rename(Clock.Today = "1011 Trt2 Early")
trt3_10 <- trt3_10 %>% rename(Clock.Today = "1011 Trt3 LateFlw/Dynamic")


soilwater <- bind_rows(trt1_09, trt2_09, trt3_09, trt4_09, trt1_10, trt2_10, trt3_10)

soilwater$Clock.Today = format(soilwater$Clock.Today, "%d/%m/%Y")

soilwater <- soilwater %>% select(Clock.Today, year, treatment, everything())


