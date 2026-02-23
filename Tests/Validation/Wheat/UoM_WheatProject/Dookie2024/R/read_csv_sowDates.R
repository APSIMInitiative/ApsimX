#' Read Sowing Dates CSV and format date columns
#'
#' @param folder_path Character. The base directory path where the raw data is located.
#' @param file_name Character. The name of the CSV file to read.
#' @return A dataframe (tibble) with parsed Date columns.
#' @export
read_csv_sowDates <- function(folder_path, file_name) {
  
  # Construct the full file path
  full_path <- file.path(folder_path, file_name)
  
  # Check if file exists to fail safely
  if (!file.exists(full_path)) {
    stop("CSV file not found: ", full_path)
  }
  
  # Read the CSV (using readr for speed and better tibble formatting)
  df <- readr::read_csv(full_path, show_col_types = FALSE)
  
  # Convert all columns containing "Date" to actual R Date objects
  df <- df %>%
    dplyr::mutate(
      dplyr::across(
        tidyselect::contains("Date"),
        ~ as.Date(.x, format = "%d-%b-%Y")
      )
    )
  
  return(df)
}