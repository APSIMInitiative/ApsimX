#' Save Data Frame to CSV for APSIM-X
#'
#' @description
#' This function saves a processed data frame as a CSV file. It ensures the 
#' destination directory exists before writing and returns the file path 
#' for compatibility with targets' file-tracking system.
#'
#' @param df A data frame to be exported.
#' @param folder String. The directory path where the file should be saved.
#' @param filename String. The name of the file (e.g., "Observed_Data.csv").
#'
#' @return String. The full path to the saved CSV file.
#' 
#' @export
save_df_into_csv <- function(df, folder, filename) {
  # 1. Construct the full output path
  out_path <- file.path(folder, filename)
  
  # 2. Ensure the directory exists
  if (!dir.exists(folder)) {
    dir.create(folder, recursive = TRUE)
  }
  
  # 3. Write the CSV
  # na = "" ensures missing values are blank, which APSIM often prefers
  readr::write_csv(df, file = out_path, na = "")
  
  # 4. Return the path so targets can track the file
  return(out_path)
}