#' Compile and Format All Observed Data
#'
#' @description
#' Reads multiple sheets from an Excel file based on metadata, renames columns, 
#' applies unit corrections, and dynamically injects `SimulationName` into each 
#' dataset by cross-referencing a Cultivar lookup table.
#'
#' @param folder Character. Folder path where the raw data resides.
#' @param excel_file Character. Name of the Excel file.
#' @param df_obs_info Data frame. Contains metadata mapping: `df_name`, `sheet_name`, 
#'   `column_name`, `apsim_var_name`, and `corr_fact`.
#' @param df_simNameByCult Data frame. A lookup table mapping the physical 
#'   `Cultivar` to the exact APSIM `SimulationName`.
#'
#' @return A nested tibble with `df_name` and a `data` list-column containing 
#'   the processed dataframes.
#'
#' @importFrom dplyr left_join relocate tibble
#' @export
compile_all_observed <- function(folder, excel_file, df_obs_info, df_simNameByCult) {
  
  require(dplyr)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!all(c("df_name","sheet_name","column_name","apsim_var_name","corr_fact") %in% names(df_obs_info))) {
    stop("CRITICAL: 'df_obs_info' is missing required columns.")
  }
  
  # Ensure the new lookup table has the required mapping keys
  if (!all(c("Cultivar", "SimulationName") %in% names(df_simNameByCult))) {
    stop("CRITICAL: 'df_simNameByCult' must contain both 'Cultivar' and 'SimulationName' columns.")
  }
  
  # ------------------------------------------------------------------
  # 2. READ AND INJECT
  # ------------------------------------------------------------------
  file_path <- file.path(folder, excel_file)
  results <- vector("list", nrow(df_obs_info))
  
  for (i in seq_len(nrow(df_obs_info))) {
    row <- df_obs_info[i, ]
    
    # Read the raw dataframe using the existing helper function
    raw_df <- read_observed_func(
      file_path   = file_path,
      SheetName   = as.character(row$sheet_name),
      VarName     = as.character(row$column_name),
      NewVarName  = as.character(row$apsim_var_name),
      UnitCorrect = as.numeric(row$corr_fact)
    )
    
    # Inject SimulationName based on Cultivar
    if ("Cultivar" %in% names(raw_df)) {
      raw_df <- raw_df %>%
        dplyr::left_join(df_simNameByCult, by = "Cultivar") %>%
        # Neatly organize columns so SimulationName sits right next to Cultivar
        dplyr::relocate(SimulationName, .after = Cultivar) 
    } else {
      # Warn if Cultivar is missing from this specific sheet so the user knows SimulationName wasn't joined
      warning(sprintf("Dataframe '%s' does not contain a 'Cultivar' column. 'SimulationName' could not be joined.", row$df_name), call. = FALSE)
    }
    
    results[[i]] <- raw_df
    names(results)[i] <- row$df_name
  }
  
  # ------------------------------------------------------------------
  # 3. FORMAT NESTED OUTPUT
  # ------------------------------------------------------------------
  final <- dplyr::tibble(
    df_name = df_obs_info$df_name,
    data    = results
  )
  
  return(final)
}