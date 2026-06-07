#' Read, Format, and Average Soil Data
#'
#' @description
#' Reads raw soil data, combines depth columns, safely forces string artifacts 
#' into NAs, averages variables, and strictly orders the profile by increasing depth.
#' 
#' @details
#' **Defensive Column Resolution:** Automatically handles messy Excel formatting, 
#' trailing spaces, and duplicate column names. If a requested column appears multiple 
#' times, the function actively scans them and isolates the FIRST instance that actually 
#' contains valid data (bypassing empty columns or columns containing only a unit row), 
#' logging the intervention.
#'
#' @param folder Character. Path to the folder containing the file.
#' @param file Character. Name of the Excel file.
#' @param sheet Character. Name of the sheet to read.
#' @param vars_to_extract Character vector of column names to process.
#' @param col_depth_from Character. Exact name of the 'Depth From' column.
#' @param col_depth_to Character. Exact name of the 'Depth To' column.
#'
#' @return A clean dataframe sorted top-to-bottom by depth.
#' @export
read_soil_data <- function(folder, file, sheet, vars_to_extract, 
                           col_depth_from = "Depth From", 
                           col_depth_to = "Depth To") {
  
  if (!requireNamespace("readxl", quietly = TRUE)) stop("Package 'readxl' required.")
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("stringr", quietly = TRUE)) stop("Package 'stringr' required.")
  
  file_path <- file.path(folder, file)
  if (!file.exists(file_path)) stop(sprintf("CRITICAL: File not found at '%s'", file_path))
  
  message(sprintf("📖 Reading soil data from: %s (Sheet: %s)", file, sheet))
  
  # 1. Read the raw data
  df_raw <- suppressMessages(readxl::read_excel(file_path, sheet = sheet))
  
  # 2. DEFENSIVE COLUMN RESOLVER
  # ---------------------------------------------------------
  # Strip readxl's duplicate suffixes (e.g., "...3") and trim hidden spaces
  raw_names <- names(df_raw)
  base_names <- stringr::str_replace(raw_names, "\\.\\.\\.[0-9]+$", "")
  base_names <- trimws(base_names)
  
  # The master list of columns we need to find
  target_cols <- c(col_depth_from, col_depth_to, vars_to_extract)
  
  resolved_indices <- integer(length(target_cols))
  missing_cols <- character()
  duplicate_logs <- c()
  
  for (i in seq_along(target_cols)) {
    tgt <- trimws(target_cols[i])
    matches <- which(base_names == tgt)
    
    if (length(matches) == 0) {
      missing_cols <- c(missing_cols, tgt)
      
    } else if (length(matches) == 1) {
      resolved_indices[i] <- matches[1]
      
    } else {
      # THE DUPLICATE SHIELD: Find the first instance that ACTUALLY contains data
      best_match <- matches[1] # Default fallback
      data_found <- FALSE
      
      for (m in matches) {
        col_data <- df_raw[[m]]
        
        # Strip NAs and empty whitespace
        valid_vals <- col_data[!is.na(col_data)]
        valid_vals <- trimws(as.character(valid_vals))
        valid_vals <- valid_vals[valid_vals != ""]
        
        # THE FIX: Require > 1 valid entry to bypass isolated unit strings (e.g., "mg/kg")
        # An empty duplicate with just a unit will have length == 1. 
        # A real column will have length > 1 (Units + Data).
        if (length(valid_vals) > 1) {
          best_match <- m
          data_found <- TRUE
          break # Stop searching once we find the populated bulk column
        }
      }
      
      resolved_indices[i] <- best_match
      
      if (data_found) {
        duplicate_logs <- c(
          duplicate_logs,
          sprintf("   -> Column '%s' found %d times. Safely extracted Index %d (bypassed empty/unit-only duplicates).", 
                  tgt, length(matches), best_match)
        )
      } else {
        duplicate_logs <- c(
          duplicate_logs,
          sprintf("   -> Column '%s' found %d times, but ALL instances appear empty. Defaulted to Index %d.", 
                  tgt, length(matches), best_match)
        )
      }
    }
  }
  
  # ---------------------------------------------------------
  # 3. AUDIT ALARMS
  # ---------------------------------------------------------
  if (length(missing_cols) > 0) {
    stop(sprintf("\n🚨 CRITICAL ERROR: The following requested columns are totally missing from the sheet:\n   -> [%s]\n", 
                 paste(missing_cols, collapse = ", ")), call. = FALSE)
  }
  
  if (length(duplicate_logs) > 0) {
    log_box <- c(
      "",
      "----------------------------------------------------------------------",
      " ⚠️ PIPELINE INTERVENTION: DUPLICATE COLUMNS DETECTED ⚠️",
      "----------------------------------------------------------------------",
      duplicate_logs,
      "----------------------------------------------------------------------",
      ""
    )
    message(paste(log_box, collapse = "\n"))
  }
  
  # 4. ISOLATE AND NORMALIZE
  # Subset only the resolved columns and rename them to strictly match the targets
  df_processed <- df_raw[, resolved_indices]
  names(df_processed) <- target_cols
  
  # 5. Process, Scrub, Average, and SORT
  df_processed <- df_processed %>%
    dplyr::mutate(
      Depth = paste0(.data[[col_depth_from]], "-", .data[[col_depth_to]]),
      
      # BULLETPROOF SORT KEY: Strip all letters/symbols and force to pure number
      SortKey = suppressWarnings(as.numeric(gsub("[^0-9.]", "", .data[[col_depth_from]])))
    ) %>%
    dplyr::mutate(
      dplyr::across(dplyr::all_of(vars_to_extract), ~ suppressWarnings(as.numeric(.)))
    ) %>%
    dplyr::group_by(Depth, SortKey) %>%
    dplyr::summarise(
      dplyr::across(
        dplyr::all_of(vars_to_extract), 
        ~ mean(., na.rm = TRUE)
      ),
      .groups = "drop"
    ) %>%
    # STRICT INCREASING ORDER: Sorts topsoil (e.g., 0) down to subsoil (e.g., 160)
    dplyr::arrange(SortKey) %>%
    dplyr::select(-SortKey)
  
  # 6. Final NA warning
  total_nas <- sum(is.na(df_processed[vars_to_extract]))
  if (total_nas > 0) {
    message(sprintf("⚠️ Note: %d NAs generated/detected in the requested variables.", total_nas))
  }
  
  return(df_processed)
}