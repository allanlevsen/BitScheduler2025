# BitScheduler Benchmarks

This project contains BenchmarkDotNet benchmarks for `BitDay` reserve/free operations and EF Core-backed schedule persistence.

## Current Benchmarks

### In-memory `BitDay`

- `ReserveAndFreeSequentialSingleBitDay`
- `ReserveAndFreeAlternatingSingleBitDay`
- `ReserveThenFreeBatchedSingleBitDay`
- `ReserveAndFreeAcrossRollingDays`

### Database read/write

- `LoadScheduleData_FromSqlite`
- `SaveScheduleData_InsertNewResourceRangeAsync`
- `SaveScheduleData_UpdateExistingResourceRangeAsync`

`BitDay` benchmarks run with these parameter sets:

- `OperationCount`: `10`, `100`, `1_000`, `10_000`, `100_000`
- `SlotLength`: `1`, `2`, `4`

Database benchmarks run with these parameter sets:

- `RequestedDayCount`: `7`, `30`, `180`

## Run

From the repo root:

```powershell
 dotnet run -c Release --project .\BitScheduler.Benchmarks\BitScheduler.Benchmarks.csproj
```

## Notes

- Use `Release` mode for meaningful results.
- BenchmarkDotNet will generate detailed reports under `BenchmarkDotNet.Artifacts`.
- Database benchmarks use an isolated SQLite database per iteration so reads and writes hit a real EF Core persistence path without depending on a shared external PostgreSQL instance.
- The benchmark project is kept separate from the application projects so performance code does not mix with production code.
