library(readxl)
library(dplyr)

read_excel_observed <- function(path) {
  # Read the data
  df <- read_xlsx(path)
  
  # Apply type conversions:
  # 1. Convert all character columns to factors
  # 2. Ensure numbers are numeric (read_xlsx usually does this, 
  #    but we can force it for safety)
  df <- df %>%
    mutate(
      across(where(is.character), as.factor),
      across(where(is.numeric), as.numeric)
    ) %>%
    mutate(Clock.Today = lubridate::as_date(Clock.Today))
    
  
  return(df)
}