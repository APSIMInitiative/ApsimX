#' Inject Harvest Stage into Existing Observations (Universal Pattern-Matching Master)
#'
#' @description
#' Identifies the final measurement date for one or more reference variables or column string patterns 
#' for each simulation, and injects a target stage value (e.g., "HarvestRipe") into a specified tracking 
#' column on those exact operational data rows.
#'
#' @details
#' **Dynamic Pattern Expansion:** The \code{ref_vars} parameter can accept explicit column names or sub-string 
#' patterns (e.g., \code{"Grain.Wt"}). The function dynamically scans the incoming dataset using a case-insensitive 
#' regex search and expands the reference list to target all matching columns (e.g., \code{"Wheat.Grain.Wt"}). 
#'
#' **Discrepancy Validation:** If the expanded reference tracks find conflicting terminal measurement dates 
#' within the same simulation run, the function triggers a descriptive warning pinpointing the exact 
#' data frame variance, helping you debug staggered harvest recording times.
#'
#' @param df Data frame containing the final formatted observations.
#' @param ref_vars Character vector. Full column names or string patterns used to find the last active 
#'   measurements (e.g., \code{"Grain.Wt"} or \code{c("Grain.Wt", "AboveGround.Wt")}).
#' @param new_col_name Character. The name of the target column to hold the stage name entry.
#' @param new_col_value Character. The stage name value to assign (e.g., \code{"HarvestRipe"}).
#'
#' @return A data frame with normalized calendar records and updated harvest stage markers.
#' @export
#'
#' @examples
#' \dontrun{
#' df_final <- add_harv_into_obs(
#'   df = df_final_observed,
#'   ref_vars = c("Grain.Wt"), 
#'   new_col_name = "Wheat.Phenology.CurrentStageName",
#'   new_col_value = "HarvestRipe"
#' )
#' }
add_harv_into_obs <- function(df, ref_vars, new_col_name, new_col_value) {
  
  # ---- 1. DYNAMIC STRING PATTERN MATCHING EXPANDER ----
  if (is.null(ref_vars) || length(ref_vars) == 0) {
    stop("Error [add_harv_into_obs]: 'ref_vars' parameter cannot be empty or null.")
  }
  
  # Scan column names using a flexible case-insensitive regex pattern builder
  matched_vars <- unique(unlist(lapply(ref_vars, function(pat) {
    # Escape any literal periods so "Grain.Wt" means "Grain.Wt" and not any character
    escaped_pat <- gsub("\\.", "\\.", pat)
    grep(escaped_pat, names(df), ignore.case = TRUE, value = TRUE)
  })))
  
  if (length(matched_vars) == 0) {
    stop(paste("Error [add_harv_into_obs]: No columns in the dataset matched the provided string pattern(s):", 
               paste(ref_vars, collapse = ", ")))
  }
  
  # ---- 2. DEFENSIVE STRUCTURAL INTEGRITY CHECKS ----
  if (!"SimulationName" %in% names(df) || !"Clock.Today" %in% names(df)) {
    stop("Error [add_harv_into_obs]: The dataset 'df' must contain 'SimulationName' and 'Clock.Today' tracking keys.")
  }
  if (is.null(new_col_name) || length(new_col_name) != 1 || new_col_name == "") {
    stop("Error [add_harv_into_obs]: 'new_col_name' must be a valid, single character string.")
  }
  
  # ---- 3. ISOLATE AND RECAST SYSTEM DATETIMES ----
  df_dates <- df %>%
    dplyr::select(SimulationName, Clock.Today, dplyr::all_of(matched_vars)) %>%
    dplyr::filter(!is.na(Clock.Today), stringr::str_trim(as.character(Clock.Today)) != "") %>%
    dplyr::mutate(
      .temp_date = suppressWarnings(
        lubridate::parse_date_time(
          as.character(Clock.Today), 
          orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd", "dmy HM")
        )
      )
    ) %>%
    dplyr::filter(!is.na(.temp_date)) # Clear out rows that completely failed date parsing
  
  # ---- 4. SCAN TERMINAL HARVEST MATRIX PER COMPONENT ----
  max_dates <- df_dates %>%
    tidyr::pivot_longer(
      cols = dplyr::all_of(matched_vars),
      names_to = "ref_var",
      values_to = "val",
      values_transform = list(val = as.character) # Safely allows merging mixed numeric/character columns
    ) %>%
    dplyr::filter(!is.na(val), stringr::str_trim(val) != "", !grepl("na", val, ignore.case = TRUE)) %>%
    dplyr::group_by(SimulationName, ref_var) %>%
    dplyr::slice_max(.temp_date, n = 1, with_ties = FALSE) %>%
    dplyr::ungroup()
  
  if (nrow(max_dates) == 0) {
    warning("Warning [add_harv_into_obs]: Every matched reference column contains only missing/NA fields. No rows modified.", call. = FALSE)
    return(df)
  }
  
  # ---- 5. HARVEST CHRONOLOGY CONFIRMED LOOKUPS ----
  if (length(matched_vars) > 1) {
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
          "Staggered Harvest Target inside '%s': Reference parameters span multiple final dates (%s to %s). Overwrite '%s' mapped across all boundary ends.",
          discrepancies$SimulationName[i],
          format(discrepancies$min_date[i], "%Y-%m-%d"),
          format(discrepancies$max_date[i], "%Y-%m-%d"),
          new_col_value
        ), call. = FALSE)
      }
    }
  }
  
  # ---- 6. INJECT LABELS VIA HASH-KEYS MAPS ----
  harvest_lookup <- max_dates %>%
    dplyr::select(SimulationName, Clock.Today) %>%
    dplyr::distinct() %>%
    dplyr::mutate(.is_harvest_target = TRUE)
  
  if (!new_col_name %in% names(df)) {
    df[[new_col_name]] <- NA_character_
  }
  
  df_final <- df %>%
    dplyr::left_join(harvest_lookup, by = c("SimulationName", "Clock.Today")) %>%
    dplyr::mutate(
      !!new_col_name := dplyr::if_else(
        !is.na(.is_harvest_target), 
        new_col_value, 
        as.character(.data[[new_col_name]])
      )
    ) %>%
    dplyr::select(-.is_harvest_target)
  
  message(sprintf("Success [add_harv_into_obs]: Injected '%s' label into column '%s' across %d unique row coordinates.\n -> Pattern expanded: [%s] matched variables: (%s)", 
                  new_col_value, new_col_name, nrow(harvest_lookup), 
                  paste(ref_vars, collapse = ", "), paste(matched_vars, collapse = ", ")))
  
  return(df_final)
}