
df <- NULL
for (i in 1:length(nlo)) {
  row <- nlo[[i]]
  initial <- row$x0
  solution <- row$solution
  msg <- row$message
  objective <- row$objective
  iterations <- row$iterations
  df <- rbind(df, c(i, objective, iterations, initial, solution, msg))
}

cols <- c('Repetition', 'Objective Function Value', 'Number of Iterations')
for (param in param_names) {
  cols <- c(cols, paste(param, 'Initial'))
}
for (param in param_names) {
  cols <- c(cols, paste(param, 'Final'))
}
cols <- c(cols, 'Message')
colnames(df) <- cols
write.table(df, row.names = F, col.names = T, sep = ',')