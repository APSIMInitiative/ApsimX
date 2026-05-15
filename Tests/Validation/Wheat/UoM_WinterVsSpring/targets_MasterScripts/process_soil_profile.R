#' Read, Clean, and Average APSIM Soil Profile Data
#'
#' @param folder_name Character. Directory containing the Excel file.
#' @param file_name Character. Name of the Excel file.
#' @param sheet_name Character. Name of the sheet containing soil data.
#' @param var_list Character vector. The exact column names to extract and average.
#' @param rep_name Character. The column name indicating the replicate.
#' @param col_depth_from Character. Exact name of the upper depth column (default: "Depth from").
#' @param col_depth_to Character. Exact name of the lower depth column (default: "Depth To").
#'
#' @return A dataframe summarized by Depth, properly sorted for APSIM.
#' @export
process_soil_profile <- function(folder_name, file_name, sheet_name, var_list, rep_name, 
                                 col_depth_from = "Depth from", col_depth_to = "Depth To") {
  
  require(dplyr)
  require(readxl)
  require(tidyr)
  
  file_path <- file.path(folder_name, file_name)
  if (!file.exists(file_path)) stop(sprintf("CRITICAL: File not found at '%s'", file_path))
  
  # 1. Read all columns as text to trap non-numeric errors before they become silent NAs
  df_raw <- readxl::read_excel(file_path, sheet = sheet_name, col_types = "text")
  
  # Defensive check to ensure required columns actually exist
  req_cols <- c(col_depth_from, col_depth_to, rep_name, var_list)
  missing_cols <- setdiff(req_cols, names(df_raw))
  if (length(missing_cols) > 0) {
    stop(sprintf("CRITICAL: Missing expected columns in Excel: %s", paste(missing_cols, collapse = ", ")))
  }
  
  # 2. Construct Depth and prep hidden sorting column
  df <- df_raw %>%
    dplyr::mutate(
      Depth = paste0(.data[[col_depth_from]], "-", .data[[col_depth_to]]),
      # Create a temporary numeric column just to ensure layers sort chronologically later
      Top_Depth_Sort = as.numeric(.data[[col_depth_from]]) 
    )
  
  # 3. Data Sanitization & Custom Warnings
  warning_logs <- c()
  
  for (v in var_list) {
    raw_vals <- df[[v]]
    # Attempt to convert to numeric. legitimate text becomes NA
    num_vals <- suppressWarnings(as.numeric(raw_vals))
    
    # Identify rows that became NA, but were NOT empty in the raw Excel file
    bad_idx <- which(is.na(num_vals) & !is.na(raw_vals) & trimws(raw_vals) != "")
    
    if (length(bad_idx) > 0) {
      for (i in bad_idx) {
        bad_str <- raw_vals[i]
        bad_dep <- df$Depth[i]
        bad_rep <- df[[rep_name]][i]
        
        warning_logs <- c(
          warning_logs,
          sprintf(" -> Var: '%s' | Depth: %s | Rep: %s | Found: '%s' (Forced to NA)", 
                  v, bad_dep, bad_rep, bad_str)
        )
      }
    }
    # Overwrite the original text column with the cleaned numeric values
    df[[v]] <- num_vals
  }
  
  # Print the consolidated warning block if bad data was found
  if (length(warning_logs) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " ⚠️ NON-NUMERIC SOIL DATA DETECTED ⚠️ ",
      "======================================================================",
      " The following entries could not be processed and were coerced to NA:",
      warning_logs,
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    warning("Non-numeric soil data was coerced to NA. See console for exact locations.", call. = FALSE)
  }
  
  # 4. Average across replicates and format for APSIM
  df_final <- df %>%
    dplyr::group_by(Top_Depth_Sort, Depth) %>%
    dplyr::summarise(
      # Calculate the mean for every variable in the var_list, ignoring the NAs we just made
      dplyr::across(dplyr::all_of(var_list), ~ mean(.x, na.rm = TRUE)),
      .groups = "drop"
    ) %>%
    # Sort strictly by the numeric upper layer so 100-120 doesn't jump above 20-30
    dplyr::arrange(Top_Depth_Sort) %>%
    # Drop the temporary sorting column
    dplyr::select(-Top_Depth_Sort) 
  
  return(df_final)
}