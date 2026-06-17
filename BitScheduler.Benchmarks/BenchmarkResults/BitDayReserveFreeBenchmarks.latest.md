# BitDayReserveFreeBenchmarks

Generated: 2026-06-01T17:25:07.6917422-06:00

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8457/25H2/2025Update/HudsonValley2)
Apple Silicon 3.07GHz, 4 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), Arm64 RyuJIT armv8.0-a


```
| Method                                | OperationCount | SlotLength | Mean           | Error        | StdDev       | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |--------------- |----------- |---------------:|-------------:|-------------:|------:|-------:|----------:|------------:|
| ReserveAndFreeAlternatingSingleBitDay | 10             | 1          |       111.6 ns |      0.11 ns |      0.10 ns |  0.99 | 0.0421 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 10             | 1          |       113.1 ns |      0.27 ns |      0.24 ns |  1.00 | 0.0421 |      88 B |        1.00 |
| ReserveThenFreeBatchedSingleBitDay    | 10             | 1          |       135.2 ns |      0.44 ns |      0.35 ns |  1.20 | 0.2370 |     496 B |        5.64 |
| ReserveAndFreeAcrossRollingDays       | 10             | 1          |       370.9 ns |      0.34 ns |      0.28 ns |  3.28 | 1.3466 |    2816 B |       32.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAlternatingSingleBitDay | 10             | 2          |       111.6 ns |      0.40 ns |      0.34 ns |  0.99 | 0.0421 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 10             | 2          |       112.6 ns |      0.10 ns |      0.09 ns |  1.00 | 0.0421 |      88 B |        1.00 |
| ReserveThenFreeBatchedSingleBitDay    | 10             | 2          |       115.4 ns |      0.41 ns |      0.34 ns |  1.02 | 0.1452 |     304 B |        3.45 |
| ReserveAndFreeAcrossRollingDays       | 10             | 2          |       374.0 ns |      1.65 ns |      1.37 ns |  3.32 | 1.3466 |    2816 B |       32.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveThenFreeBatchedSingleBitDay    | 10             | 4          |       105.6 ns |      0.12 ns |      0.11 ns |  0.94 | 0.0994 |     208 B |        2.36 |
| ReserveAndFreeAlternatingSingleBitDay | 10             | 4          |       112.4 ns |      0.12 ns |      0.11 ns |  1.00 | 0.0421 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 10             | 4          |       112.6 ns |      0.11 ns |      0.09 ns |  1.00 | 0.0421 |      88 B |        1.00 |
| ReserveAndFreeAcrossRollingDays       | 10             | 4          |       386.2 ns |      0.80 ns |      0.74 ns |  3.43 | 1.3466 |    2816 B |       32.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveThenFreeBatchedSingleBitDay    | 100            | 1          |       999.6 ns |      1.24 ns |      1.10 ns |  0.83 | 0.2365 |     496 B |        5.64 |
| ReserveAndFreeAcrossRollingDays       | 100            | 1          |     1,100.6 ns |      1.67 ns |      1.48 ns |  0.92 | 1.3466 |    2816 B |       32.00 |
| ReserveAndFreeAlternatingSingleBitDay | 100            | 1          |     1,196.8 ns |      0.73 ns |      0.65 ns |  1.00 | 0.0420 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 100            | 1          |     1,199.3 ns |      1.47 ns |      1.38 ns |  1.00 | 0.0420 |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveThenFreeBatchedSingleBitDay    | 100            | 2          |       948.7 ns |      2.49 ns |      2.21 ns |  0.79 | 0.1450 |     304 B |        3.45 |
| ReserveAndFreeAcrossRollingDays       | 100            | 2          |     1,117.4 ns |      3.24 ns |      2.87 ns |  0.93 | 1.3466 |    2816 B |       32.00 |
| ReserveAndFreeAlternatingSingleBitDay | 100            | 2          |     1,198.3 ns |      1.59 ns |      1.49 ns |  1.00 | 0.0420 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 100            | 2          |     1,202.2 ns |      1.78 ns |      1.66 ns |  1.00 | 0.0420 |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveThenFreeBatchedSingleBitDay    | 100            | 4          |       914.2 ns |      1.29 ns |      1.20 ns |  0.76 | 0.0992 |     208 B |        2.36 |
| ReserveAndFreeAcrossRollingDays       | 100            | 4          |     1,157.9 ns |      3.46 ns |      3.23 ns |  0.96 | 1.3466 |    2816 B |       32.00 |
| ReserveAndFreeAlternatingSingleBitDay | 100            | 4          |     1,200.0 ns |      1.43 ns |      1.19 ns |  1.00 | 0.0420 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 100            | 4          |     1,202.8 ns |      1.75 ns |      1.37 ns |  1.00 | 0.0420 |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAcrossRollingDays       | 1000           | 1          |     8,412.1 ns |      8.71 ns |      6.80 ns |  0.70 | 1.3428 |    2816 B |       32.00 |
| ReserveThenFreeBatchedSingleBitDay    | 1000           | 1          |     8,950.6 ns |     12.69 ns |     11.25 ns |  0.74 | 0.2289 |     496 B |        5.64 |
| ReserveAndFreeAlternatingSingleBitDay | 1000           | 1          |    12,064.3 ns |     17.12 ns |     15.17 ns |  1.00 | 0.0305 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 1000           | 1          |    12,092.9 ns |     19.51 ns |     17.29 ns |  1.00 | 0.0305 |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAcrossRollingDays       | 1000           | 2          |     8,694.8 ns |     22.64 ns |     21.18 ns |  0.72 | 1.3428 |    2816 B |       32.00 |
| ReserveThenFreeBatchedSingleBitDay    | 1000           | 2          |     8,911.6 ns |     27.16 ns |     24.08 ns |  0.74 | 0.1373 |     304 B |        3.45 |
| ReserveAndFreeAlternatingSingleBitDay | 1000           | 2          |    12,052.1 ns |     11.44 ns |     10.14 ns |  1.00 | 0.0305 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 1000           | 2          |    12,065.1 ns |     20.06 ns |     18.76 ns |  1.00 | 0.0305 |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveThenFreeBatchedSingleBitDay    | 1000           | 4          |     8,792.4 ns |     10.15 ns |      9.49 ns |  0.73 | 0.0916 |     208 B |        2.36 |
| ReserveAndFreeAcrossRollingDays       | 1000           | 4          |     8,920.0 ns |     15.49 ns |     13.73 ns |  0.74 | 1.3428 |    2816 B |       32.00 |
| ReserveAndFreeAlternatingSingleBitDay | 1000           | 4          |    12,070.1 ns |     10.59 ns |      9.38 ns |  1.00 | 0.0305 |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 1000           | 4          |    12,093.5 ns |     25.55 ns |     22.65 ns |  1.00 | 0.0305 |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAcrossRollingDays       | 10000          | 1          |    82,229.1 ns |    157.00 ns |    146.85 ns |  0.68 | 1.3428 |    2816 B |       32.00 |
| ReserveThenFreeBatchedSingleBitDay    | 10000          | 1          |    88,685.1 ns |     91.55 ns |     81.15 ns |  0.73 | 0.1221 |     496 B |        5.64 |
| ReserveAndFreeAlternatingSingleBitDay | 10000          | 1          |   120,497.6 ns |    131.16 ns |    122.69 ns |  1.00 |      - |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 10000          | 1          |   120,772.5 ns |     77.31 ns |     72.31 ns |  1.00 |      - |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAcrossRollingDays       | 10000          | 2          |    84,544.1 ns |    131.13 ns |    122.66 ns |  0.70 | 1.3428 |    2816 B |       32.00 |
| ReserveThenFreeBatchedSingleBitDay    | 10000          | 2          |    89,778.4 ns |    389.58 ns |    364.42 ns |  0.74 | 0.1221 |     304 B |        3.45 |
| ReserveAndFreeAlternatingSingleBitDay | 10000          | 2          |   120,566.7 ns |     89.69 ns |     83.90 ns |  1.00 |      - |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 10000          | 2          |   120,732.9 ns |    106.20 ns |     88.68 ns |  1.00 |      - |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAcrossRollingDays       | 10000          | 4          |    87,309.7 ns |     94.13 ns |     73.49 ns |  0.72 | 1.3428 |    2816 B |       32.00 |
| ReserveThenFreeBatchedSingleBitDay    | 10000          | 4          |    90,348.4 ns |    100.39 ns |     93.91 ns |  0.75 |      - |     208 B |        2.36 |
| ReserveAndFreeAlternatingSingleBitDay | 10000          | 4          |   120,911.1 ns |    101.70 ns |     90.15 ns |  1.00 |      - |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 10000          | 4          |   121,050.7 ns |     59.61 ns |     52.84 ns |  1.00 |      - |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAcrossRollingDays       | 100000         | 1          |   826,283.8 ns |  2,810.54 ns |  2,346.93 ns |  0.68 | 0.9766 |    2816 B |       32.00 |
| ReserveThenFreeBatchedSingleBitDay    | 100000         | 1          |   890,649.3 ns |    775.84 ns |    725.72 ns |  0.73 |      - |     496 B |        5.64 |
| ReserveAndFreeSequentialSingleBitDay  | 100000         | 1          | 1,220,386.4 ns | 14,083.40 ns | 13,173.62 ns |  1.00 |      - |      88 B |        1.00 |
| ReserveAndFreeAlternatingSingleBitDay | 100000         | 1          | 1,228,017.8 ns | 12,364.94 ns | 11,566.18 ns |  1.01 |      - |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAcrossRollingDays       | 100000         | 2          |   851,817.4 ns |  1,571.97 ns |  1,470.42 ns |  0.70 | 0.9766 |    2816 B |       32.00 |
| ReserveThenFreeBatchedSingleBitDay    | 100000         | 2          |   891,902.9 ns |  2,755.49 ns |  2,442.67 ns |  0.73 |      - |     304 B |        3.45 |
| ReserveAndFreeAlternatingSingleBitDay | 100000         | 2          | 1,225,175.6 ns | 11,492.50 ns | 10,750.09 ns |  1.00 |      - |      88 B |        1.00 |
| ReserveAndFreeSequentialSingleBitDay  | 100000         | 2          | 1,225,442.9 ns | 14,674.53 ns | 12,253.90 ns |  1.00 |      - |      88 B |        1.00 |
|                                       |                |            |                |              |              |       |        |           |             |
| ReserveAndFreeAcrossRollingDays       | 100000         | 4          |   873,390.8 ns |  2,058.31 ns |  1,824.64 ns |  0.71 | 0.9766 |    2816 B |       32.00 |
| ReserveThenFreeBatchedSingleBitDay    | 100000         | 4          |   904,505.8 ns |  1,636.53 ns |  1,450.74 ns |  0.74 |      - |     208 B |        2.36 |
| ReserveAndFreeSequentialSingleBitDay  | 100000         | 4          | 1,225,114.2 ns | 13,626.74 ns | 11,378.95 ns |  1.00 |      - |      88 B |        1.00 |
| ReserveAndFreeAlternatingSingleBitDay | 100000         | 4          | 1,225,460.8 ns | 12,011.52 ns | 11,235.58 ns |  1.00 |      - |      88 B |        1.00 |

