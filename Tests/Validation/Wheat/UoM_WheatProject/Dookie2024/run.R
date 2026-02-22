# run.R

# Load the package
library(targets)

# Fetch the directory of this current script and set it as the working directory
setwd(dirname(rstudioapi::getActiveDocumentContext()$path))


# -------------------------------------------------------------------
# 1. INSPECT THE PIPELINE
# -------------------------------------------------------------------
# Check for any errors in your _targets.R without running the data processing
tar_manifest()

# Visualize the dependency graph (opens in your Viewer pane)
tar_visnetwork()

# -------------------------------------------------------------------
# 2. RUN THE PIPELINE
# -------------------------------------------------------------------
# Execute the pipeline. `{targets}` will automatically skip anything 
# that is already up-to-date.
tar_make()

# -------------------------------------------------------------------
# 3. REVIEW THE RESULTS
# -------------------------------------------------------------------
# Check the status of your pipeline (e.g., what is built, what is outdated)
tar_visnetwork() 

# Load a specific target directly into your current R environment to inspect it
tar_load(df_eva)
head(df_eva)

# Alternatively, read a target's value and assign it to a new variable
final_output_path <- tar_read(output_csv)
print(final_output_path)

# -------------------------------------------------------------------
# 4. MAINTENANCE (Optional)
# -------------------------------------------------------------------
# See which targets are out of date (will run on the next tar_make())
tar_outdated()

# If you ever need to force a target to rerun, invalidate it:
# tar_invalidate(df_wwhi)