#' Calculate Absolute Nutrient Amounts (Explicit Universal Engine)
#'
#' @description
#' Calculates the absolute mass (e.g., g/m2) of specific tissue nutrients 
#' (like N or WSC) by cross-multiplying explicit organ dry weights by their 
#' concentrations. Dynamically aggregates these explicit pools into a total sum.
#'
#' @details
#' **Zero-Sum NA Protection:** If an observation date is missing concentration data 
#' for ALL specified organs, the aggregate sum will safely return \code{NA} rather 
#' than \code{0}, preventing APSIM-X from reading a false zero-nutrient state.
#'
#' @param df Data frame. The compiled observation data.
#' @param crop_prefix Character. The base crop name prefix (default: \code{"Wheat"}).
#' @param organs Character vector. The exact explicit organ names to search for 
#'   (e.g., \code{c("Leaf.Live", "Leaf.Dead", "Stem.Live", "Spike.Live")}).
#' @param conc_targets Named character vector. The target nutrient names mapping to 
#'   their raw concentration suffixes. E.g., \code{c("N" = "NConc", "WSC" = "WSCc")}.
#' @param mass_suffix Character. The suffix denoting dry weight (default: \code{"Wt"}).
#' @param ag_name Character. The base name for the aggregated variable (default: \code{"Wheat.AboveGround"}).
#' @param divisor Numeric. The scaling factor to convert concentration values to fractions 
#'   before multiplying by mass. Use 100 if concentrations are in % (default), or 1 if g/g.
#'
#' @return A data frame with newly calculated individual nutrient amounts and aggregated totals appended.
#' @export
calc_nutrient_absolute_amounts <- function(df, 
                                           crop_prefix = "Wheat",
                                           organs = c("Leaf.Live", "Leaf.Dead", "Stem.Live", "Spike.Live"),
                                           conc_targets = c("N" = "NConc", "WSC" = "WSCc"),
                                           mass_suffix = "Wt",
                                           ag_name = "Wheat.AboveGround",
                                           divisor = 1) {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df) || !is.data.frame(df) || nrow(df) == 0) {
    stop("Error [calc_nutrient_absolute_amounts]: Main observation dataframe is missing or empty.")
  }
  if (length(organs) == 0) {
    stop("Error [calc_nutrient_absolute_amounts]: Must provide a vector of explicit organs.")
  }
  
  df_out <- df
  total_calcs <- 0
  created_vars <- c() # Tracks all newly generated columns for the final warning log
  
  # ---- 2. DYNAMIC EXPLICIT CROSS-MULTIPLICATION ----
  # Loop through each target nutrient (e.g., "N" then "WSC")
  for (target_nutrient in names(conc_targets)) {
    
    raw_conc_suffix <- conc_targets[[target_nutrient]]
    cols_to_sum <- c() # Tracks successfully mapped columns for the final AboveGround aggregate
    
    for (organ in organs) {
      
      # Construct the EXACT column names using the explicit lego pieces
      mass_col <- paste(crop_prefix, organ, mass_suffix, sep = ".")     # e.g., Wheat.Stem.Live.Wt
      conc_col <- paste(crop_prefix, organ, raw_conc_suffix, sep = ".") # e.g., Wheat.Stem.Live.NConc
      out_col  <- paste(crop_prefix, organ, target_nutrient, sep = ".") # e.g., Wheat.Stem.Live.N
      
      # Only perform the math if BOTH the mass and concentration columns physically exist in this dataset
      if (mass_col %in% names(df_out) && conc_col %in% names(df_out)) {
        
        df_out <- df_out %>%
          dplyr::mutate(
            !!out_col := (.data[[mass_col]] * .data[[conc_col]]) / divisor
          )
        
        cols_to_sum <- c(cols_to_sum, out_col)
        created_vars <- c(created_vars, out_col)
        total_calcs <- total_calcs + 1
        
      } else {
        # If the user requested an organ that doesn't exist in this specific trial, safely pad it with NA
        df_out <- df_out %>% dplyr::mutate(!!out_col := NA_real_)
        cols_to_sum <- c(cols_to_sum, out_col)
        created_vars <- c(created_vars, out_col)
      }
    }
    
    # ---- 3. AGGREGATE SUMMATION (WITH NA SHIELD) ----
    ag_col_out <- paste(ag_name, target_nutrient, sep = ".") # e.g., Wheat.AboveGround.N
    
    df_out <- df_out %>%
      dplyr::mutate(
        !!ag_col_out := dplyr::if_else(
          # CONDITION: Are there any non-NA values to sum in this specific row?
          rowSums(!is.na(dplyr::select(., dplyr::all_of(cols_to_sum)))) > 0,
          
          # TRUE: Perform the sum, ignoring individual NAs
          rowSums(dplyr::select(., dplyr::all_of(cols_to_sum)), na.rm = TRUE),
          
          # FALSE: Every single organ was NA, so the sum must be NA, not 0.
          NA_real_
        )
      )
    
    created_vars <- c(created_vars, ag_col_out)
  }
  
  # ---- 4. COMPLETION NOTIFICATION & EXPLICIT WARNING LOG ----
  message(sprintf("Success [calc_nutrient_absolute_amounts]: Derived %d explicit organ pools and aggregated to '%s'.", 
                  total_calcs, ag_name))
  
  if (length(created_vars) > 0) {
    warning(sprintf(
      "Quality variables created and inserted into Observations: %s", 
      paste(created_vars, collapse = ", ")
    ), call. = FALSE)
  }
  
  return(df_out)
}