#!/bin/bash
set -eu

workflow="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../APSIM.Workflow" && pwd)/workflow.sh"
test_dir="$(mktemp -d)"
mkdir -p "$test_dir/Tests/A folder"
cd "$test_dir"

cleanup() {
  rm -f -- "Tests/A folder/Example.db" metadata.txt local.stdout.txt collector-called
  rmdir "Tests/A folder" Tests
  rmdir "$test_dir"
}
trap cleanup EXIT

sudo() {
  case "$*" in
    *postats2-collector*)
      touch collector-called
      return "${COLLECTOR_EXIT:-0}"
      ;;
    *)
      if [[ "$SIMULATION_MODE" != missing && "$SIMULATION_MODE" != stale ]]; then
        printf 'new database' > "Tests/A folder/Example.db"
      fi
      if [[ "$SIMULATION_MODE" == failed ]]; then
        return 42
      fi
      ;;
  esac
}
bc() { echo 0; }
export -f sudo bc
export Path="/wd/Tests/A folder/Example.apsimx" PR_NUMBER=123 FULL_COMMIT_HASH=abc AUTHOR=test TIME=now COMMIT_SHA=abc

run_workflow() {
  local result=0
  bash "$workflow" run_00001 || result=$?
  if [[ "$result" != "$1" ]]; then
    echo "Expected exit code $1, got $result for $SIMULATION_MODE" >&2
    exit 1
  fi
  test -f collector-called
  rm -f -- collector-called
}

export SIMULATION_MODE=success
run_workflow 0
test "$(cat "Tests/A folder/Example.db")" = "new database"

export SIMULATION_MODE=failed
run_workflow 42
test "$(cat "Tests/A folder/Example.db")" = "new database"

export SIMULATION_MODE=stale
touch -t 200001010000 "Tests/A folder/Example.db"
run_workflow 0
test "$(cat "Tests/A folder/Example.db")" = "new database"

rm -f -- "Tests/A folder/Example.db"
export SIMULATION_MODE=missing
run_workflow 1
grep -q "Database not found" local.stdout.txt

export SIMULATION_MODE=success COLLECTOR_EXIT=7
run_workflow 7
test -f "Tests/A folder/Example.db"

export SIMULATION_MODE=failed
run_workflow 42
test -f "Tests/A folder/Example.db"

export Path="/invalid/Example.apsimx" SIMULATION_MODE=missing COLLECTOR_EXIT=0
run_workflow 1
grep -q "Database not found" local.stdout.txt

echo "Workflow database checks passed."
