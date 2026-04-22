#' Extract and Code Phenological Stage Observation Data
#'
#' @description
#' Transforms a raw dataset of reached phenological stages by assigning specific 
#' numeric codes to primary wheat development stages. It standardizes the date 
#' column and subsets the output to essential variables for simulation tracking.
#'
#' @details
#' The function uses pattern matching (ignoring case) to assign standard numeric 
#' codes to key growth stages:
#' * **Emerging** = 3
#' * **StemElongating** = 6
#' * **Flowering** = 8
#' 
#' Any stage not matching these three primary categories is safely assigned 
#' `NA_real_` to maintain strict numeric (double) typing in the output column.
#'
#' @param df_StageReached A data.frame containing the observed stages. 
#'   Must include `SimulationName`, `DateReached`, and `StageName`.
#' @param var_name Character. The desired column name for the newly created 
#'   numeric stage codes.
#'
#' @return A data.frame containing `SimulationName`, `Date`, and the dynamically 
#'   named numeric stage column defined by `var_name`.
#'
#' @importFrom dplyr select rename mutate case_when
#' @importFrom rlang `!!` `:=`
#' @export
doStageObsData <- function(df_StageReached, var_name) {
  
  require(dplyr)
  require(rlang)
  
  df_stg <- df_StageReached
  vr_nm <- var_name
  
  df <- df_stg %>%
    dplyr::select(SimulationName, DateReached, StageName) %>%
    dplyr::rename(Date = DateReached) %>%
    
    # --- CONDITIONAL MUTATE ---
    dplyr::mutate(
      !!vr_nm := dplyr::case_when(
        grepl("Emerging", StageName, ignore.case = TRUE) ~ 3,       # Assigning DOUBLE (Numeric)
        grepl("StemElongating", StageName, ignore.case = TRUE) ~ 6, # Assigning DOUBLE (Numeric)
        grepl("Flowering", StageName, ignore.case = TRUE) ~ 8,      # Assigning DOUBLE (Numeric)
        TRUE ~ NA_real_                                             # Assigning DOUBLE NA (Numeric)
      ) 
    ) %>%
    dplyr::select(SimulationName, Date, !!vr_nm)  
  
  return(df)
}