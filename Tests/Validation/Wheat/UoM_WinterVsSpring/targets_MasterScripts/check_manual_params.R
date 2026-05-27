#' Load, Validate, or Generate Manual Wheat Parameters
#'
#' @description
#' Checks for the existence of a manual parameter CSV. If missing, it generates 
#' a template. If it exists, it validates SimulationNames. If new simulations 
#' are detected in the pipeline, it auto-appends them to the CSV with default values.
#'
#' @export
check_manual_params <- function(folder, filename, df_active_sims) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("readr", quietly = TRUE)) stop("Package 'readr' required.")
  
  active_sims <- unique(df_active_sims$SimulationName)
  file_path <- file.path(folder, filename)
  
  col_phyllo <- "[Wheat].Phenology.Phyllochron.BasePhyllochron.FixedValue"
  col_sensit <- "[Wheat].Phenology.PhyllochronPpSensitivity.FixedValue"
  
  # ------------------------------------------------------------------
  # 1. Template Generation (If file is completely missing)
  # ------------------------------------------------------------------
  if (!file.exists(file_path)) {
    message("📁 Manual parameter file not found. Generating template...")
    
    if (!dir.exists(folder)) {
      warning("⚠️ The folder ", folder, " does not exist. Creating it...")
      dir.create(folder, recursive = TRUE)
    }
    
    template_df <- data.frame(
      SimulationName = active_sims,
      `[Wheat].Phenology.Phyllochron.BasePhyllochron.FixedValue` = 95,
      `[Wheat].Phenology.PhyllochronPpSensitivity.FixedValue` = 0.3,
      check.names = FALSE
    )
    
    readr::write_csv(template_df, file_path)
    message("✅ Template created at: ", file_path)
    return(template_df)
  }
  
  # ------------------------------------------------------------------
  # 2. Loading & Auto-Appending (If file exists)
  # ------------------------------------------------------------------
  param_df <- readr::read_csv(file_path, show_col_types = FALSE)
  
  missing_sims <- setdiff(active_sims, param_df$SimulationName)
  
  if (length(missing_sims) > 0) {
    message(sprintf("⚠️ Detected %d new simulations missing from the parameter CSV.", length(missing_sims)))
    message(" -> Auto-appending them to the file with default values...")
    
    new_rows <- data.frame(
      SimulationName = missing_sims,
      `[Wheat].Phenology.Phyllochron.BasePhyllochron.FixedValue` = 95,
      `[Wheat].Phenology.PhyllochronPpSensitivity.FixedValue` = 0.3,
      check.names = FALSE
    )
    
    # Bind the new default rows to the existing data and overwrite the CSV
    param_df <- dplyr::bind_rows(param_df, new_rows)
    readr::write_csv(param_df, file_path)
    
    warning("New simulations were automatically added to the manual parameter file. Please review the defaults in Excel!", call. = FALSE)
  }
  
  # ------------------------------------------------------------------
  # 3. Validation Rules (Ranges)
  # ------------------------------------------------------------------
  violations <- param_df %>%
    dplyr::filter(SimulationName %in% active_sims) %>%
    dplyr::mutate(
      phyllo_err = .data[[col_phyllo]] < 80 | .data[[col_phyllo]] > 120,
      sensit_err = .data[[col_sensit]] < 0  | .data[[col_sensit]] > 0.6
    )
  
  if (any(violations$phyllo_err)) {
    bad_p <- violations$SimulationName[violations$phyllo_err]
    warning("❌ Phyllochron out of range (80-120) for: ", paste(bad_p, collapse = ", "), call. = FALSE)
  }
  
  if (any(violations$sensit_err)) {
    bad_s <- violations$SimulationName[violations$sensit_err]
    warning("❌ PpSensitivity out of range (0-0.6) for: ", paste(bad_s, collapse = ", "), call. = FALSE)
  }
  
  return(param_df %>% dplyr::filter(SimulationName %in% active_sims))
}