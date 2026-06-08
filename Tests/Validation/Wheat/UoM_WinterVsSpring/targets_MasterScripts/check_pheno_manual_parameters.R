#' Validate or Generate Manual Phenology and Germination Parameters
#'
#' @description
#' Audits two manual input files: ConstantPhenology and GermDatesInput. 
#' Generates templates if missing. If present, it validates SimulationNames, 
#' ensures required APSIM columns exist, and strictly enforces data types 
#' (Numeric for phenology, dd-MMM-yyyy for dates).
#'
#' @param folder_name Character. The directory where the files should live.
#' @param proj_name Character. The project prefix (e.g., "Dookie2024").
#' @param sim_names_df Dataframe. Must contain a 'SimulationName' column.
#'
#' @return A named list containing the two validated dataframes.
#' @export
check_pheno_manual_parameters <- function(folder_name, proj_name, sim_names_df) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("readr", quietly = TRUE)) stop("Package 'readr' required.")
  if (!requireNamespace("stringr", quietly = TRUE)) stop("Package 'stringr' required.")
  
  # ------------------------------------------------------------------
  # 0. SETUP & EXTRACTION
  # ------------------------------------------------------------------
  if (!"SimulationName" %in% names(sim_names_df)) {
    stop("CRITICAL: 'sim_names_df' must contain a 'SimulationName' column.", call. = FALSE)
  }
  
  active_sims <- unique(sim_names_df$SimulationName)
  active_sims <- active_sims[!is.na(active_sims) & active_sims != ""]
  
  if (!dir.exists(folder_name)) {
    dir.create(folder_name, recursive = TRUE)
  }
  
  # Define File Paths
  file_pheno <- file.path(folder_name, paste0(proj_name, "_ConstantPhenology.csv"))
  file_germ  <- file.path(folder_name, paste0(proj_name, "_GermDatesInput.csv"))
  
  # Define Required Columns (Bracket syntax requires exact matching)
  cols_pheno <- c(
    "[Grain].MaximumPotentialGrainSize.FixedValue",
    "[Grain].NumberFunction.GrainNumber.GrainsPerGramOfStem.FixedValue",
    "[Phenology].PhyllochronPpSensitivity.FixedValue",
    "[Phenology].CAMP.FLNparams.MinLN",
    "[Phenology].CAMP.FLNparams.PpLN",
    "[Phenology].CAMP.FLNparams.VrnLN",
    "[Phenology].CAMP.FLNparams.VxPLN",
    "[Phenology].HeadEmergenceLongDayBase.FixedValue",
    "[Phenology].HeadEmergencePpSensitivity.FixedValue",
    "[Phenology].CAMP.EnvData.VrnTreatTemp",
    "[Phenology].CAMP.EnvData.VrnTreatDuration"
  )
  
  col_germ <- "[Wheat].Phenology.Germinating.DateToProgress"
  
  cat("\n======================================================================\n")
  cat(" \U0001F9EA  AUDITING MANUAL PHENOLOGY & GERMINATION PARAMETERS \n")
  cat("======================================================================\n")
  
  # ==================================================================
  # 1. PROCESS CONSTANT PHENOLOGY
  # ==================================================================
  cat(" -> Checking:", basename(file_pheno), "\n")
  
  if (!file.exists(file_pheno)) {
    warning(sprintf("⚠️ File not found. Creating template: %s", basename(file_pheno)), call. = FALSE)
    df_pheno <- data.frame(SimulationName = active_sims, check.names = FALSE)
    for (col in cols_pheno) df_pheno[[col]] <- NA_real_
    readr::write_csv(df_pheno, file_pheno)
    
  } else {
    df_pheno <- suppressMessages(readr::read_csv(file_pheno, show_col_types = FALSE))
    file_modified <- FALSE
    
    # Check 1A: Missing Columns
    missing_cols <- setdiff(cols_pheno, names(df_pheno))
    if (length(missing_cols) > 0) {
      warning(sprintf("⚠️ Phenology file is missing %d required columns. Auto-injecting them as NAs.", length(missing_cols)), call. = FALSE)
      for (col in missing_cols) df_pheno[[col]] <- NA_real_
      file_modified <- TRUE
    }
    
    # Check 1B: Missing Simulations
    missing_sims <- setdiff(active_sims, df_pheno$SimulationName)
    if (length(missing_sims) > 0) {
      warning(sprintf("⚠️ Detected %d new simulations. Auto-appending them to Phenology file.", length(missing_sims)), call. = FALSE)
      new_rows <- data.frame(SimulationName = missing_sims, check.names = FALSE)
      for (col in cols_pheno) new_rows[[col]] <- NA_real_
      df_pheno <- dplyr::bind_rows(df_pheno, new_rows)
      file_modified <- TRUE
    }
    
    if (file_modified) readr::write_csv(df_pheno, file_pheno)
    
    # Check 1C: Data Type Integrity (Must be numeric)
    bad_numeric_cols <- c()
    for (col in cols_pheno) {
      # Suppress warnings to silently check for coercion failures
      test_num <- suppressWarnings(as.numeric(as.character(df_pheno[[col]])))
      # If a value isn't NA originally, but becomes NA when forced to numeric, it's text.
      if (any(!is.na(df_pheno[[col]]) & is.na(test_num))) {
        bad_numeric_cols <- c(bad_numeric_cols, col)
      }
    }
    
    if (length(bad_numeric_cols) > 0) {
      stop_msg <- c(
        "",
        "🚨 FATAL ERROR: NON-NUMERIC DATA IN PHENOLOGY PARAMETERS 🚨",
        "The following columns contain text/letters instead of numbers:",
        paste("   -", bad_numeric_cols),
        sprintf(" -> File to fix: %s", file_pheno),
        "Please fix these in Excel before continuing."
      )
      stop(paste(stop_msg, collapse = "\n"), call. = FALSE)
    }
  }
  
  # ==================================================================
  # 2. PROCESS GERM DATES
  # ==================================================================
  cat(" -> Checking:", basename(file_germ), "\n")
  
  if (!file.exists(file_germ)) {
    warning(sprintf("⚠️ File not found. Creating template: %s", basename(file_germ)), call. = FALSE)
    df_germ <- data.frame(SimulationName = active_sims, check.names = FALSE)
    df_germ[[col_germ]] <- "" # Empty string to force user entry
    readr::write_csv(df_germ, file_germ)
    
  } else {
    df_germ <- suppressMessages(readr::read_csv(file_germ, show_col_types = FALSE))
    file_modified <- FALSE
    
    # Check 2A: Missing Columns
    if (!col_germ %in% names(df_germ)) {
      warning(sprintf("⚠️ Germination file is missing the required date column. Auto-injecting it."), call. = FALSE)
      df_germ[[col_germ]] <- ""
      file_modified <- TRUE
    }
    
    # Check 2B: Missing Simulations
    missing_sims <- setdiff(active_sims, df_germ$SimulationName)
    if (length(missing_sims) > 0) {
      warning(sprintf("⚠️ Detected %d new simulations. Auto-appending them to Germination file.", length(missing_sims)), call. = FALSE)
      new_rows <- data.frame(SimulationName = missing_sims, check.names = FALSE)
      new_rows[[col_germ]] <- ""
      df_germ <- dplyr::bind_rows(df_germ, new_rows)
      file_modified <- TRUE
    }
    
    if (file_modified) readr::write_csv(df_germ, file_germ)
    
    # Check 2C: Strict Date Format Verification (dd-MMM-yyyy)
    dates_to_check <- df_germ[[col_germ]][!is.na(df_germ[[col_germ]]) & trimws(df_germ[[col_germ]]) != ""]
    
    if (length(dates_to_check) > 0) {
      # Regex strictly enforces exactly 1 or 2 digits, a hyphen, 3 letters, a hyphen, and 4 digits
      valid_format <- stringr::str_detect(trimws(dates_to_check), "^[0-9]{1,2}-[a-zA-Z]{3}-[0-9]{4}$")
      
      if (!all(valid_format)) {
        bad_dates <- dates_to_check[!valid_format]
        stop_msg <- c(
          "",
          "🚨 FATAL ERROR: INVALID DATE FORMAT IN GERMINATION FILE 🚨",
          "APSIM strictly requires dates in the format 'dd-MMM-yyyy' (e.g., 15-May-2024).",
          "The following invalid entries were found:",
          paste("   -", head(bad_dates, 5)),
          if (length(bad_dates) > 5) "...and more.",
          sprintf(" -> File to fix: %s", file_germ),
          "Please fix these in Excel before continuing."
        )
        stop(paste(stop_msg, collapse = "\n"), call. = FALSE)
      }
    }
  }
  
  cat("======================================================================\n\n")
  
  # Return both dataframes cleanly mapped
  return(list(
    ConstantPhenology = df_pheno %>% dplyr::filter(SimulationName %in% active_sims),
    GermDatesInput    = df_germ %>% dplyr::filter(SimulationName %in% active_sims)
  ))
}