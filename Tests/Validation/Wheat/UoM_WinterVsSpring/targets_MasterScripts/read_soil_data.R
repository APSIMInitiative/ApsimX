#' Read, Format, and Average Soil Data
#'
#' @description
#' Reads raw soil data, merges staggered headers, extracts requested variables, 
#' rounds them to 3 decimals, and exports a clean CSV. 
#' 
#' @details
#' **Data Gap Handling:** If requested variables are missing from the file or contain 
#' completely empty data, the script will safely bypass them and trigger a detailed 
#' warning alarm at the end of the run instead of crashing the pipeline.
#'
#' @param folder Character. Path to the folder containing the file.
#' @param file Character. Name of the Excel file.
#' @param sheet Character. Name of the sheet to read.
#' @param vars_to_extract Character vector of column names to process.
#' @param col_depth_from Character. Exact name of the 'Depth From' column.
#' @param col_depth_to Character. Exact name of the 'Depth To' column.
#' @param log_file_name Character. The name of the CSV file to export.
#'
#' @return A clean dataframe sorted top-to-bottom by depth.
#' @export
read_soil_data <- function(folder, file, sheet, vars_to_extract, 
                           col_depth_from = "Depth From", 
                           col_depth_to = "Depth To",
                           log_file_name = "soil_setup_log.csv") {
  
  if (!requireNamespace("readxl", quietly = TRUE)) stop("Package 'readxl' required.")
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("stringr", quietly = TRUE)) stop("Package 'stringr' required.")
  
  # DEFENSIVE SHIELD: Ensure only a single file is processed
  if (length(file) > 1) {
    warning(sprintf("\u26A0\uFE0F Multiple files passed to read_soil_data. Safely defaulting to the first file: '%s'", file[1]), call. = FALSE)
    file <- file[1]
  }
  
  file_path <- file.path(folder, file)
  if (!file.exists(file_path)) stop(sprintf("CRITICAL: File not found at '%s'", file_path))
  
  message(sprintf("📖 Reading soil data from: %s (Sheet: %s)", file, sheet))
  
  # ---------------------------------------------------------
  # 1. READ BLIND & COALESCE STAGGERED HEADERS
  # ---------------------------------------------------------
  df_raw <- suppressMessages(readxl::read_excel(file_path, sheet = sheet, col_names = FALSE))
  
  r1 <- as.character(df_raw[1, ])
  r2 <- as.character(df_raw[2, ])
  r1[is.na(r1)] <- ""
  r2[is.na(r2)] <- ""
  
  merged_names <- trimws(paste(r1, r2, sep = " "))
  merged_names[merged_names == ""] <- "Blank_Col"
  
  safe_names <- paste0(merged_names, "___DUP", seq_along(merged_names))
  names(df_raw) <- safe_names
  df_raw <- df_raw[-c(1, 2), ]
  
  # ---------------------------------------------------------
  # 2. DEFENSIVE COLUMN RESOLVER
  # ---------------------------------------------------------
  raw_names <- names(df_raw)
  base_names <- stringr::str_replace(raw_names, "___DUP[0-9]+$", "")
  base_names <- trimws(base_names)
  
  target_cols <- c(col_depth_from, col_depth_to, vars_to_extract)
  
  resolved_indices <- integer()
  found_targets <- character()
  
  missing_depth_cols <- character()
  missing_var_cols <- character()
  duplicate_logs <- c()
  
  for (tgt in target_cols) {
    tgt_clean <- trimws(tgt)
    
    # 1. Exact match
    matches <- which(base_names == tgt_clean)
    
    # 2. Prefix fallback (ignores appended row 2 units)
    if (length(matches) == 0) {
      matches <- which(stringr::str_starts(base_names, stringr::fixed(tgt_clean)))
    }
    
    if (length(matches) == 0) {
      if (tgt_clean %in% c(col_depth_from, col_depth_to)) {
        missing_depth_cols <- c(missing_depth_cols, tgt_clean)
      } else {
        missing_var_cols <- c(missing_var_cols, tgt_clean)
      }
    } else {
      best_match <- matches[1]
      
      if (length(matches) > 1) {
        data_found <- FALSE
        for (m in matches) {
          col_data <- df_raw[[m]]
          valid_vals <- col_data[!is.na(col_data)]
          valid_vals <- trimws(as.character(valid_vals))
          valid_vals <- valid_vals[valid_vals != ""]
          
          if (length(valid_vals) > 1) {
            best_match <- m
            data_found <- TRUE
            break 
          }
        }
        
        if (data_found) {
          duplicate_logs <- c(duplicate_logs, sprintf("   -> Column '%s' found %d times. Safely extracted Index %d.", tgt_clean, length(matches), best_match))
        } else {
          duplicate_logs <- c(duplicate_logs, sprintf("   -> Column '%s' found %d times, but ALL instances appear empty. Defaulted to Index %d.", tgt_clean, length(matches), best_match))
        }
      }
      
      resolved_indices <- c(resolved_indices, best_match)
      found_targets <- c(found_targets, tgt_clean)
    }
  }
  
  # ---------------------------------------------------------
  # 3. FATAL ALARMS & INTERVENTION LOGS
  # ---------------------------------------------------------
  if (length(missing_depth_cols) > 0) {
    stop(sprintf("\n🚨 FATAL ERROR: Essential depth columns are missing: [%s]\n", paste(missing_depth_cols, collapse = ", ")), call. = FALSE)
  }
  
  if (length(duplicate_logs) > 0) {
    message(paste(c("", "--- PIPELINE INTERVENTION: DUPLICATE COLUMNS ---", duplicate_logs, "------------------------------------------------"), collapse = "\n"))
  }
  
  # ---------------------------------------------------------
  # 4. ISOLATE, NORMALIZE, AND CALCULATE
  # ---------------------------------------------------------
  df_processed <- df_raw[, resolved_indices]
  names(df_processed) <- found_targets
  
  vars_actually_found <- intersect(vars_to_extract, found_targets)
  
  df_processed <- df_processed %>%
    dplyr::mutate(
      Depth = paste0(.data[[col_depth_from]], "-", .data[[col_depth_to]]),
      SortKey = suppressWarnings(as.numeric(gsub("[^0-9.]", "", .data[[col_depth_from]])))
    ) %>%
    dplyr::mutate(
      dplyr::across(dplyr::all_of(vars_actually_found), ~ suppressWarnings(as.numeric(.)))
    ) %>%
    dplyr::group_by(Depth, SortKey) %>%
    dplyr::summarise(
      dplyr::across(
        dplyr::all_of(vars_actually_found), 
        ~ mean(., na.rm = TRUE)
      ),
      .groups = "drop"
    ) %>%
    dplyr::arrange(SortKey) %>%
    dplyr::select(-SortKey) %>%
    # ROUND ALL FOUND VARIABLES TO 3 DECIMAL PLACES
    dplyr::mutate(
      dplyr::across(dplyr::all_of(vars_actually_found), ~ round(., 3))
    )
  
  # ---------------------------------------------------------
  # 5. DATA GAP ALARM (Missing & Empty Variables)
  # ---------------------------------------------------------
  empty_var_cols <- character()
  for (v in vars_actually_found) {
    if (all(is.na(df_processed[[v]]))) {
      empty_var_cols <- c(empty_var_cols, v)
    }
  }
  
  if (length(missing_var_cols) > 0 || length(empty_var_cols) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " ⚠️ DATA GAP ALARM: MISSING OR EMPTY VARIABLES ⚠️",
      "======================================================================",
      if (length(missing_var_cols) > 0) c(" -> NOT FOUND (Missing entirely from Excel):", paste("    -", missing_var_cols)),
      if (length(empty_var_cols) > 0) c(" -> NO DATA (Column exists, but is 100% NA):", paste("    -", empty_var_cols)),
      "======================================================================",
      " Note: The pipeline bypassed these and continued processing the rest.",
      ""
    )
    warning(paste(warning_box, collapse = "\n"), call. = FALSE)
  }
  
  # ---------------------------------------------------------
  # 6. LOCAL CSV EXPORT
  # ---------------------------------------------------------
  write.csv(df_processed, log_file_name, row.names = FALSE, na = "")
  message(sprintf("💾 Success: Exported cleaned soil profile to '%s'", log_file_name))
  
  return(df_processed)
}