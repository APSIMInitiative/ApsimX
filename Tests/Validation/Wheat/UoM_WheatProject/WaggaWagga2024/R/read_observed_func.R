read_observed_func <- function(file_path, SheetName, VarName, NewVarName, UnitCorrect) {
  
  # ---- SANITIZE SHEET ARGUMENT ----
  # Must be length 1 and not NA/empty
  if (is.null(SheetName) || length(SheetName) != 1 || is.na(SheetName) || SheetName == "") {
    stop(paste("Invalid sheet name in metadata:", SheetName))
  }
  
  # ---- READ THE SHEET ----
  df <- readxl::read_excel(
    path = file_path,
    sheet = SheetName,
    col_types = "text"
  )
  
  # ---- DATE COLUMN DETECTION ----
  date_col <- names(df)[grepl("date", names(df), ignore.case = TRUE)][1]
  if (is.na(date_col)) {
    stop(paste("No date column found in sheet:", SheetName))
  }
  
  parse_any_date <- function(x) {
    suppressWarnings({
      nums <- suppressWarnings(as.numeric(x))
      if (!all(is.na(nums))) {
        out <- as.Date(nums, origin = "1899-12-30")
        if (!all(is.na(out))) return(out)
      }
      formats <- c("%d/%m/%y", "%d/%m/%Y", "%Y-%m-%d")
      for (fmt in formats) {
        out <- suppressWarnings(as.Date(x, format = fmt))
        if (!all(is.na(out))) return(out)
      }
      suppressWarnings(as.Date(x))
    })
  }
  
  df <- df %>%
    mutate(
      Date      = parse_any_date(.data[[date_col]]),
      Block     = as.factor(`!Block`),
      Cultivar  = as.factor(`!Cultivar`),
      !!NewVarName := as.numeric(.data[[VarName]])
    ) %>%
    select(Date, Cultivar, Block, all_of(NewVarName)) %>%
    group_by(Date, Cultivar) %>%
    summarise(
      across(
        where(is.numeric),
        ~ replace(mean(.x, na.rm = TRUE) * UnitCorrect,
                  is.nan(mean(.x, na.rm = TRUE)), NA)
      ),
      .groups = "drop"
    )
  
  return(df)
}
