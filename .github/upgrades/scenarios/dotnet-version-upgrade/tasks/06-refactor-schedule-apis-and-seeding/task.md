# 06-refactor-schedule-apis-and-seeding: Refactor schedule requests, API usage, and seeding for resource-based storage

Update the schedule-facing models, API endpoints, and seed data flow so reads and writes target a specific resource and use the new range-based persistence model rather than daily client-wide rows.

**Done when**: Schedule requests carry a resource identifier, API and service flows use resource-based loading and saving, and seeding populates the new resource schedule range storage.

## Research Notes
- `BitResourceId` has already been added to the schedule-facing request and configuration models as part of the persistence redesign.
- The API endpoints now validate and pass `BitResourceId` into runtime schedule configuration.
- Seeding now populates `BitResourceScheduleRanges` in addition to legacy `BitDay` rows so the compatibility-first storage model has real data to load.
- There is no separate frontend schedule client in the current solution tree, so the API layer is the primary schedule surface to refactor here.

## Execution Notes
- Keep compatibility with existing `BitDay`-based in-memory behavior.
- Continue using the API layer as the main integration point for resource-aware scheduling.
