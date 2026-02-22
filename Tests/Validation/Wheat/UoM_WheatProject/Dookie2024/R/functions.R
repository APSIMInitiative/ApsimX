#' Process worked input from Excel
#'
#' @param filepath Character. The relative path to the Excel file.
#' @param config List. Contains configuration parameters (sheet, range, date_cols).
#' @return A processed tibble/dataframe.
#' @export
process_worked_input <- function(filepath, config) {
  
  # Read specific columns from the Excel file
  df <- readxl::read_excel(
    path = filepath,
    sheet = config$sheet,
    range = readxl::cell_cols(config$col_range),
    col_names = TRUE,
    guess_max = 1000
  )
  
  # Convert date-like cells to Date, then format as dd-mmm-yyyy
  df2 <- df %>%
    dplyr::mutate(
      dplyr::across(
        dplyr::all_of(config$date_cols),
        ~ format(as.Date(.x), "%d-%b-%Y")
      )
    )
  
  # Convert the first column to a factor
  df2[[1]] <- as.factor(df2[[1]])
  
  return(df2)
}

#' Combine processed dataframes and save to CSV
#'
#' @param df_list List. A list containing the processed dataframes to bind.
#' @param output_path Character. The relative path where the final CSV will be saved.
#' @return Character. The output path (required by targets to track the file).
#' @export
combine_and_save <- function(df_list, output_path) {
  
  # Bind all rows from the provided list of dataframes
  df_final <- dplyr::bind_rows(df_list)
  
  # Ensure the target directory exists before attempting to write
  dir.create(dirname(output_path), showWarnings = FALSE, recursive = TRUE)
  
  # Save the file
  write.csv(df_final, output_path, row.names = FALSE, quote = FALSE)
  
  # Return the file path so `targets` can track changes to the output
  return(output_path)
}