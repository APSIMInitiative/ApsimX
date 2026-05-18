#' Prepare Final Observations for APSIM (Universal Master)
#'
#' @description
#' Filters out unwanted datasets, un-nests the master list, squashes staggered 
#' rows measured on the same day into a single row, and formats timestamps 
#' into the strict `Clock.Today` format required by APSIM.
#'
#' @param compiled_obs A nested tibble containing the final corrected dataframes.
#' @param dfs_out Character vector. Names of dataframes (from `df_name`) to 
#'   exclude from the final output. Defaults to NULL.
#'
#' @return A single, wide dataframe ready for APSIM injection without empty rows.
#'
#' @importFrom dplyr bind_rows filter mutate select everything any_of group_by summarise across relocate
#' @export
prepare_apsim_observed <- function(compiled_obs, dfs_out = NULL) {
  
  require(dplyr)
  
  # ------------------------------------------------------------------
  # 1. EXCLUSION FILTER (The 'dfs_out' Logic)
  # ------------------------------------------------------------------
  if (!is.null(dfs_out) && length(dfs_out) > 0) {
    
    # Check which exclusions actually exist in the nested tibble
    found_exclusions <- intersect(dfs_out, compiled_obs$df_name)
    missing_exclusions <- setdiff(dfs_out, compiled_obs$df_name)
    
    if (length(found_exclusions) > 0) {
      message(sprintf("   -> Notice: Excluding datasets from final output: [%s]", 
                      paste(found_exclusions, collapse = ", ")))
      compiled_obs <- compiled_obs %>% dplyr::filter(!df_name %in% dfs_out)
    }
    
    # Let the user know if they made a typo in their exclusion list
    if (length(missing_exclusions) > 0) {
      warning(sprintf("Requested exclusion [%s] not found in the master list.", 
                      paste(missing_exclusions, collapse = ", ")), call. = FALSE)
    }
  }
  
  # ------------------------------------------------------------------
  # 2. BIND & FIREWALL
  # ------------------------------------------------------------------
  df_final <- dplyr::bind_rows(compiled_obs$data) %>%
    
    # Aggressively scrub "ghost" rows
    dplyr::filter(
      !is.na(SimulationName), 
      trimws(as.character(SimulationName)) != "",
      !is.na(Date)
    ) %>%
    
    # Create the 'Clock.Today' column in the strict APSIM format
    dplyr::mutate(
      Clock.Today = format(as.POSIXct(Date), "%d/%m/%Y 00:00:00")
    )
  
  # ------------------------------------------------------------------
  # 3. STAGGERED ROW COLLAPSE
  # ------------------------------------------------------------------
  # Squash rows with the same SimulationName and Date into a single row.
  df_final <- df_final %>%
    # Safely drop intermediate joining keys so they don't break the numeric summary
    dplyr::select(-dplyr::any_of(c("Cultivar", "Date", "SowTime"))) %>% 
    
    dplyr::group_by(SimulationName, Clock.Today) %>%
    dplyr::summarise(
      dplyr::across(
        dplyr::everything(),
        ~ replace(mean(.x, na.rm = TRUE), is.nan(mean(.x, na.rm = TRUE)), NA)
      ),
      .groups = "drop"
    ) %>%
    
    # ----------------------------------------------------------------
  # 4. FINAL APSIM ORDERING
  # ----------------------------------------------------------------
  dplyr::relocate(SimulationName, Clock.Today)
  
  message("Successfully compiled, collapsed, and formatted the final APSIM observation dataframe.")
  return(df_final)
}