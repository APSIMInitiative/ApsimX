doStageObsData <- function (df_StageReached,
                            df_simNameByCult,
                            var_name) {
  
  require(dplyr)
  
  df_stg <- df_StageReached
  df_nm <- df_simNameByCult
  vr_nm <- var_name
  
  df <- df_stg %>%
    dplyr::select(DateReached, Cultivar, StageName) %>%
    dplyr::rename(Date = DateReached) %>%
    dplyr::inner_join(df_nm, by = "Cultivar") %>%
    
    # --- CONDITIONAL MUTATE ---
    dplyr::mutate(
      !!vr_nm := dplyr::case_when(
        grepl("Emerging", StageName, ignore.case = TRUE) ~ 3,      # Assigning DOUBLE (Numeric)
        grepl("StemElongating", StageName, ignore.case = TRUE) ~ 6, # Assigning DOUBLE (Numeric)
        grepl("Flowering", StageName, ignore.case = TRUE) ~ 8,      # Assigning DOUBLE (Numeric)
        TRUE ~ NA_real_                                             # Assigning CHARACTER (String)
      ) 
    ) %>%
    dplyr::select(Date, Cultivar, !!vr_nm)  
  
  return(df)
}