# 05-resource-range-storage: Introduce per-resource schedule range storage

Replace the persistence model that stores schedule state one day at a time with a resource-oriented range model that can store a larger contiguous period per resource, including explicit `StartDate` and `EndDate` boundaries for efficient overlap searches. Preserve the current `BitDay` behavior as the in-memory representation while introducing a serialized range payload suitable for multi-month storage.

**Done when**: The core data model and persistence layer support per-resource schedule range rows, including schema, serialization, load logic, and save logic for larger date ranges.
