# R/functions.R

#' Saves a data frame as a CSV file in the specified folder, converting 
#' Date columns to the DD-MMM-YYYY string format required by APSIM.
#'
#' @param df_input The data frame containing the APSIM stage input parameters.
#' @param folder_path The directory where the CSV file should be saved.
#' @param file_name_saved The desired name for the output CSV file (e.g., "pheno_input.csv").
#' @return The full path to the saved CSV file (required by targets).
saveInputParam <- function(df_input, folder_path, file_name_saved) {
  
  # Load necessary libraries (Ensure these are in your tar_option_set)
  require(readr)
  require(dplyr)
  
  # 1. Date Conversion Step
  # Identify columns that are of class 'Date' or 'POSIXct'
  date_cols <- df_input %>%
    dplyr::select(where(lubridate::is.Date) | where(lubridate::is.POSIXct)) %>%
    names()
  
  if (length(date_cols) > 0) {
    # Convert all identified date columns to the required character format: 01-May-2024
    df_input_formatted <- df_input %>%
      dplyr::mutate(
        dplyr::across(
          .cols = dplyr::all_of(date_cols),
          .fns = ~format(.x, format = "%d-%b-%Y")
        )
      )
  } else {
    df_input_formatted <- df_input
  }
  
  # 2. Define the complete file path using the input file_name_saved
  full_path <- file.path(folder_path, file_name_saved)
  full_path <- normalizePath(full_path, winslash = "/", mustWork = FALSE)
  message("DEBUG: The full path is currently evaluating to: ", full_path)
  
  # Ensure the directory exists before attempting to save
  if (!dir.exists(folder_path)) {
    dir.create(folder_path, recursive = TRUE)
  }
  
  # 3. Write the CSV file
  readr::write_csv(
    x = df_input_formatted, 
    file = full_path,
    na = "",        # Ensure NA values are represented by empty strings
    append = FALSE
  )
  
  # 4. Return the file path so targets knows where the side effect happened.
  return(full_path)
}