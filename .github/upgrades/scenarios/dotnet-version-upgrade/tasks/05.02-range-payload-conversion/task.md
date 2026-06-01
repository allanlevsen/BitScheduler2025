# 05.02-range-payload-conversion: Implement payload packing and BitDay conversion helpers

## Objective
Create the serialization layer that can pack and unpack many days of `BitDay` schedule data into a single larger payload for a resource range row.

## Scope
- Add helper types or methods that map `(date, block)` values to packed payload offsets
- Serialize and deserialize `BitDay` values across a date range
- Preserve current `BitDay` in-memory behavior while making larger resource ranges storable

## Done when
- There is a deterministic way to convert a date-range schedule to and from the stored payload
- `BitDay` values can be reconstructed from the new range representation
- Edge cases around date offsets and payload sizing are handled

## Research Notes
- A single `BitDay` already exposes persisted `BitsLow`, `BitsHigh`, and `IsFree`, which can be flattened into a byte payload without changing the in-memory model.
- A compatibility-first approach can treat each day in the range as a fixed-size payload segment so later persistence code can update or reconstruct `BitDay` instances deterministically.
- A simple fixed-size segment per day keeps the first implementation easy to reason about while still allowing multi-day storage in one row.

## Payload Layout
- Per day segment:
  - 8 bytes for `BitsLow`
  - 8 bytes for `BitsHigh`
  - 1 byte for `IsFree`
- Total bytes = `dayCount * 17`

## Helper Responsibilities
- Validate date range boundaries.
- Create empty `BitDay` entries for missing dates.
- Serialize an in-memory day dictionary into the stored payload.
- Deserialize the stored payload back into `BitDay` values.
- Compute date and bit offsets for future range-based operations.
