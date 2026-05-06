#' Save Dataframe to CSV (APSIM Optimized)
#'
#' @description
#' A generic utility function that ensures the target directory exists, 
#' writes a dataframe to a quote-free CSV, and returns the absolute path 
#' for {targets} file tracking.
#'
#' @details
#' APSIM often fails to read CSVs if strings are wrapped in quotes. This function 
#' forces `quote = FALSE` and ensures missing values are written as blank spaces 
#' (`na = ""`) rather than the string "NA".
#'
#' @param df Data frame to be saved.
#' @param folder Character. The directory to save the output CSV.
#' @param filename Character. The name of the output CSV file.
#'
#' @return Character. The full path to the saved CSV file.
#'
#' @export
save_df_into_csv <- function(df, folder, filename) {
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!is.data.frame(df)) {
    stop("CRITICAL: Input 'df' must be a dataframe.")
  }
  
  # ------------------------------------------------------------------
  # 2. FILE PATH PREPARATION
  # ------------------------------------------------------------------
  # Ensure the output directory exists before attempting to write
  dir.create(folder, showWarnings = FALSE, recursive = TRUE)
  full_path <- file.path(folder, filename)
  
  # ------------------------------------------------------------------
  # 3. WRITE TO CSV
  # ------------------------------------------------------------------
  # Save to CSV without quotes, preventing NA from printing as "NA"
  write.csv(df, file = full_path, row.names = FALSE, quote = FALSE, na = "")
  
  message(sprintf("Successfully saved CSV to: %s", full_path))
  
  # Return the file path so {targets} can track changes to the output file
  return(full_path)
}