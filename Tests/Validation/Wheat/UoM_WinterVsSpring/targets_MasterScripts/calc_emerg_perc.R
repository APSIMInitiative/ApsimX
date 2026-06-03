#' Calculate Emergence Percentage from Raw Counts (Nested Tibble Version)
#'
#' @description
#' Homogenizes emergence data by converting raw plant counts into a 0-100 percentage scale.
#' It isolates the maximum count per `SimulationName` and divides all observations by that maximum.
#' This version is explicitly designed to work with {targets} nested tibbles 
#' (where datasets are stored in 'df_name' and 'data' columns).
#'
#' @param df_tbl A tibble containing at least 'df_name' (character) and 'data' (list of dataframes).
#' @param df_input_var_name Character. The string name inside 'df_name' to target.
#' @param df_new_var_name Character. The name of the new row/dataset to be appended.
#'
#' @return The original tibble with a newly appended row containing the percentage data.
#' @export
calc_emerg_perc <- function(df_tbl, df_input_var_name, df_new_var_name) {
  
  # ---- 1. DEFENSIVE CHECKS & STRUCTURE MAPPING ----
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  
  if (!is.data.frame(df_tbl) || !"df_name" %in% names(df_tbl) || !"data" %in% names(df_tbl)) {
    stop("CRITICAL [calc_emerg_perc]: Input must be a tibble containing 'df_name' and 'data' columns.")
  }
  
  # Check if the target actually exists inside the df_name column
  if (!df_input_var_name %in% df_tbl$df_name) {
    stop(sprintf("CRITICAL [calc_emerg_perc]: Input dataframe '%s' not found inside the 'df_name' column.", df_input_var_name))
  }
  
  # Extract the specific nested dataframe
  target_idx <- which(df_tbl$df_name == df_input_var_name)[1]
  df_target <- df_tbl$data[[target_idx]]
  
  if (is.null(df_target) || nrow(df_target) == 0) {
    warning(sprintf("Warning [calc_emerg_perc]: Nested dataframe '%s' is empty. Returning tibble unaltered.", df_input_var_name))
    return(df_tbl)
  }
  
  if (!"SimulationName" %in% names(df_target)) {
    stop("CRITICAL [calc_emerg_perc]: Target nested dataframe must contain a 'SimulationName' column.")
  }
  
  # ---- 2. DYNAMIC COLUMN IDENTIFICATION ----
  # Safely locate the date column
  date_col <- intersect(c("Clock.Today", "Date"), names(df_target))[1]
  if (is.na(date_col)) stop("CRITICAL [calc_emerg_perc]: Could not identify a valid Date or Clock.Today column.")
  
  # Isolate the core observation column (ignoring standard meta-keys)
  val_col <- setdiff(names(df_target), c("SimulationName", date_col, "Plot", "Exp_key_name"))[1]
  if (is.na(val_col)) stop("CRITICAL [calc_emerg_perc]: Could not identify the target observation/count column.")
  
  # ---- 3. PERCENTAGE CONVERSION LOGIC ----
  df_perc <- df_target %>%
    dplyr::group_by(SimulationName) %>%
    dplyr::mutate(
      # Find the local max, ignoring NAs. (suppressWarnings prevents -Inf output if all rows are NA)
      LocalMax = suppressWarnings(max(.data[[val_col]], na.rm = TRUE)),
      
      # Perform the math, with a shield against division by zero 
      !!val_col := dplyr::case_when(
        is.na(.data[[val_col]]) ~ NA_real_,
        is.infinite(LocalMax) | LocalMax <= 0 ~ 0,
        TRUE ~ (.data[[val_col]] / LocalMax) * 100
      )
    ) %>%
    dplyr::ungroup() %>%
    dplyr::select(-LocalMax)
  
  # ---- 4. APPEND NEW ROW TO MASTER TIBBLE ----
  # Create a new 1-row tibble to mimic the master structure
  new_row <- tibble::tibble(
    df_name = df_new_var_name,
    data = list(df_perc)
  )
  
  # Bind it to the bottom of the master table
  df_tbl_final <- dplyr::bind_rows(df_tbl, new_row)
  
  # ---- 5. AUDIT LOGGING ----
  log_box <- c(
    "",
    "----------------------------------------------------------------------",
    " 📈 PIPELINE ACTION: EMERGENCE PERCENTAGE CALCULATED 📈",
    "----------------------------------------------------------------------",
    sprintf(" -> Input Source  : '%s'", df_input_var_name),
    sprintf(" -> Target Column : '%s' (Internal variable preserved)", val_col),
    sprintf(" -> Output Target : '%s' (Appended as new row)", df_new_var_name),
    " -> Action        : Converted raw counts to 0-100% scale per SimulationName",
    "----------------------------------------------------------------------",
    ""
  )
  message(paste(log_box, collapse = "\n"))
  
  return(df_tbl_final)
}