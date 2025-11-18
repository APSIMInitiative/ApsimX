library(readxl)
library(dplyr)

# -------------------------------------------------------------------
# Helper function to read and process one file
# -------------------------------------------------------------------
process_worked_input <- function(filepath) {
  
  # Read sheet WorkedForInput, columns A:D only
  df <- read_excel(
    path = filepath,
    sheet = "WorkedForInput",
    range = cell_cols("A:D"),
    col_names = TRUE,
    guess_max = 1000
  )
  
  # Identify the 3 date columns (modify names if needed)
  date_cols <- c(
    "[Wheat].Phenology.Emerging.DateToProgress",
    "[Wheat].Phenology.StemElongating.DateToProgress",
    "[Wheat].Phenology.Flowering.DateToProgress"
  )
  
  # Convert date-like cells to Date if possible
  df2 <- df %>%
    mutate(across(all_of(date_cols), ~ as.Date(.x)))
  
  # Format dates as dd-mmm-yyyy
  df2 <- df2 %>%
    mutate(across(all_of(date_cols),
                  ~ format(.x, "%d-%b-%Y")))
  
  # Convert first column to factor (if desired)
  df2[[1]] <- as.factor(df2[[1]])
  
  return(df2)
}

# -------------------------------------------------------------------
# Apply function to both files
# -------------------------------------------------------------------

file1 <- "C:/github/ApsimX/Tests/Validation/Wheat/Dookie2024/DookieEVA2024Pivot.xlsx"
file2 <- "C:/github/ApsimX/Tests/Validation/Wheat/Dookie2024/DookieWWHI2024Pivot.xlsx"

df_A <- process_worked_input(file1)
df_B <- process_worked_input(file2)

# -------------------------------------------------------------------
# Bind both datasets (same column structure)
# -------------------------------------------------------------------
df_final <- bind_rows(df_A, df_B)

# -------------------------------------------------------------------
# Save final CSV
# -------------------------------------------------------------------
output_path <- "C:/github/ApsimX/Tests/Validation/Wheat/inputs/DookiePhenoDatesInput_OBS.csv"

write.csv(df_final, output_path, row.names = FALSE, na = "")
