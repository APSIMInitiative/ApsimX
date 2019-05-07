# -----------------------------------------------------------------------
# <copyright file="SummaryPresenter.cs" company="APSIM Initiative">
#     Copyright (c) APSIM Initiative
# </copyright>
# -----------------------------------------------------------------------


#' Loads a package, installing it if necessary.
#'
#' @param pkg Name of the package to be installed/loaded.
#' @return Nothing.
getPackage <- function(pkg) {
    if (!pkg %in% rownames(installed.packages())) {
		i = which(!grepl("Program Files", .libPaths()))[1]
		location = ""
        if (is.na(i) || i > length(.libPaths()) || identical(i, integer(0))) {
		    # No lib paths outside of program files exist....
			location = getwd()
		} else {
			location = .libPaths()[i]
		}
		print(paste('Installing package', pkg, 'to location', location))
		install.packages(pkg,repos = "https://cran.csiro.au/", lib = location)
	} else {
		print('Package', pkg, 'is already installed.')
	}
}

args = commandArgs(TRUE)
for (arg in args) {
	getPackage(arg)
}
