#' Compile and Format All Observed Data (Universal Master)
#'
#' @export
compile_all_observed <- function(folder, excel_files, df_obs_info, df_simNames) {
  
  require(dplyr)
  require(purrr)
  require(stringr)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  req_cols <- c("df_name", "sheet_name", "column_name", "apsim_var_name", "corr_fact")
  missing_cols <- setdiff(req_cols, names(df_obs_info))
  if (length(missing_cols) > 0) {
    stop(sprintf(
      "CRITICAL: 'df_obs_info' missing columns: %s \n   -> Found columns instead: [%s]", 
      paste(missing_cols, collapse=", "),
      paste(names(df_obs_info), collapse=", ")
    ))
  }
  
  if (!all(c("Cultivar", "SimulationName") %in% names(df_simNames))) {
    stop(sprintf(
      "CRITICAL: 'df_simNames' must contain both 'Cultivar' and 'SimulationName' columns. \n   -> Found columns instead: [%s]",
      paste(names(df_simNames), collapse=", ")
    ))
  }
  
  full_paths <- file.path(folder, excel_files)
  missing_files <- full_paths[!file.exists(full_paths)]
  if (length(missing_files) > 0) {
    stop("CRITICAL: Files not found on disk: ", paste(basename(missing_files), collapse=", "))
  }
  
  # ------------------------------------------------------------------
  # 2. READ, STACK, AND INJECT
  # ------------------------------------------------------------------
  final_tibble <- df_obs_info %>%
    dplyr::mutate(
      data = purrr::pmap(
        list(df_name, sheet_name, column_name, apsim_var_name, corr_fact),
        function(name_val, sh, col, new_col, corr) {
          
          # Read from all provided files (1 to N) and bind rows vertically
          raw_df <- purrr::map_dfr(full_paths, function(path) {
            read_observed_func(
              file_path   = path,
              SheetName   = as.character(sh),
              VarName     = as.character(col),
              NewVarName  = as.character(new_col),
              UnitCorrect = as.numeric(corr)
            )
          })
          
          if (nrow(raw_df) == 0) {
            return(dplyr::tibble())
          }
          
          # Universal Join Logic: Inject SimulationName based on Cultivar
          if ("Cultivar" %in% names(raw_df)) {
            
            if (!"SimulationName" %in% names(raw_df)) {
              
              # =========================================================
              # THE FUZZY CULTIVAR MATCHER
              # =========================================================
              official_cults <- unique(df_simNames$Cultivar)
              raw_cults <- unique(raw_df$Cultivar)
              
              # Find which raw cultivars don't exist in the official metadata mapping
              unmatched_cults <- setdiff(raw_cults, official_cults)
              
              if (length(unmatched_cults) > 0) {
                for (u_cult in unmatched_cults) {
                  if (is.na(u_cult) || u_cult == "") next
                  
                  # Phase 1: Does the official name live inside the raw name?
                  match_idx <- which(stringr::str_detect(tolower(u_cult), stringr::fixed(tolower(official_cults))))
                  
                  # Phase 2: Does the raw name live inside the official name?
                  if (length(match_idx) == 0) {
                    match_idx <- which(stringr::str_detect(tolower(official_cults), stringr::fixed(tolower(u_cult))))
                  }
                  
                  # Phase 3: Minor typo detection
                  if (length(match_idx) == 0) {
                    match_idx <- agrep(tolower(u_cult), tolower(official_cults), max.distance = 0.1)
                  }
                  
                  # Apply fix ONLY if we found exactly ONE confident match
                  if (length(match_idx) == 1) {
                    matched_official <- official_cults[match_idx]
                    
                    # Overwrite the raw dataframe with the cleaned official name
                    raw_df$Cultivar[raw_df$Cultivar == u_cult] <- matched_official
                    
                    # Print the polite notice to the console
                    message(sprintf("   -> Notice in '%s': Auto-corrected raw Cultivar '%s' to '%s'", 
                                    name_val, u_cult, matched_official))
                  }
                }
              }
              # =========================================================
              
              # Now execute the standard join safely!
              raw_df <- raw_df %>%
                dplyr::left_join(df_simNames, by = "Cultivar") %>%
                dplyr::relocate(SimulationName, .after = Cultivar) 
            }
            
          } else {
            warning(sprintf("Dataframe '%s' does not contain a 'Cultivar' column. 'SimulationName' could not be joined.", name_val), call. = FALSE)
          }
          
          # =========================================================
          # STRICT OUTPUT FILTER
          # =========================================================
          # Drop everything except the APSIM essentials
          raw_df <- raw_df %>%
            dplyr::select(dplyr::any_of(c("SimulationName", "Date", new_col)))
          
          return(raw_df)
        }
      )
    ) %>%
    dplyr::select(df_name, data)
  
  return(final_tibble)
}