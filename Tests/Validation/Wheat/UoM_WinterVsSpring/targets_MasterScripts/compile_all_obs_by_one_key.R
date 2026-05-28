#' Compile and Format All Observed Data (Explicit Single-Key Version)
#'
#' @export
compile_all_obs_by_one_key <- function(folder, excel_files, df_obs_info, df_simNames, unique_key, exp_keys = NULL) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("purrr", quietly = TRUE)) stop("Package 'purrr' required.")
  if (!requireNamespace("tidyr", quietly = TRUE)) stop("Package 'tidyr' required.")
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS: STRICT COLUMN VALIDATION
  # ------------------------------------------------------------------
  req_cols <- c("df_name", "sheet_name", "column_name", "apsim_var_name", "corr_fact")
  missing_cols <- setdiff(req_cols, names(df_obs_info))
  if (length(missing_cols) > 0) {
    stop(sprintf("\n🚨 CRITICAL ERROR: 'df_obs_info' missing required columns: [%s]", 
                 paste(missing_cols, collapse=", ")), call. = FALSE)
  }
  
  if (!unique_key %in% names(df_simNames)) {
    stop(sprintf("\n🚨 CRITICAL ERROR: The unique_key '%s' was not found in your mapping CSV ('df_simNames').", 
                 unique_key), call. = FALSE)
  }
  
  if (!"SimulationName" %in% names(df_simNames)) {
    stop("\n🚨 CRITICAL ERROR: 'SimulationName' is missing from your mapping CSV ('df_simNames').", call. = FALSE)
  }
  
  # Setup Join Keys
  use_keys <- !is.null(exp_keys)
  join_keys <- unique_key
  
  if (use_keys) {
    if (length(excel_files) != length(exp_keys)) {
      stop("\n🚨 CRITICAL ERROR: The number of 'excel_files' must exactly match the number of 'exp_keys'.", call. = FALSE)
    }
    if (!"Exp_key_name" %in% names(df_simNames)) {
      stop("\n🚨 CRITICAL ERROR: You provided 'exp_keys' but 'Exp_key_name' is missing from the mapping CSV.", call. = FALSE)
    }
    join_keys <- c("Exp_key_name", unique_key)
  }
  
  # ------------------------------------------------------------------
  # 2. THE DUPLICATE KEY DEFENDER (Pre-scan mapping table)
  # ------------------------------------------------------------------
  dup_check <- df_simNames %>%
    dplyr::group_by(dplyr::across(dplyr::all_of(join_keys))) %>%
    dplyr::tally() %>%
    dplyr::filter(n > 1)
  
  if (nrow(dup_check) > 0) {
    dup_str <- paste(capture.output(print(dup_check)), collapse = "\n")
    stop_msg <- c(
      "",
      "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
      " \U0001F6A8 FATAL ALARM: DUPLICATE LOOKUP KEYS IN MAPPING TABLE \U0001F6A8 ",
      "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
      sprintf(" -> Join Keys Tested: [%s]", paste(join_keys, collapse = ", ")),
      "",
      " -> DUPLICATES FOUND:",
      dup_str,
      "",
      " ACTION: Fix the mapping CSV so each key combination only appears once!",
      "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
      ""
    )
    stop(paste(stop_msg, collapse = "\n"), call. = FALSE)
  }
  
  # Prepare mapping table by dropping everything except the keys and target
  # Forces keys to character to prevent integer/character join mismatch!
  clean_mapping_df <- df_simNames %>% 
    dplyr::select(dplyr::all_of(c(join_keys, "SimulationName"))) %>%
    dplyr::mutate(dplyr::across(dplyr::all_of(join_keys), as.character))
  
  full_paths <- file.path(folder, excel_files)
  iter_keys <- if (use_keys) exp_keys else rep(NA, length(full_paths))
  
  # ------------------------------------------------------------------
  # 3. READ, STACK, AND INJECT
  # ------------------------------------------------------------------
  final_tibble <- df_obs_info %>%
    dplyr::mutate(
      data = purrr::pmap(
        list(df_name, sheet_name, column_name, apsim_var_name, corr_fact),
        function(name_val, sh, col, new_col, corr) {
          
          raw_df <- purrr::map2_dfr(full_paths, iter_keys, function(path, key) {
            
            temp_df <- tryCatch({
              read_observed_one_key(
                file_path   = path,
                SheetName   = as.character(sh),
                VarName     = as.character(col),
                NewVarName  = as.character(new_col),
                UnitCorrect = as.numeric(corr),
                unique_key  = unique_key          
              )
            }, error = function(e) {
              message(sprintf("\n \u26A0\uFE0F SKIPPING EXTRACTION: '%s' in sheet '%s'. Error: %s", col, sh, e$message))
              return(NULL)
            })
            
            if (is.null(temp_df) || !is.data.frame(temp_df) || nrow(temp_df) == 0) {
              return(dplyr::tibble())
            }
            
            if (use_keys) temp_df <- temp_df %>% dplyr::mutate(Exp_key_name = key)
            
            return(temp_df)
          })
          
          if (nrow(raw_df) == 0) return(dplyr::tibble())
          
          # ---------------------------------------------------------
          # THE STRICT JOIN
          # ---------------------------------------------------------
          if (!"SimulationName" %in% names(raw_df)) {
            
            if (!unique_key %in% names(raw_df)) {
              message(sprintf("   -> \u274C ERROR in '%s': Raw data does not contain the unique key '%s'. Skipping join.", 
                              name_val, unique_key))
              return(dplyr::tibble())
            }
            
            raw_df <- raw_df %>%
              dplyr::left_join(clean_mapping_df, by = join_keys, relationship = "many-to-one") %>%
              dplyr::relocate(SimulationName, .before = 1)
          }
          
          # ---------------------------------------------------------
          # 4. REPLICATE AGGREGATION & NA PURGE
          # ---------------------------------------------------------
          # This averages all replicate plots that share the same SimulationName and Date
          raw_df <- raw_df %>%
            dplyr::select(dplyr::any_of(c("SimulationName", "Date", new_col))) %>%
            dplyr::filter(!is.na(SimulationName)) %>%
            dplyr::group_by(SimulationName, Date) %>%
            dplyr::summarise(
              dplyr::across(
                dplyr::all_of(new_col),
                ~ replace(mean(.x, na.rm = TRUE), is.nan(mean(.x, na.rm = TRUE)), NA)
              ),
              .groups = "drop"
            ) %>%
            tidyr::drop_na(dplyr::all_of(new_col))
          
          return(raw_df)
        }
      )
    ) %>%
    dplyr::select(df_name, data)
  
  return(final_tibble)
}