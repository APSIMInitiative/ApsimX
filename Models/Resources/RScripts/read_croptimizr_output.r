
objectives <- c()
for (rep in res$nlo) {
  objectives <- c(objectives, rep$objective)
}
optimal_index <- which.min(objectives)

df <- NULL
for (i in 1:length(res$nlo)) {
  row <- res$nlo[[i]]
  initial <- row$x0
  solution <- row$solution
  msg <- row$message
  objective <- row$objective
  iterations <- row$iterations
  
  vals <- c()
  for (j in 1:length(param_names)) {
    vals <- c(vals, initial[j], solution[j])
  }
  rowdata <- c(i, i == optimal_index, objective, iterations, vals, msg)
  df <- rbind(df, rowdata)
}

cols <- c('Repetition', 'Is Optimal', 'Objective Function Value', 'Number of Iterations')
for (param in param_names) {
  cols <- c(cols, paste(param, 'Initial'))
  cols <- c(cols, paste(param, 'Final'))
}
cols <- c(cols, 'Message')
colnames(df) <- cols
write.table(df, row.names = F, col.names = T, sep = ',')
