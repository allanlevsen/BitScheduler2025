# 05.01-range-entity-and-schema: Add the per-resource schedule range entity and EF schema

# 05.01-range-entity-and-schema: Add the per-resource schedule range entity and EF schema

## Objective
Introduce a new persistence model for schedule storage that represents a larger contiguous schedule range for a single resource instead of a single day for a client.

## Scope
- Add a new entity for resource schedule ranges
- Include `BitClientId`, `BitResourceId`, `StartDate`, `EndDate`, and serialized payload storage
- Update `BitScheduleDbContext` mappings and relationships
- Add or update EF migration artifacts to represent the new table and indexes

## Done when
- The solution contains a concrete per-resource schedule range entity
- EF Core maps the new entity with overlap-search-friendly keys and indexes
- Migration artifacts exist for the new schema changes

## Research Notes
- Current persisted schedule state is stored in `BitDay` rows keyed by `ClientId + Date`.
- Resource identity exists in `BitResource`, but the current schedule persistence path does not store schedule rows per resource.
- A compatibility-first redesign can add a new `BitResourceScheduleRange` entity alongside `BitDay` without breaking existing code paths immediately.
- The new table needs explicit `StartDate` and `EndDate` columns plus a binary payload so later subtasks can pack many `BitDay` values into a single row.

## Proposed Schema Shape
- `BitResourceScheduleRangeId`
- `BitClientId`
- `BitResourceId`
- `StartDate`
- `EndDate`
- `Payload`

## Mapping Notes
- Add a unique key over client/resource/date-range identity.
- Add an index that supports overlap queries by resource and date range.
- Add relationships from both `BitClient` and `BitResource` to the new range entity.
