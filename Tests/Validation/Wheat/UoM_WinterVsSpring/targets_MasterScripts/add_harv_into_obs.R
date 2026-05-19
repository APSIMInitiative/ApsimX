#' Inject Harvest Stage into Existing Observations (Universal Master)
#'
#' @description
#' Identifies the final measurement date for one or more reference variables 
#' for each simulation, and injects a new stage value (e.g., "HarvestRipe") 
#' into a target column on those exact existing rows.
#'
#' @details
#' If multiple reference variables are provided, the function evaluates each independently. 
#' If the final measurement dates differ among variables within the same simulation, 
#' it flags all respective dates and throws a descriptive warning so the user is aware 
#' of staggered harvest measurements.
#'
#' @param df Data frame containing the final formatted observations.
#' @param ref_vars Character vector. The column names used to find the last measurements 
#'   (e.g., "Wheat.Grain.Wt" or c("Wheat.Grain.Wt", "Wheat.AboveGround.Wt")).
#' @param new_col_name Character. The name of the column to hold the stage name.
#' @param new_col_value Character. The stage name to assign (e.g., "HarvestRipe").
#'
#' @return A data frame with the updated rows.
#'
#' @importFrom dplyr filter mutate group_by slice_max ungroup select left_join if_else summarise n_distinct distinct all_of
#' @importFrom tidyr pivot_longer
#' @importFrom lubridate parse_date_time
#' @importFrom rlang `:=` .data
#' @export
add_harv_into_obs <- function(df, ref_vars, new_col_name, new_col_value) {
  
  require(dplyr)
  require(tidyr)
  require(lubridate)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!"SimulationName" %in% names(df) || !"Clock.Today" %in% names(df)) {
    stop("CRITICAL: 'df' must contain 'SimulationName' and 'Clock.Today' columns.")
  }
  
  missing_vars <- setdiff(ref_vars, names(df))
  if (length(missing_vars) > 0) {
    stop(sprintf("CRITICAL: Reference variable(s) not found in dataframe: %s", 
                 paste(missing_vars, collapse = ", ")))
  }
  
  # ------------------------------------------------------------------
  # 2. ISOLATE AND PARSE DATES
  # ------------------------------------------------------------------
  df_dates <- df %>%
    dplyr::select(SimulationName, Clock.Today, dplyr::all_of(ref_vars)) %>%
    dplyr::filter(!is.na(Clock.Today), as.character(Clock.Today) != "") %>%
    dplyr::mutate(
      .temp_date = suppressWarnings(
        lubridate::parse_date_time(
          as.character(Clock.Today), 
          orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd")
        )
      )
    )
  
  # ------------------------------------------------------------------
  # 3. FIND MAX DATES PER VARIABLE
  # ------------------------------------------------------------------
  max_dates <- df_dates %>%
    tidyr::pivot_longer(
      cols = dplyr::all_of(ref_vars),
      names_to = "ref_var",
      values_to = "val",
      values_transform = list(val = as.character) # Safely mix numeric/text cols
    ) %>%
    dplyr::filter(!is.na(val)) %>%
    dplyr::group_by(SimulationName, ref_var) %>%
    dplyr::slice_max(.temp_date, n = 1, with_ties = FALSE) %>%
    dplyr::ungroup()
  
  # ------------------------------------------------------------------
  # 4. DISCREPANCY CHECK & WARNINGS
  # ------------------------------------------------------------------
  if (length(ref_vars) > 1) {
    discrepancies <- max_dates %>%
      dplyr::group_by(SimulationName) %>%
      dplyr::summarise(
        min_date = min(.temp_date, na.rm = TRUE),
        max_date = max(.temp_date, na.rm = TRUE),
        n_distinct_dates = dplyr::n_distinct(.temp_date),
        .groups = "drop"
      ) %>%
      dplyr::filter(n_distinct_dates > 1)
    
    if (nrow(discrepancies) > 0) {
      for (i in seq_len(nrow(discrepancies))) {
        warning(sprintf(
          "Staggered Harvest in '%s': Max dates for reference variables span from %s to %s. Flag '%s' applied to multiple dates.",
          discrepancies$SimulationName[i],
          format(discrepancies$min_date[i], "%Y-%m-%d"),
          format(discrepancies$max_date[i], "%Y-%m-%d"),
          new_col_value
        ), call. = FALSE)
      }
    }
  }
  
  # ------------------------------------------------------------------
  # 5. BUILD LOOKUP AND INJECT
  # ------------------------------------------------------------------
  harvest_lookup <- max_dates %>%
    dplyr::select(SimulationName, Clock.Today) %>%
    dplyr::distinct() %>%
    dplyr::mutate(.is_harvest_target = TRUE)
  
  if (!new_col_name %in% names(df)) {
    df <- df %>% dplyr::mutate(!!new_col_name := NA_character_)
  }
  
  df_final <- df %>%
    dplyr::left_join(harvest_lookup, by = c("SimulationName", "Clock.Today")) %>%
    dplyr::mutate(
      !!new_col_name := dplyr::if_else(
        !is.na(.is_harvest_target), 
        new_col_value, 
        .data[[new_col_name]]
      )
    ) %>%
    dplyr::select(-.is_harvest_target)
  
  message(sprintf("Successfully injected '%s' into '%s' across %d unique dates based on: [%s]", 
                  new_col_value, new_col_name, nrow(harvest_lookup), paste(ref_vars, collapse = ", ")))
  
  return(df_final)
}