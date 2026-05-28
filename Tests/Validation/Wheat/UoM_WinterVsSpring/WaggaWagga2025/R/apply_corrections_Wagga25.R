#' Apply Date Corrections to Nested Biomass Dataframes (Wagga 25 Specific)
#'
#' @export
apply_corrections_Wagga25 <- function(df_tbl, df_pheno_final, vars_stage_6, vars_stage_8) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' is required.")
  if (!requireNamespace("tidyr", quietly = TRUE)) stop("Package 'tidyr' is required.")
  if (!requireNamespace("purrr", quietly = TRUE)) stop("Package 'purrr' is required.")
  
  # 1. Create the Date Lookup Table 
  date_lookup <- df_pheno_final %>%
    dplyr::filter(Wheat.Phenology.Stage %in% c(6, 8, "6", "8", "6.0", "8.0")) %>%
    dplyr::select(SimulationName, Date = Clock.Today, Wheat.Phenology.Stage) %>%
    dplyr::mutate(
      PhenoDate = paste0("PhenoDate_", round(as.numeric(Wheat.Phenology.Stage)))
    ) %>%
    dplyr::select(-Wheat.Phenology.Stage) %>%
    dplyr::distinct(SimulationName, PhenoDate, .keep_all = TRUE) %>%
    tidyr::pivot_wider(names_from = PhenoDate, values_from = Date)
  
  # 2. Apply Wagga 25 specific corrections
  df_tbl <- df_tbl %>%
    dplyr::mutate(
      data = purrr::map2(
        .x = data,
        .y = df_name,
        .f = function(df, nm) {
          
          # Patch missing dates for Groups 6 and 8
          if (nm %in% c(vars_stage_6, vars_stage_8)) {
            
            df <- df %>% dplyr::left_join(date_lookup, by = "SimulationName")
            
            if (nm %in% vars_stage_6 && "PhenoDate_6" %in% names(df)) {
              df <- df %>% dplyr::mutate(Date = as.Date(PhenoDate_6))
            } else if (nm %in% vars_stage_8 && "PhenoDate_8" %in% names(df)) {
              df <- df %>% dplyr::mutate(Date = as.Date(PhenoDate_8))
            }
            
            df <- df %>%
              dplyr::select(-dplyr::any_of(c("PhenoDate_6", "PhenoDate_8"))) %>%
              dplyr::filter(!is.na(Date)) 
          }
          
          # ---> (If you ever find a weird sensor typo in Wagga25 data later, 
          # ---> you add that bespoke fix right here!)
          
          return(df)
        }
      )
    )
  
  message("Success: Applied Wagga 2025 specific data corrections.")
  return(df_tbl)
}