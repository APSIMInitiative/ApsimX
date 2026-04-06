#' Spread Phenology Data and Save to CSV
#'
#' Filters for the required columns, formats dates to dd-mmm-yyyy, pivots
#' the APSIM parameters to wide format, and exports to a quote-free CSV.
#'
#' @param folder_path Character. The directory to save the output CSV.
#' @param file_name Character. The name of the output CSV file.
#' @param df_pheno_interp Dataframe. The long-format interpolated phenology data.
#' @return Character. The full path to the saved CSV file.
#' @export
spread_and_csv_save <- function(folder_path, file_name, df_pheno_interp) {
  
  # Ensure the output directory exists before attempting to write
  dir.create(folder_path, showWarnings = FALSE, recursive = TRUE)
  full_path <- file.path(folder_path, file_name)
  
  df_wide <- df_pheno_interp %>%
    # Isolate the exact three columns needed
    dplyr::select(SimulationName, ParamName, Clock.Today) %>%
    
    # Format the date into the required APSIM string format
    dplyr::mutate(Clock.Today = format(Clock.Today, "%d-%b-%Y")) %>%
    
    # Pivot wider so ParamNames become the column headers
    tidyr::pivot_wider(
      names_from = ParamName,
      values_from = Clock.Today
    )
  
  # Save to CSV without quotes, preventing NA from printing as "NA"
  write.csv(df_wide, file = full_path, row.names = FALSE, quote = FALSE, na = "")
  
  # Return the file path so {targets} can track changes to the output file
  return(full_path)
}