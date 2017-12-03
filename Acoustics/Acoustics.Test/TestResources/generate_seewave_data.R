library(tuneR)
library(seewave)
library(jsonlite)

test.file <- c("whip bird2.wav")
seewave.version <- info$otherPkgs$seewave$Version

commands <- c(
  'Im(hilbert(a))',
  'env(a, envt="hil")',
  'env(a, envt="abs")',
  'env(a, envt="hil", msmooth=c(10,50))',
  'env(a, envt="abs", msmooth=c(10,50))',
  'env(a, envt="hil", ksmooth=kernel("daniell",10))',
  'env(a, envt="abs", ksmooth=kernel("daniell",10))',
  'env(a, envt="hil", ssmooth=50)',
  'env(a, envt="abs", ssmooth=50)',
  'env(a, envt="hil", norm=TRUE)',
  'env(a, envt="abs", norm=TRUE)',
  'env(a, envt="hil", msmooth=c(10,50), norm=TRUE)',
  'env(a, envt="abs", msmooth=c(10,50), norm=TRUE)',
  'env(a, envt="hil", ksmooth=kernel("daniell",10), norm=TRUE)',
  'env(a, envt="abs", ksmooth=kernel("daniell",10), norm=TRUE)',
  'env(a, envt="hil", ssmooth=50, norm=TRUE)',
  'env(a, envt="abs", ssmooth=50, norm=TRUE)'

)

run_tests <- function (test.file, commands) {
  a <- readWave(test.file)

  results <- lapply(commands, function(command) {
    data <- eval(parse(text=command))
    
    return(list(
      seewave_version=seewave.version,
      test_file = test.file,
      command = command,
      result = data[1:100]
    ))
  })
}

all.results <- run_tests(test.file, commands)

jsonlite::write_json(all.results, path='seewave_data.json', auto_unbox = TRUE)
