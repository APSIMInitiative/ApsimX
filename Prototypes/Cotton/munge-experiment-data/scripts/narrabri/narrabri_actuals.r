#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Narrabri(Stephen)")
path <- file.path(dir_sourcedata, "Deficit Trials - Output Summary.xls")




# Get Input Data from Spreadsheet
# -------------------------------

lai_2006 <- read_xls(path, sheet = "Actuals 0607", range = "D4:K8")
bolls_2006 <- read_xls(path, sheet = "Actuals 0607", range = "D12:K16") #actually "fruit count" but I interperate this as boll no.
yield_2006 <- read_xls(path, sheet = "Actuals 0607", range = "D27:E31")
sites_2006 <- read_xls(path, sheet = "Actuals 0607", range = "P4:T8")


lai_2007 <- read_xls(path, sheet = "Actuals 0708", range = "D4:K8")
bolls_2007 <- read_xls(path, sheet = "Actuals 0708", range = "D12:K16") 
squares_2007 <- read_xls(path, sheet = "Actuals 0708", range = "D20:K24") 
yield_2007 <- read_xls(path, sheet = "Actuals 0708", range = "D27:E31")
sites_2007 <- read_xls(path, sheet = "Actuals 0708", range = "P4:U8")


lai_2008 <- read_xls(path, sheet = "Actuals 0809", range = "D4:K8")
bolls_2008 <- read_xls(path, sheet = "Actuals 0809", range = "D12:K16") 
squares_2008 <- read_xls(path, sheet = "Actuals 0809", range = "D20:K24") 
yield_2008 <- read_xls(path, sheet = "Actuals 0809", range = "D27:G31")
sites_2008 <- read_xls(path, sheet = "Actuals 0809", range = "P4:U8")
greenbolls_2008 <- read_xls(path, sheet = "Actuals 0809", range = "O12:U16")
openbolls_2008 <- read_xls(path, sheet = "Actuals 0809", range = "O20:U25")
maturity_2008 <- read_xls(path, sheet = "Actuals 0809", range = "P28:Q33")



# Clean up the input data
# -----------------------

lai_2006 <- lai_2006 %>% select(-2,-3)
lai_2007 <- lai_2007 %>% select(-2,-3)
lai_2008 <- lai_2008 %>% select(-2,-3)

bolls_2006 <- bolls_2006 %>% select(-2,-3)
bolls_2007 <- bolls_2007 %>% select(-2,-3)
bolls_2008 <- bolls_2008 %>% select(-2,-3)

squares_2007 <- squares_2007 %>% select(-2,-3)
squares_2008 <- squares_2008 %>% select(-2,-3)

greenbolls_2008 <- greenbolls_2008 %>% select(-2)
openbolls_2008 <- openbolls_2008 %>% select(-2)




# Munge the Data
# --------------


# 2006
# ----

lai_2006 <- lai_2006 %>% pivot_longer(cols = c("71","86","104","126","161"), names_to = "das", values_to = "lai")
lai_2006 <- lai_2006 %>% pivot_longer(cols = lai, names_to = "variable", values_to = "values")
bolls_2006 <- bolls_2006 %>% pivot_longer(cols = c("71","86","104","126","161"), names_to = "das", values_to = "bolls")
bolls_2006 <- bolls_2006 %>% pivot_longer(cols = bolls, names_to = "variable", values_to = "values")
sites_2006 <- sites_2006 %>% pivot_longer(cols = c("71","86","104","126"), names_to = "das", values_to = "sites")
sites_2006 <- sites_2006 %>% pivot_longer(cols = sites, names_to = "variable", values_to = "values")

plant_2006 <- bind_rows(lai_2006, sites_2006, bolls_2006)
plant_2006 <- plant_2006 %>% pivot_wider(names_from = variable, values_from = values)
plant_2006 <- plant_2006 %>% mutate(date = dmy("10/10/2006") + days(das))
plant_2006 <- plant_2006 %>% mutate(year = 2006) %>% select(year, Treatment, date, das, everything())

yield_2006 <- yield_2006 %>% mutate(date = dmy("10/10/2006") + days(163)) # I have no idea what the harvest date was so I used das=163 because last value in long table in "Actuals 0607" worksheet
yield_2006 <- yield_2006 %>% mutate(year = 2006) %>% select(year, Treatment, everything())


# 2007
# ----

lai_2007 <- lai_2007 %>% pivot_longer(cols = c("79","92","115","135","170"), names_to = "das", values_to = "lai")
lai_2007 <- lai_2007 %>% pivot_longer(cols = lai, names_to = "variable", values_to = "values")
bolls_2007 <- bolls_2007 %>% pivot_longer(cols  = c("79","92","115","135","170"), names_to = "das", values_to = "bolls")
bolls_2007 <- bolls_2007 %>% pivot_longer(cols = bolls, names_to = "variable", values_to = "values")
squares_2007 <- squares_2007 %>% pivot_longer(cols  = c("79","92","115","135","170"), names_to = "das", values_to = "squares")
squares_2007 <- squares_2007 %>% pivot_longer(cols = squares, names_to = "variable", values_to = "values")
sites_2007 <- sites_2007 %>% pivot_longer(cols  = c("79","92","115","135","170"), names_to = "das", values_to = "sites")
sites_2007 <- sites_2007 %>% pivot_longer(cols = sites, names_to = "variable", values_to = "values")

plant_2007 <- bind_rows(lai_2007, sites_2007, squares_2007, bolls_2007)
plant_2007 <- plant_2007 %>% pivot_wider(names_from = variable, values_from = values)
plant_2007 <- plant_2007 %>% mutate(date = dmy("15/10/2007") + days(das))
plant_2007 <- plant_2007 %>% mutate(year = 2007) %>% select(year, Treatment, date, das, everything())


yield_2007 <- yield_2007 %>% mutate(date = dmy("15/10/2007") + days(218)) # I have no idea what the harvest date was so I used das=218 because last value in long table in "Actuals 0607" worksheet
yield_2007 <- yield_2007 %>% mutate(year = 2007) %>% select(year, Treatment, everything())



# 2008
# ----

lai_2008 <- lai_2008 %>% pivot_longer(cols = c("76","90","116","137","165"), names_to = "das", values_to = "lai")
lai_2008 <- lai_2008 %>% pivot_longer(cols = lai, names_to = "variable", values_to = "values")
bolls_2008 <- bolls_2008 %>% pivot_longer(cols  = c("76","90","116","137","165"), names_to = "das", values_to = "bolls")
bolls_2008 <- bolls_2008 %>% pivot_longer(cols = bolls, names_to = "variable", values_to = "values")
squares_2008 <- squares_2008 %>% pivot_longer(cols  = c("76","90","116","137","165"), names_to = "das", values_to = "squares")
squares_2008 <- squares_2008 %>% pivot_longer(cols = squares, names_to = "variable", values_to = "values")
sites_2008 <- sites_2008 %>% pivot_longer(cols  = c("76","90","116","137","165"), names_to = "das", values_to = "sites")
sites_2008 <- sites_2008 %>% pivot_longer(cols = sites, names_to = "variable", values_to = "values")
greenbolls_2008 <- greenbolls_2008 %>% pivot_longer(cols  = c("76","90","116","137","165"), names_to = "das", values_to = "greenbolls")
greenbolls_2008 <- greenbolls_2008 %>% pivot_longer(cols = greenbolls, names_to = "variable", values_to = "values")
openbolls_2008 <- openbolls_2008 %>% pivot_longer(cols  = c("76","90","116","137","165"), names_to = "das", values_to = "openbolls")
openbolls_2008 <- openbolls_2008 %>% pivot_longer(cols = openbolls, names_to = "variable", values_to = "values")

plant_2008 <- bind_rows(lai_2008, sites_2008, squares_2008, bolls_2008, greenbolls_2008, openbolls_2008)
plant_2008 <- plant_2008 %>% pivot_wider(names_from = variable, values_from = values)
plant_2008 <- plant_2008 %>% mutate(date = dmy("15/10/2008") + days(das))
plant_2008 <- plant_2008 %>% mutate(year = 2008) %>% select(year, Treatment, date, das, everything())

yield_2008 <- yield_2008 %>% mutate(date = dmy("15/10/2008") + days(218)) # I have no idea what the harvest date was so I used das=218 because last value in long table in "Actuals 0607" worksheet
yield_2008 <- yield_2008 %>% mutate(year = 2008) %>% select(year, Treatment, everything())


# Combined
# --------

plant <- bind_rows(plant_2006, plant_2007, plant_2008)

yield <- bind_rows(yield_2006, yield_2007, yield_2008)





