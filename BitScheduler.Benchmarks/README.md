# BitScheduler Benchmarks

This project contains BenchmarkDotNet benchmarks for `BitDay` reserve/free operations.

## Current Benchmarks

- `ReserveAndFreeSequentialSingleBitDay`
- `ReserveAndFreeAlternatingSingleBitDay`
- `ReserveThenFreeBatchedSingleBitDay`
- `ReserveAndFreeAcrossRollingDays`

Each benchmark runs with these parameter sets:

- `OperationCount`: `10`, `100`, `1_000`, `10_000`, `100_000`
- `SlotLength`: `1`, `2`, `4`

## Run

From the repo root:

```powershell
 dotnet run -c Release --project .\BitScheduler.Benchmarks\BitScheduler.Benchmarks.csproj
```

## Notes

- Use `Release` mode for meaningful results.
- BenchmarkDotNet will generate detailed reports under `BenchmarkDotNet.Artifacts`.
- The benchmark project is kept separate from the application projects so performance code does not mix with production code.
