#' Merge Wide Phenology Stage Dates into Observed Data
#'
#' @description
#' Transforms a wide-format phenology dataframe (with APSIM stage dates as columns) 
#' into a long format. Maps specific APSIM stage names to their corresponding numeric 
#' values, renames the date column to 'Clock.Today', and securely appends these 
#' discrete event rows to the continuous observations dataframe.
#'
#' @details
#' The mapping dictionary enforces strict numeric values based on APSIM standards:
#' Emerging = 3, LeavesInitiating = 4, SpikeletsDifferentiating = 5, 
#' StemElongating = 6, Heading = 7, Flowering = 8, GrainFilling = 9.
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
      cols = -SimulationName, # Pivot everything except SimulationName
      names_to = "Stage_String",
      values_to = "Clock.Today",
      values_transform = list(Clock.Today = as.character) # Prevent date coercion crashes
    ) %>%
    # Filter out stages that did not occur (missing/empty dates)
    dplyr::filter(!is.na(Clock.Today), Clock.Today != "")
  
  # ------------------------------------------------------------------
  # 3. MAP TO APSIM NUMERIC VALUES & FORMAT DATES
  # ------------------------------------------------------------------
  df_pheno_mapped <- df_pheno_long %>%
    dplyr::mutate(
      # Scan the complex APSIM column string for the exact stage name
      !!new_var_name := dplyr::case_when(
        stringr::str_detect(Stage_String, "Emerging") ~ 3,
        stringr::str_detect(Stage_String, "LeavesInitiating") ~ 4,
        stringr::str_detect(Stage_String, "SpikeletsDifferentiating") ~ 5,
        stringr::str_detect(Stage_String, "StemElongating") ~ 6,
        stringr::str_detect(Stage_String, "Heading") ~ 7,
        stringr::str_detect(Stage_String, "Flowering") ~ 8,
        stringr::str_detect(Stage_String, "GrainFilling") ~ 9,
        TRUE ~ NA_real_
      )
    ) %>%
    
    # Drop rows if a non-stage column was accidentally caught in the pivot
    dplyr::filter(!is.na(!!rlang::sym(new_var_name))) %>%
    
    # Employ the bulletproof date parser to ensure it matches df_obs Date class
    dplyr::mutate(
      .temp_date = suppressWarnings(
        lubridate::parse_date_time(Clock.Today, orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd"))
      ),
      Clock.Today = as.Date(.temp_date)
    ) %>%
    
    # Strip away temporary columns, keeping strictly what is needed for the bind
    dplyr::select(SimulationName, Clock.Today, !!rlang::sym(new_var_name))
  
  # ------------------------------------------------------------------
  # 4. BIND TO OBSERVED DATA & SORT
  # ------------------------------------------------------------------
  # bind_rows elegantly handles the structural difference, padding 
  # variables like Wheat.Grain.Wt with NAs for these specific stage rows.
  df_combined <- dplyr::bind_rows(df_obs, df_pheno_mapped) %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  message(sprintf("Successfully appended %d numeric phenology stages into '%s'.", 
                  nrow(df_pheno_mapped), new_var_name))
  
  return(df_combined)
}