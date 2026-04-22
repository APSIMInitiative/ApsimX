#' Load, Validate, or Generate Manual Wheat Parameters
#'
#' @description
#' Checks for the existence of a manual parameter CSV. If missing, it generates 
#' a template using current SimulationNames and default values. If it exists, 
#' it validates SimulationNames and parameter ranges (80-120 for Phyllochron, 
#' 0-0.6 for PpSensitivity).
#'
#' @param folder String. Path to the folder containing the input CSV.
#' @param filename String. Name of the CSV file.
#' @param active_sims Vector. Unique SimulationNames from the observed data.
#'
#' @return A data frame containing validated parameters.
#' 
#' @export
check_manual_params <- function(folder, filename, df_active_sims) {
  
  active_sims <- unique(df_active_sims$SimulationName)
  
  file_path <- file.path(folder, filename)
  
  col_phyllo <- "[Wheat].Phenology.Phyllochron.BasePhyllochron.FixedValue"
  col_sensit <- "[Wheat].Phenology.PhyllochronPpSensitivity.FixedValue"
  
  # 1. Template Generation (------------- If file is not found ------------)
  if (!file.exists(file_path)) {
    message("📁 Manual parameter file not found. Generating template...")
    
  #  if (!dir.exists(folder)) dir.create(folder, recursive = TRUE)
    if (!dir.exists(folder)) {
      warning("⚠️ The folder ",folder," does not exist")
      
    }
    
    template_df <- data.frame(
      SimulationName = active_sims,
      # Default starting values
      `[Wheat].Phenology.Phyllochron.BasePhyllochron.FixedValue` = 95,
      `[Wheat].Phenology.PhyllochronPpSensitivity.FixedValue` = 0.3,
      check.names = FALSE # Preserves the [brackets] in column names
    )
    
    readr::write_csv(template_df, file_path)
    message("✅ Template created at: ", file_path)
    return(template_df)
  }
  
  # 2. Loading and Validation (--------- If file exists-----------)
  param_df <- readr::read_csv(file_path, show_col_types = FALSE)
  
  # Check for missing SimulationNames
  missing_sims <- setdiff(active_sims, param_df$SimulationName)
  if (length(missing_sims) > 0) {
    warning("⚠️ The following simulations are in your data but missing from the CSV: ", 
            paste(missing_sims, collapse = ", "))
  }
  
  # Validate Ranges
  violations <- param_df %>%
    dplyr::filter(SimulationName %in% active_sims) %>%
    dplyr::mutate(
      phyllo_err = .data[[col_phyllo]] < 80 | .data[[col_phyllo]] > 120,
      sensit_err = .data[[col_sensit]] < 0 | .data[[col_sensit]] > 0.6
    )
  
  if (any(violations$phyllo_err)) {
    bad_p <- violations$SimulationName[violations$phyllo_err]
    message("❌ Phyllochron out of range (80-120) for: ", paste(bad_p, collapse = ", "))
  }
  
  if (any(violations$sensit_err)) {
    bad_s <- violations$SimulationName[violations$sensit_err]
    message("❌ PpSensitivity out of range (0-0.6) for: ", paste(bad_s, collapse = ", "))
  }
  
  return(param_df %>% dplyr::filter(SimulationName %in% active_sims))
}