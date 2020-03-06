#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Narrabri(Stephen)", "Soil Probe Data")
path <- file.path(dir_sourcedata, "ProbeReading Summary.xls")


#nb. the probes only went down to 120cm. So all the sw measurments are the cumulative sw down to 120cm


# 2006
# ----

blue_date <- read_xls(path, sheet = "2006-07", range = "C6:AJ6", col_names = FALSE)
blue_sw <- read_xls(path, sheet = "2006-07", range = "C7:AJ7", col_names = FALSE)
blue_date <- t(blue_date) %>% as.Date()
blue_sw <- t(blue_sw) 
blue <- tibble(date = blue_date, sw_120 = blue_sw)
# names(blue)[1] <- "date"
# names(blue)[2] <- "sw_120"
blue_2006 <- blue %>% mutate(year = 2006, treatment = "Blue") %>% select(year, treatment, date, sw_120)


red_date <- read_xls(path, sheet = "2006-07", range = "C11:AJ11", col_names = FALSE)
red_sw <- read_xls(path, sheet = "2006-07", range = "C12:AJ12", col_names = FALSE)
red_date <- t(red_date) %>% as.Date()
red_sw <- t(red_sw) 
red <- tibble(date = red_date, sw_120 = red_sw)
# names(red)[1] <- "date"
# names(red)[2] <- "sw_120"
red_2006 <- red %>% mutate(year = 2006, treatment = "Red") %>% select(year, treatment, date, sw_120)


green_date <- read_xls(path, sheet = "2006-07", range = "C16:AC16", col_names = FALSE)
green_sw <- read_xls(path, sheet = "2006-07", range = "C17:AC17", col_names = FALSE)
green_date <- t(green_date) %>% as.Date()
green_sw <- t(green_sw) 
green <- tibble(date = green_date, sw_120 = green_sw)
# names(green)[1] <- "date"
# names(green)[2] <- "sw_120"
green_2006 <- green %>% mutate(year = 2006, treatment = "Green") %>% select(year, treatment, date, sw_120)


grey_date <- read_xls(path, sheet = "2006-07", range = "C21:Z21", col_names = FALSE)
grey_sw <- read_xls(path, sheet = "2006-07", range = "C22:Z22", col_names = FALSE)
grey_date <- t(grey_date) %>% as.Date()
grey_sw <- t(grey_sw) 
grey <- tibble(date = grey_date, sw_120 = grey_sw)
# names(grey)[1] <- "date"
# names(grey)[2] <- "sw_120"
grey_2006 <- grey %>% mutate(year = 2006, treatment = "Grey") %>% select(year, treatment, date, sw_120)




# 2007
# ----

blue_date <- read_xls(path, sheet = "2007-08", range = "C6:AP6", col_names = FALSE)
blue_sw <- read_xls(path, sheet = "2007-08", range = "C7:AP7", col_names = FALSE)
blue_date <- t(blue_date) %>% as.Date()
blue_sw <- t(blue_sw) 
blue <- tibble(date = blue_date, sw_120 = blue_sw)
# names(blue)[1] <- "date"
# names(blue)[2] <- "sw_120"
blue_2007 <- blue %>% mutate(year = 2007, treatment = "Blue") %>% select(year, treatment, date, sw_120)


red_date <- read_xls(path, sheet = "2007-08", range = "C11:AC11", col_names = FALSE)
red_sw <- read_xls(path, sheet = "2007-08", range = "C12:AC12", col_names = FALSE)
red_date <- t(red_date) %>% as.Date()
red_sw <- t(red_sw) 
red <- tibble(date = red_date, sw_120 = red_sw)
# names(red)[1] <- "date"
# names(red)[2] <- "sw_120"
red_2007 <- red %>% mutate(year = 2007, treatment = "Red") %>% select(year, treatment, date, sw_120)


green_date <- read_xls(path, sheet = "2007-08", range = "C16:AB16", col_names = FALSE)
green_sw <- read_xls(path, sheet = "2007-08", range = "C17:AB17", col_names = FALSE)
green_date <- t(green_date) %>% as.Date()
green_sw <- t(green_sw) 
green <- tibble(date = green_date, sw_120 = green_sw)
# names(green)[1] <- "date"
# names(green)[2] <- "sw_120"
green_2007 <- green %>% mutate(year = 2007, treatment = "Green") %>% select(year, treatment, date, sw_120)


grey_date <- read_xls(path, sheet = "2007-08", range = "C21:Y21", col_names = FALSE)
grey_sw <- read_xls(path, sheet = "2007-08", range = "C22:Y22", col_names = FALSE)
grey_date <- t(grey_date) %>% as.Date()
grey_sw <- t(grey_sw) 
grey <- tibble(date = grey_date, sw_120 = grey_sw)
# names(grey)[1] <- "date"
# names(grey)[2] <- "sw_120"
grey_2007 <- grey %>% mutate(year = 2007, treatment = "Grey") %>% select(year, treatment, date, sw_120)




# 2008
# ----

blue_date <- read_xls(path, sheet = "2008-09", range = "C6:AP6", col_names = FALSE)
blue_sw <- read_xls(path, sheet = "2008-09", range = "C7:AP7", col_names = FALSE)
blue_date <- t(blue_date) %>% as.Date()
blue_sw <- t(blue_sw) 
blue <- tibble(date = blue_date, sw_120 = blue_sw)
# names(blue)[1] <- "date"
# names(blue)[2] <- "sw_120"
blue_2008 <- blue %>% mutate(year = 2008, treatment = "Blue") %>% select(year, treatment, date, sw_120)


red_date <- read_xls(path, sheet = "2008-09", range = "C11:AH11", col_names = FALSE)
red_sw <- read_xls(path, sheet = "2008-09", range = "C12:AH12", col_names = FALSE)
red_date <- t(red_date) %>% as.Date()
red_sw <- t(red_sw) 
red <- tibble(date = red_date, sw_120 = red_sw)
# names(red)[1] <- "date"
# names(red)[2] <- "sw_120"
red_2008 <- red %>% mutate(year = 2008, treatment = "Red") %>% select(year, treatment, date, sw_120)


green_date <- read_xls(path, sheet = "2008-09", range = "C16:AI16", col_names = FALSE)
green_sw <- read_xls(path, sheet = "2008-09", range = "C17:AI17", col_names = FALSE)
green_date <- t(green_date) %>% as.Date()
green_sw <- t(green_sw) 
green <- tibble(date = green_date, sw_120 = green_sw)
# names(green)[1] <- "date"
# names(green)[2] <- "sw_120"
green_2008 <- green %>% mutate(year = 2008, treatment = "Green") %>% select(year, treatment, date, sw_120)


grey_date <- read_xls(path, sheet = "2008-09", range = "C21:Z21", col_names = FALSE)
grey_sw <- read_xls(path, sheet = "2008-09", range = "C22:Z22", col_names = FALSE)
grey_date <- t(grey_date) %>% as.Date()
grey_sw <- t(grey_sw) 
grey <- tibble(date = grey_date, sw_120 = grey_sw)
# names(grey)[1] <- "date"
# names(grey)[2] <- "sw_120"
grey_2008 <- grey %>% mutate(year = 2008, treatment = "Grey") %>% select(year, treatment, date, sw_120)



# Combine years
# -------------

blue <- bind_rows(blue_2006, blue_2007, blue_2008)
red <- bind_rows(red_2006, red_2007, red_2008)
green <- bind_rows(green_2006, green_2007, green_2008)
grey <- bind_rows(grey_2006, grey_2007, grey_2008)

soilwater <- bind_rows(blue, red, green, grey)





