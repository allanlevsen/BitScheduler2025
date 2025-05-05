# BitScheduler2025



# Lastest Performance Test Results

=== Correctness Tests ===
Correctness tests passed.

=== Performance Tests ===
Performing 1,000,000 iterations for day slot operations...
Day slot operations elapsed time: 101 ms
Performing 1,000,000 iterations for metadata operations...
Metadata operations elapsed time: 10 ms
=== Empty Weekday Availability Test ===
[No Reservations] Available days count (expected: 31): 31
[After Reserving Day 15] Available days count (expected: 30): 30
Day 15 is correctly not available.
Empty Weekday Availability Test completed.

=== Multiple Weekday Availability Test ===
Initial available days for Tuesday and Thursday with 9:00-11:00 block:
  8/5/2025 - Tuesday
  8/7/2025 - Thursday
  8/12/2025 - Tuesday
  8/14/2025 - Thursday
  8/19/2025 - Tuesday
  8/21/2025 - Thursday
  8/26/2025 - Tuesday
  8/28/2025 - Thursday
Reserved time block on Tuesday, 8/5/2025
Available days after reserving one Tuesday:
  8/12/2025 - Tuesday
  8/14/2025 - Thursday
  8/19/2025 - Tuesday
  8/21/2025 - Thursday
  8/26/2025 - Tuesday
  8/28/2025 - Thursday
The week with the reserved Tuesday is correctly excluded.
Multiple Weekday Availability Test completed.

=== Empty Weekday Availability Performance Test ===
Empty Weekday Performance Test: 100,000 iterations took 161 ms

=== Multiple Weekday Availability Performance Test ===
Multiple Weekday Performance Test: 100,000 iterations took 150 ms

Running Utility Methods Tests...
TimeToBlockIndex(9:00) passed.
BlockIndexToTime(36) passed.
CreateRangeFromBlocks(36,39) passed.
CreateRangeFromTimes(09:00,10:00) passed.
CreateRangeFromTimes(9:15,10:15) passed.
Utility Methods Tests Completed.

Running Utility Methods Performance Tests...
TimeToBlockIndex: 1,000,000 iterations took 10 ms
BlockIndexToTime: 1,000,000 iterations took 4 ms
CreateRangeFromBlocks: 1,000,000 iterations took 37 ms
CreateRangeFromTimes: 1,000,000 iterations took 70 ms
Utility Methods Performance Tests Completed.

=== Testing CreateRangeFromBlocks ===
PASS: CreateRangeFromBlocks(0, 0) returned 00:00:00 to 00:15:00
PASS: CreateRangeFromBlocks(0, 1) returned 00:00:00 to 00:30:00
PASS: CreateRangeFromBlocks(10, 10) returned 02:30:00 to 02:45:00
PASS: CreateRangeFromBlocks(95, 95) returned 23:45:00 to 1.00:00:00
PASS: CreateRangeFromBlocks(5, 3) correctly threw an exception: startBlock must be less than or equal to endBlock.
PASS: CreateRangeFromBlocks(-1, 3) correctly threw an exception: startBlock must be between 0 and 95. (Parameter 'startBlock')
PASS: CreateRangeFromBlocks(5, 96) correctly threw an exception: endBlock must be between 0 and 95. (Parameter 'endBlock')
=== End Testing CreateRangeFromBlocks ===

=== BitSchedule Functional Tests ===
Functional Test: ReadSchedule returned 0 days
Functional Test: WriteSchedule result = True
Functional Test: After WriteSchedule, ReadSchedule returned 0 days
=== End Functional Tests ===

=== BitSchedule Performance Tests ===
Performance Test: ReadSchedule 1,000,000 iterations took 348 ms
Performance Test: WriteSchedule 1,000,000 iterations took 145 ms
=== End Performance Tests ===

=== Test Configuration Change Refresh ===
PASS: Identical configuration did not refresh schedule data.
PASS: Different configuration.
Last Refreshed 474
Last Refreshed 475
PASS: Different data as config change should refresh data...

PASS: Different configuration causes a refresh which resets the dirty flag.


=== Testing the OnConfiguration Change...

Changing the ActiveDays - with the autorefresh on - data should change...
PASS: Data was reloaded after Active days changed...

Changing the ActiveDays - with the autorefresh off - data should NOT change...
PASS: Data was NOT reloaded after Active days changed - as expected...

=== End Test Configuration Change Refresh ===
