#' Generate Synthetic Observation Data from Wide Master Timeline
#'
#' @description
#' A specialized workflow function that transforms consolidated, wide-format APSIM stage 
#' inputs into a vertical observation data frame. It melts the wide stage columns down 
#' and maps the text-based APSIM variables back to their standard numeric phenology codes.
#'
#' @details
#' **Single Source of Truth:** By restricting this function to ingest the finalized 
#' \code{df_haunBased} data frame as its exclusive input, it guarantees that the synthetic 
#' observations generated for \code{Observed.xlsx} perfectly match the forced inputs parameterized 
#' in the simulation.
#' 
#' **APSIM-X Code Mapping Matrix:**
#' \itemize{
#'   \item \code{Emerging} = 3
#'   \item \code{LeavesInitiating (Double Ridge)} = 4
#'   \item \code{SpikeletsDifferentiating (Terminal Spikelet)} = 5
#'   \item \code{StemElongating} = 6
#'   \item \code{Heading} = 7
#'   \item \code{Flowering} = 8
#'   \item \code{GrainFilling} = 10
#' }
#'
#' @param df_haunBased Data frame. The wide-format, prioritized APSIM stage inputs timeline table.
#' @param var_name Character. The desired output variable name for the mapped numeric column 
#'    injected into the observation dataset (e.g., \code{"Wheat.Phenology.Stage"}).
#'
#' @return A tidy tibble reduced strictly to: \code{SimulationName}, \code{Date}, 
#'   and the dynamically named target column specified by \code{var_name}.
#' @export
#'
#' @examples
#' \dontrun{
#' df_stages_Observ <- create_phenoData_for_obs(
#'   df_haunBased = df_apsimStageInput_haunBased, 
#'   var_name = "Wheat.Phenology.Stage"
#' )
#' }
create_phenoData_for_obs <- function(df_haunBased, var_name) {
  
  # ---- 1. DEFENSIVE STRUCTURAL INTEGRITY CHECKS ----
  if (missing(df_haunBased) || is.null(df_haunBased)) {
    stop("Error [create_phenoData_for_obs]: Input argument 'df_haunBased' is missing or null.")
  }
  if (!"SimulationName" %in% names(df_haunBased)) {
    stop("Error [create_phenoData_for_obs]: 'df_haunBased' is missing the mandatory 'SimulationName' anchor column.")
  }
  if (is.null(var_name) || length(var_name) != 1 || var_name == "") {
    stop("Error [create_phenoData_for_obs]: 'var_name' argument must be a valid, single character string.")
  }
  if (ncol(df_haunBased) < 2) {
    stop("Error [create_phenoData_for_obs]: 'df_haunBased' contains no stage columns to pivot.")
  }
  
  # ---- 2. PIVOT WIDE TIMELINE TO TIDY LONG OBSERVATIONS ----
  # Melt columns down to a vertical frame, ignoring SimulationName
  df_long <- df_haunBased %>%
    tidyr::pivot_longer(
      cols = -SimulationName,
      names_to = "StageName",
      values_to = "DateStr"
    ) %>%
    # Rigorously strip out any stages that didn't get a valid date calculated
    dplyr::filter(!is.na(DateStr), 
                  DateStr != "NA", 
                  stringr::str_trim(DateStr) != "")
  
  # ---- 3. MAP DEVELOPMENT PHASES TO APSIM NUMERIC CODES ----
  df_final <- df_long %>%
    dplyr::mutate(
      # Standardize incoming date strings safely to R Date classes across mixed format inputs
      Date = as.Date(DateStr, tryFormats = c("%d-%m-%Y", "%Y-%m-%d", "%Y/%m/%d")),
      
      # Execute type-safe, double-precision numeric mapping via regex matching
      !!var_name := dplyr::case_when(
        grepl("Emerging", StageName, ignore.case = TRUE)                 ~ 3,
        grepl("LeavesInitiating", StageName, ignore.case = TRUE)         ~ 4, # Double Ridge
        grepl("SpikeletsDifferentiating", StageName, ignore.case = TRUE) ~ 5, # Terminal Spikelet
        grepl("StemElongating", StageName, ignore.case = TRUE)           ~ 6,
        grepl("Heading", StageName, ignore.case = TRUE)                  ~ 7,
        grepl("Flowering", StageName, ignore.case = TRUE)                ~ 8,
        grepl("GrainFilling", StageName, ignore.case = TRUE)             ~ 10,
        TRUE                                                             ~ NA_real_
      )
    ) %>%
    # Clean drop for any rows that did not map to your active physiological scale components
    dplyr::filter(!is.na(.data[[var_name]])) %>%
    
    # ---- 4. OUTPUT SELECTION & SEQUENCING ----
  dplyr::select(SimulationName, Date, !!var_name) %>%
    dplyr::arrange(SimulationName, Date)
  
  # ---- 5. PIPELINE LOGGER NOTIFICATION ----
  message(sprintf("Success [create_phenoData_for_obs]: Mapped %d wide timeline rows into vertical '%s' codes.", 
                  nrow(df_final), var_name))
  
  return(df_final)
}