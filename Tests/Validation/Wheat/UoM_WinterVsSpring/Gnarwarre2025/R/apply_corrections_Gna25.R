#' Apply Tailored Corrections to Wide Observation Data
#'
#' @description
#' Applies specific structural corrections to the finalized wide observation dataframe.
#'
#' @export
apply_corrections_Gna25 <- function(df) {
  
  if (!is.data.frame(df)) stop("CRITICAL: Input to apply_corrections_Gna25 is not a dataframe.")
  
  correction_logs <- c()
  
  # ------------------------------------------------------------------
  # 1. CORRECTION: The Duplicate "Spike.Grain" Trap
  # ------------------------------------------------------------------
  duplicate_pairs <- list(
    c(general = "Wheat.Grain.Size",   spike = "Wheat.Spike.Grain.Size"),
    c(general = "Wheat.Grain.Wt",     spike = "Wheat.Spike.Grain.Wt"),
    c(general = "Wheat.Grain.Number", spike = "Wheat.Spike.Grain.Number")
  )
  
  for (pair in duplicate_pairs) {
    if (pair["spike"] %in% names(df) && pair["general"] %in% names(df)) {
      df[[pair["spike"]]] <- NULL
      correction_logs <- c(correction_logs, sprintf(" -> DROPPED   : Redundant '%s' (retained '%s')", pair["spike"], pair["general"]))
      
    } else if (pair["spike"] %in% names(df) && !(pair["general"] %in% names(df))) {
      names(df)[names(df) == pair["spike"]] <- pair["general"]
      correction_logs <- c(correction_logs, sprintf(" -> RENAMED   : '%s' to '%s'", pair["spike"], pair["general"]))
    }
  }
  
  # ------------------------------------------------------------------
  # 2. CORRECTION: Drop Redundant Yield Column
  # ------------------------------------------------------------------
  if ("Yield" %in% names(df)) {
    df$Yield <- NULL
    correction_logs <- c(correction_logs, " -> DROPPED   : Redundant 'Yield' column")
  }
  
  # ------------------------------------------------------------------
  # 3. CORRECTION: Nutrient Concentration Fix (Percentage to Fractional)
  # ------------------------------------------------------------------
  target_cols <- grep("NConc|WSC", names(df), value = TRUE)
  for (col in target_cols) {
    if (is.numeric(df[[col]])) {
      if (any(df[[col]] > 1, na.rm = TRUE)) {
        df[[col]] <- df[[col]] / 100
        correction_logs <- c(correction_logs, sprintf(" -> CONVERTED : '%s' divided by 100", col))
      }
    }
  }
  
  # ------------------------------------------------------------------
  # 4. CORRECTION: Leaf Height Unit Scaling (cm to mm)
  # ------------------------------------------------------------------
  if ("Wheat.Leaf.Height" %in% names(df)) {
    if (is.numeric(df[["Wheat.Leaf.Height"]])) {
      df[["Wheat.Leaf.Height"]] <- df[["Wheat.Leaf.Height"]] * 10
      correction_logs <- c(correction_logs, " -> CONVERTED : 'Wheat.Leaf.Height' multiplied by 10 (cm to mm)")
    }
  }
  
  # ------------------------------------------------------------------
  # 5. CORRECTION: Align Simulation Names with APSIM-X Interface
  # ------------------------------------------------------------------
  if ("SimulationName" %in% names(df)) {
    original_names <- df$SimulationName
    
    # Safely swap "Sow[Month]Cv" to "CvSow[Month]"
    df$SimulationName <- gsub("Sow([A-Za-z]{3})Cv", "CvSow\\1", df$SimulationName)
    
    # If a change actually occurred, throw the massive alarm!
    if (!identical(original_names, df$SimulationName)) {
      
      apsim_warning <- c(
        "",
        "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
        " \U0001F6A8 CRITICAL ALARM: APSIM-X SIMULATION NAME MISMATCH DETECTED \U0001F6A8",
        "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
        " ISSUE   : The raw data uses 'Sow[Month]Cv' (e.g., SowAprCv), but the",
        "           APSIM-X .apsimx file expects 'CvSow[Month]' (e.g., CvSowApr).",
        " ACTION  : The pipeline has automatically forcibly renamed all",
        "           simulations to match the APSIM-X interface.",
        " WARNING : If you ever update the .apsimx file to match the lab data,",
        "           this forced regex correction MUST be removed!",
        "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
        ""
      )
      
      message(paste(apsim_warning, collapse = "\n"))
      
      correction_logs <- c(
        correction_logs, 
        " -> RENAMED   : 'SimulationName' strings swapped (See Critical Alarm above)"
      )
    }
  }
  
  # ------------------------------------------------------------------
  # TRIGGER NOTICES
  # ------------------------------------------------------------------
  if (length(correction_logs) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " \u26A0\uFE0F  TAILORED CORRECTIONS APPLIED TO WIDE OBS DATAFRAME \u26A0\uFE0F ",
      "======================================================================",
      correction_logs,
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    warning("Tailored structural corrections were applied. See console for details.", call. = FALSE)
  }
  
  return(df)
}