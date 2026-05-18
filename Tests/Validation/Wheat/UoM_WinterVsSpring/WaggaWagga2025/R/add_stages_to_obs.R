#' Merge Wide Phenology Stage Dates into Observed Data
#'
#' @description
#' Transforms a wide-format phenology dataframe (with APSIM stage dates as columns) 
#' into a long format. Maps specific APSIM stage names to their corresponding numeric 
#' values, standardizes dates, and securely appends these discrete event rows 
#' to the continuous observations dataframe.
#'
#' @details
#' The mapping dictionary enforces strict numeric values based on APSIM standards:
#' Emerging = 3, LeavesInitiating = 4, SpikeletsDifferentiating = 5, 
#' StemElongating = 6, Heading = 7, Flowering = 8, GrainFilling = 10.
#'
#' @param df_obs Data frame. The main observations containing continuous data.
#' @param df_pheno Data frame. Wide format containing interpolated/Haun phenology dates.
#' @param new_var_name Character. The name of the new column to hold the numeric stage 
#'   values (e.g., "Wheat.Phenology.Stage").
#'
#' @return A unified data frame sorted chronologically by SimulationName.
#'
#' @importFrom dplyr filter mutate select bind_rows arrange case_when
#' @importFrom tidyr pivot_longer
#' @importFrom stringr str_detect
#' @importFrom lubridate parse_date_time
#' @importFrom rlang `:=` sym
#' @export
add_stages_to_obs <- function(df_obs, df_pheno, new_var_name) {
  
  require(dplyr)
  require(tidyr)
  require(stringr)
  require(lubridate)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!"SimulationName" %in% names(df_obs) || !"SimulationName" %in% names(df_pheno)) {
    stop("CRITICAL: Both data frames must contain a 'SimulationName' column.")
  }
  
  if (!"Clock.Today" %in% names(df_obs)) {
    stop("CRITICAL: 'df_obs' must contain a 'Clock.Today' column.")
  }
  
  # ------------------------------------------------------------------
  # 2. RESHAPE WIDE PHENO TO LONG FORMAT
  # ------------------------------------------------------------------
  df_pheno_long <- df_pheno %>%
    tidyr::pivot_longer(
      cols = -SimulationName, 
      names_to = "Stage_String",
      values_to = "Clock.Today",
      values_transform = list(Clock.Today = as.character) 
    ) %>%
    dplyr::filter(!is.na(Clock.Today), Clock.Today != "")
  
  # ------------------------------------------------------------------
  # 3. MAP TO APSIM NUMERIC VALUES & FORMAT DATES
  # ------------------------------------------------------------------
  df_pheno_mapped <- df_pheno_long %>%
    dplyr::mutate(
      !!new_var_name := dplyr::case_when(
        stringr::str_detect(Stage_String, "Emerging") ~ 3,
        stringr::str_detect(Stage_String, "LeavesInitiating") ~ 4,
        stringr::str_detect(Stage_String, "SpikeletsDifferentiating") ~ 5,
        stringr::str_detect(Stage_String, "StemElongating") ~ 6,
        stringr::str_detect(Stage_String, "Heading") ~ 7,
        stringr::str_detect(Stage_String, "Flowering") ~ 8,
        stringr::str_detect(Stage_String, "GrainFilling") ~ 10,
        TRUE ~ NA_real_
      )
    ) %>%
    dplyr::filter(!is.na(!!rlang::sym(new_var_name))) %>%
    dplyr::mutate(
      .temp_date = suppressWarnings(
        lubridate::parse_date_time(Clock.Today, orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd"))
      ),
      Clock.Today = as.Date(.temp_date)
    ) %>%
    dplyr::select(SimulationName, Clock.Today, !!rlang::sym(new_var_name))
  
  # ------------------------------------------------------------------
  # 4. HARMONIZE CLASSES & BIND TO OBSERVED DATA
  # ------------------------------------------------------------------
  # THE FIX: df_obs likely holds Clock.Today as a character string.
  # We MUST convert it to a strict Date object so bind_rows doesn't crash 
  # and arrange() mathematically sorts by time instead of alphabetically by string.
  df_obs_safe <- df_obs %>%
    dplyr::mutate(
      Clock.Today = as.Date(suppressWarnings(
        lubridate::parse_date_time(as.character(Clock.Today), orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd"))
      ))
    )
  
  df_combined <- dplyr::bind_rows(df_obs_safe, df_pheno_mapped) %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  message(sprintf("Successfully appended %d numeric phenology stages into '%s'.", 
                  nrow(df_pheno_mapped), new_var_name))
  
  return(df_combined)
}