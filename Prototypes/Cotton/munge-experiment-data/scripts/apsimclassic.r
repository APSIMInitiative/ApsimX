#! /bin/bash


library(tidyverse)

#writes .xlsx files. (you need to install the package from CRAN)
#https://cran.r-project.org/web/packages/writexl/writexl.pdf
# nb. You can use "readxl" to read .xlsx files (which is built into tidyverse, no installing needed)
library(writexl)         



dir_sourcedata <- file.path("C:","Users","ver078","Dropbox","CottonModel","OldData","APSIMClassicValidation")

path_myall <- file.path(dir_sourcedata, "MyallValeObservedData.csv")
path_gac <- file.path(dir_sourcedata, "GACObservedData.csv")



myall <- read_csv(path_myall)
gac <- read_csv(path_gac)

typeof(myall)
typeof(gac)

sheets <- list(MyallVale = myall, GAC = gac)  

write_xlsx(x = sheets, path = "./output/APSIMClassicValidation.xlsx")

