#! /bin/bash


library(tidyverse)

library(readxl)
library(writexl)

library(lubridate)


dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","Narrabri(Stephen)")
path <- file.path(dir_sourcedata, "Deficit Trials - Output Summary.xls")



lai_2006 <- read_xls(path, sheet = "Actuals 0607", range = "D4:K8")
boll_no_2006 <- read_xls(path, sheet = "Actuals 0607", range = "D12:K16") #actually "fruit count" but I interperate this as boll no.
yield_2006 <- read_xls(path, sheet = "Actuals 0607", range = "D27:E31")
sites_no_2006 <- read_xls(path, sheet = "Actuals 0607", range = "P4:T8")


lai_2007 <- read_xls(path, sheet = "Actuals 0708", range = "D4:K8")
boll_no_2007 <- read_xls(path, sheet = "Actuals 0708", range = "D12:K16") 
square_no_2007 <- read_xls(path, sheet = "Actuals 0708", range = "D20:K24") 
yield_2007 <- read_xls(path, sheet = "Actuals 0708", range = "D27:E31")
sites_no_2007 <- read_xls(path, sheet = "Actuals 0708", range = "P4:T8")


lai_2008 <- read_xls(path, sheet = "Actuals 0809", range = "D4:K8")
boll_no_2008 <- read_xls(path, sheet = "Actuals 0809", range = "D12:K16") 
square_no_2008 <- read_xls(path, sheet = "Actuals 0809", range = "D20:K24") 
yield_2008 <- read_xls(path, sheet = "Actuals 0809", range = "D27:E31")
sites_no_2008 <- read_xls(path, sheet = "Actuals 0809", range = "P4:T8")
greenboll_no_2008 <- read_xls(path, sheet = "Actuals 0809", range = "P4:T8")
openboll_no_2008 <- read_xls(path, sheet = "Actuals 0809", range = "P4:T8")
maturity_2008 <- read_xls(path, sheet = "Actuals 0809", range = "P4:T8")