#' Compile and Format All Observed Data (Universal Master)
#'
#' @export
compile_all_observed <- function(folder, excel_files, df_obs_info, df_simNames, exp_keys = NULL) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("purrr", quietly = TRUE)) stop("Package 'purrr' required.")
  if (!requireNamespace("stringr", quietly = TRUE)) stop("Package 'stringr' required.")
  
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
  
  # Determine if we are using the multi-experiment tie-breaker
  use_keys <- !is.null(exp_keys)
  
  # Ensure the keys match the number of files IF keys are provided
  if (use_keys && (length(excel_files) != length(exp_keys))) {
    stop("CRITICAL: The number of 'excel_files' must exactly match the number of 'exp_keys'.")
  }
  
  full_paths <- file.path(folder, excel_files)
  
  # Setup an iterator for the map function (safely uses NA if no keys are provided)
  iter_keys <- if (use_keys) exp_keys else rep(NA, length(full_paths))
  
  # ------------------------------------------------------------------
  # 2. READ, STACK, AND INJECT
  # ------------------------------------------------------------------
  final_tibble <- df_obs_info %>%
    dplyr::mutate(
      data = purrr::pmap(
        list(df_name, sheet_name, column_name, apsim_var_name, corr_fact),
        function(name_val, sh, col, new_col, corr) {
          
          # Use map2_dfr to iterate over BOTH the paths and the experiment keys (if any)
          raw_df <- purrr::map2_dfr(full_paths, iter_keys, function(path, key) {
            
            # ---------------------------------------------------------
            # THE SHIELD: tryCatch prevents missing sheets from crashing the pipeline
            # ---------------------------------------------------------
            temp_df <- tryCatch({
              read_observed_func(
                file_path   = path,
                SheetName   = as.character(sh),
                VarName     = as.character(col),
                NewVarName  = as.character(new_col),
                UnitCorrect = as.numeric(corr)
              )
            }, error = function(e) {
              # =========================================================
              # ⚠️ THE BIG WARNING ALARM ⚠️
              # =========================================================
              message("\n", strrep("=", 60))
              message(" \u26A0\uFE0F  MISSING DATA ALARM: SKIPPING EXTRACTION \u26A0\uFE0F ")
              message(strrep("=", 60))
              message(sprintf(" -> TARGET FILE : %s", basename(path)))
              message(sprintf(" -> TARGET SHEET: '%s'", sh))
              message(sprintf(" -> TARGET VAR  : '%s'", col))
              message(sprintf(" -> R ERROR     : %s", e$message))
              message(" -> ACTION      : Pipeline bypassed error and is continuing...")
              message(strrep("-", 60), "\n")
              
              return(NULL) # Safely pass NULL to the safeguard below
            })
            
            # =========================================================
            # 🛡️ THE NULL SAFEGUARD 
            # =========================================================
            # Prevent the "length zero" crash if data was missing
            if (is.null(temp_df) || !is.data.frame(temp_df) || nrow(temp_df) == 0) {
              return(dplyr::tibble())
            }
            
            # Dynamically stamp the Experiment Key ONLY if they were provided
            if (use_keys) {
              temp_df <- temp_df %>% dplyr::mutate(Exp_key_name = key)
            }
            
            return(temp_df)
          })
          
          if (nrow(raw_df) == 0) return(dplyr::tibble())
          
          # Universal Join Logic: Inject SimulationName
          if ("Cultivar" %in% names(raw_df)) {
            if (!"SimulationName" %in% names(raw_df)) {
              
              # THE FUZZY CULTIVAR MATCHER
              official_cults <- unique(df_simNames$Cultivar)
              raw_cults <- unique(raw_df$Cultivar)
              unmatched_cults <- setdiff(raw_cults, official_cults)
              
              if (length(unmatched_cults) > 0) {
                for (u_cult in unmatched_cults) {
                  if (is.na(u_cult) || u_cult == "") next
                  
                  match_idx <- which(stringr::str_detect(tolower(u_cult), stringr::fixed(tolower(official_cults))))
                  if (length(match_idx) == 0) match_idx <- which(stringr::str_detect(tolower(official_cults), stringr::fixed(tolower(u_cult))))
                  if (length(match_idx) == 0) match_idx <- agrep(tolower(u_cult), tolower(official_cults), max.distance = 0.1)
                  
                  if (length(match_idx) == 1) {
                    matched_official <- official_cults[match_idx]
                    raw_df$Cultivar[raw_df$Cultivar == u_cult] <- matched_official
                    message(sprintf("   -> Notice in '%s': Auto-corrected raw Cultivar '%s' to '%s'", 
                                    name_val, u_cult, matched_official))
                  }
                }
              }
              
              # THE SAFE MULTI-KEY JOIN
              join_keys <- base::intersect(names(raw_df), names(df_simNames))
              
              raw_df <- raw_df %>%
                dplyr::left_join(df_simNames, by = join_keys, relationship = "many-to-one") %>%
                dplyr::relocate(SimulationName, .after = Cultivar) 
            }
          }
          
          # STRICT OUTPUT FILTER
          raw_df <- raw_df %>%
            dplyr::select(dplyr::any_of(c("SimulationName", "Date", new_col)))
          
          return(raw_df)
        }
      )
    ) %>%
    dplyr::select(df_name, data)
  
  return(final_tibble)
}