# 05.03-range-persistence-core: Refactor core loading and saving to use resource schedule ranges

## Objective
Move the core schedule persistence flow from daily `BitDay` rows to the new per-resource schedule range rows.

## Scope
- Update `BitScheduleDataService` to load overlapping schedule ranges for a resource
- Update `BitSchedule` core persistence and in-memory population logic to read from and save back to schedule ranges
- Keep read/write behavior coherent while preparing the higher-level API and seeding changes for the next task

## Done when
- Core schedule loading uses the new resource range store instead of raw `BitDay` rows
- Core write operations save changes through the range persistence path
- The project builds successfully after the core persistence refactor

## Research Notes
- The new storage model should be keyed by `BitResourceId` and canonical 6-month date windows so a single resource read or write can cover a large contiguous period.
- `BitSchedule` can continue to use an in-memory dictionary of `BitDay` objects while the persistence layer maps those days into range rows.
- Configuration changes that alter the resource identity must now trigger reloads just like date-range and active-day changes.

## Planned Changes
- Refactor `BitScheduleDataService.LoadScheduleData` to read overlapping `BitResourceScheduleRange` rows and deserialize them to `BitDay` instances.
- Add a save path in `BitScheduleDataService` that groups day updates into canonical 6-month range rows and persists their payloads.
- Update `BitSchedule` write methods to save through the new data service rather than tracking individual `BitDay` EF entities.
- Update bulk writes to create missing in-memory days when a range exists logically but does not yet exist in storage.
