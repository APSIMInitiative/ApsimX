#' Export Selected Observations to CSV
#'
#' @description
#' Subsets a wide observation dataframe to a specific list of variables, 
#' ensures the primary key is included, strips out any "ghost rows", 
#' prints a detailed summary of extracted records and data ranges, 
#' and writes to a CSV.
#'
#' @param df_in Dataframe containing the wide observations.
#' @param file_name_out Character. Path and filename for the output CSV.
#' @param select_vars Character vector of column names to extract.
#' @param primary_key Character. The master key column. Defaults to "SimulationName".
#'
#' @export
print_csv_with_select_obs <- function(df_in, file_name_out, select_vars, primary_key = "SimulationName") {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("readr", quietly = TRUE)) stop("Package 'readr' required.")
  
  # 1. Ensure the primary key actually exists in the data
  if (!primary_key %in% names(df_in)) {
    stop(sprintf("CRITICAL: Primary key '%s' not found in the dataframe.", primary_key), call. = FALSE)
  }
  
  # 2. Combine primary key with requested vars
  target_cols <- unique(trimws(c(primary_key, select_vars)))
  
  # 3. Defensive Check: Identify missing columns
  missing_cols <- setdiff(target_cols, names(df_in))
  if (length(missing_cols) > 0) {
    warning(sprintf("\u26A0\uFE0F Missing requested columns: [%s]", 
                    paste(missing_cols, collapse = ", ")), call. = FALSE)
  }
  
  # 4. Isolate the columns that actually exist
  final_cols <- intersect(target_cols, names(df_in))
  obs_cols <- setdiff(final_cols, primary_key) 
  
  # 5. Subset the dataframe
  df_out <- df_in %>%
    dplyr::select(dplyr::all_of(final_cols))
  
  # 6. THE GHOST ROW ASSASSIN
  if (length(obs_cols) > 0) {
    df_out <- df_out %>%
      dplyr::filter(dplyr::if_any(
        dplyr::all_of(obs_cols), 
        ~ !is.na(.) & trimws(as.character(.)) != ""
      ))
  }
  
  # 7. THE DETAILED SUMMARY LOGGER
  range_logs <- c()
  
  # 7A. Log variables that were completely missing
  for (m_col in missing_cols) {
    range_logs <- c(range_logs, sprintf("   -> '%s': [\u26A0\uFE0F NOT PRESENT IN DATA]", m_col))
  }
  
  # 7B. Evaluate and log variables that exist
  for (col in obs_cols) {
    val_data <- df_out[[col]]
    
    # Filter down to only the valid entries for this specific column to get an accurate count
    valid_data <- val_data[!is.na(val_data) & trimws(as.character(val_data)) != ""]
    n_valid <- length(valid_data)
    
    # Check 1: Is it entirely empty?
    if (n_valid == 0) {
      range_logs <- c(range_logs, sprintf("   -> '%s': [\u26A0\uFE0F VALUES ARE COMPLETELY BLANK/NA]", col))
    } 
    # Check 2: Numeric Data (Print count + Range rounded to 2 decimals)
    else if (is.numeric(valid_data)) {
      min_val <- min(valid_data, na.rm = TRUE)
      max_val <- max(valid_data, na.rm = TRUE)
      range_logs <- c(range_logs, sprintf("   -> '%s': Extracted %d records | Range [%s to %s]", 
                                          col, n_valid, round(min_val, 2), round(max_val, 2)))
    } 
    # Check 3: Text/Categorical Data (Print count + Unique categories)
    else {
      n_unique <- length(unique(valid_data))
      range_logs <- c(range_logs, sprintf("   -> '%s': Extracted %d records | [%d unique categories]", 
                                          col, n_valid, n_unique))
    }
  }
  
  # Compile and print the summary box
  summary_box <- c(
    "",
    "----------------------------------------------------------------------",
    sprintf(" \U0001F4CB EXTRACTION SUMMARY: %s", basename(file_name_out)),
    "----------------------------------------------------------------------",
    range_logs,
    "----------------------------------------------------------------------"
  )
  message(paste(summary_box, collapse = "\n"))
  
  # 8. Ensure the directory exists before saving
  out_dir <- dirname(file_name_out)
  if (!dir.exists(out_dir)) dir.create(out_dir, recursive = TRUE)
  
  # 9. Export cleanly
  readr::write_csv(df_out, file = file_name_out, na = "")
  message(sprintf("💾 Success: Exported %d valid rows and %d columns.", nrow(df_out), length(final_cols)))
  
  # Return invisibly
  return(invisible(file_name_out))
}