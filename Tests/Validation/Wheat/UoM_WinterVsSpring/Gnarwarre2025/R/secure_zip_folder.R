#' Zip a Folder with Password Protection via OS Command
#'
#' @param input_folder Character. The path to the folder you want to zip.
#' @param output_zip Character. The target file path (e.g., "Observed.zip").
#' @param pass_file Character. Path to the git-ignored text file containing the password.
#'
#' @return The path to the generated zip file.
#' @export
secure_zip_folder <- function(input_folder, output_zip, pass_file) {
  
  # 1. Defensive Checks
  if (!dir.exists(input_folder)) stop(sprintf("Folder '%s' does not exist.", input_folder))
  if (!file.exists(pass_file)) stop(sprintf("Password file '%s' not found.", pass_file))
  
  # 2. Read the password securely
  # warn = FALSE prevents R from complaining if the text file lacks a trailing newline
  my_pass <- readLines(pass_file, n = 1, warn = FALSE) 
  my_pass <- trimws(my_pass)
  
  if (nchar(my_pass) == 0) stop("Password file is empty.")
  
  # ------------------------------------------------------------------
  # NEW: CONSOLE METADATA SUMMARY
  # ------------------------------------------------------------------
  # Find all Excel files (supports .xlsx, .xls, .xlsm)
  excel_files <- list.files(input_folder, pattern = "\\.xls[mx]?$", full.names = TRUE)
  
  if (length(excel_files) > 0) {
    # Extract file metadata
    f_info <- file.info(excel_files)
    f_info$name <- basename(rownames(f_info)) # Add file names as a column
    
    # Sort by modification time (oldest to newest)
    f_info <- f_info[order(f_info$mtime), ]
    
    # Get counts and sizes
    num_files <- nrow(f_info)
    
    # Base R trick to format bytes into auto KB/MB strings
    min_size <- format(structure(min(f_info$size), class = "object_size"), units = "auto")
    max_size <- format(structure(max(f_info$size), class = "object_size"), units = "auto")
    
    # Get oldest and newest stats
    oldest <- f_info[1, ]
    newest <- f_info[num_files, ] # the last row
    
    # Format dates to dd-MMM-yyyy
    old_date <- format(oldest$mtime, "%d-%b-%Y")
    new_date <- format(newest$mtime, "%d-%b-%Y")
    
    folder_name <- basename(input_folder)
    
    # Print the requested message
    message(sprintf(
      "%s folder has %d excel files from %s to %s size saved from %s (%s) until %s (%s).",
      folder_name, num_files, min_size, max_size, 
      old_date, oldest$name, new_date, newest$name
    ))
  } else {
    message(sprintf("%s folder has 0 excel files. Proceeding to zip empty folder.", basename(input_folder)))
  }
  # ------------------------------------------------------------------
  
  # 3. Formulate the OS Command for Windows (7-Zip)
  # 'a' stands for 'add to archive'
  # '-p' followed immediately by the password applies the encryption
  
  # Try the standard command name first
  cmd <- "C:/Program Files/7-Zip/7z.exe"
  args <- c("a", paste0("-p", my_pass), output_zip, input_folder)
  
  # 4. Execute and capture output
  sys_run <- suppressWarnings(system2(cmd, args, stdout = TRUE, stderr = TRUE))
  
  # Check if the zip was actually created
  if (!file.exists(output_zip)) {
    stop(paste("Zipping failed. OS Output:\n", paste(sys_run, collapse = "\n")))
  }
  
  return(output_zip)
}