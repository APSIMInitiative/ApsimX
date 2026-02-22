# R/functions.R

#' Creates two synthetic phenology dates by interpolating a percentage point of the distance
#' between adjacent PCDS stages for each cultivar.
#'
#' @param df_interp_results The data frame output from interpolate_and_find_50pct,
#'                          containing 'Input_DF_Name', 'Cultivar', 'Variable', 'Date_50_Pct'.
#' @param PercNewStages A numeric value (0 to 1) representing the percentage of the distance 
#'                      between adjacent observed dates to calculate the new synthetic date.
#'                      e.g., 0.50 means 50% (midpoint).
#' @return A tidy data frame with Cultivar and the two new synthetic date variables.
#' 
#' 

#df_PCDS_50<- tar_read(df_PCDS_50)
#BtwStgPerc<-tar_read(BtwStgPerc)

create_synthetic_pheno_dates <- function(df_interp_results, BtwStgPerc) {
  
  # Load required packages
  require(dplyr)
  require(tidyr)
  require(lubridate)
  
  # 1. Define the required PCDS dates (assuming these are the names in the 'Variable' column)
  PCDS_VARS_BASE <- c("pcds_3_emergPlants", "pcds_6_flagLeaf", "pcds_8_anthesis")
  
  # 2. Define the NEW synthetic variable names (the required output)
  NEW_VARS <- c(
    "Wheat.Phenology.SpikeletsDifferentiating.DateToProgress",
    "Wheat.Phenology.Heading.DateToProgress"
  )
  
  # 3. Input Validation: Check if PercNewStages is a valid percentage
  if (!is.numeric(BtwStgPerc) || BtwStgPerc < 0 || BtwStgPerc > 100) {
    stop("BtwStgPerc must be a numeric value between 0 and 100")
  }
  
  # 4. Pivot the data to wide format to get all required dates side-by-side
  # df_wide <- df_interp_results %>%
  #   dplyr::filter(Variable %in% PCDS_VARS_BASE) %>%
  #   tidyr::pivot_wider(
  #     id_cols = c(Input_DF_Name, Cultivar),
  #     names_from = Variable,
  #     values_from = Date_50_Pct # Note: This column name should likely be Date_PercInt
  #   )
  
  df_wide <- df_interp_results %>%
    dplyr::select(-Input_DF_Name) %>%
    tidyr::spread(Variable,Date_50_Pct)
  
  # 5. Calculate the synthetic dates based on PercNewStages
  df_synthetic <- df_wide %>%
    dplyr::mutate(
      
      # Calculation 1: Synthetic date between pcds_3 and pcds_6
      !!NEW_VARS[1] := {
        mid_date <- if_else(
          is.na(pcds_3_emergPlants) | is.na(pcds_6_flagLeaf),
          NA_real_,
          # Use lubridate::interval: [Stage 1] + PercNewStages * [Duration]
          (pcds_3_emergPlants %--% pcds_6_flagLeaf) * BtwStgPerc/100 + pcds_3_emergPlants
        )
        as.Date(mid_date, origin = "1970-01-01") # Convert back to Date
      },
      
      # Calculation 2: Synthetic date between pcds_6 and pcds_8
      !!NEW_VARS[2] := {
        mid_date <- if_else(
          is.na(pcds_6_flagLeaf) | is.na(pcds_8_anthesis),
          NA_real_,
          (pcds_6_flagLeaf %--% pcds_8_anthesis) * BtwStgPerc/100 + pcds_6_flagLeaf
        )
        as.Date(mid_date, origin = "1970-01-01") # Convert back to Date
      }
    ) %>%
    
    # 6. Clean up the output
    dplyr::select(
      Input_DF_Name,
      Cultivar,
      !!NEW_VARS[1],
      !!NEW_VARS[2]
    )
  
  return(df_synthetic)
}