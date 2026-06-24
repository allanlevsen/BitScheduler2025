# Hex Search Follow-Up

## Current Situation

- The event form currently resolves an address in two ways:
  - geocode address to latitude/longitude
  - resolve hex grid by address
- The latitude/longitude flow is working.
- The hex-grid-by-address flow is heavier than needed for the event form.
- The hex grid data model is being built to support future proximity search by neighboring cells.

## Desired Direction

Do not change this yet. When we revisit it, the event form should use this simpler flow:

1. User selects an address from Google Places, or tabs off the address field.
2. Geocode the address once.
3. Populate `Latitude` and `Longitude` fields from the geocode result.
4. Call a hex-grid lookup endpoint using `latitude` and `longitude`.
5. Populate `HexGridId` from that lookup result when found.

## Why

- The event form only needs a single `HexGridId` for one point.
- Looking up the grid by `latitude`/`longitude` is the natural path after geocoding.
- This avoids using a second address-based lookup path just to fill `HexGridId`.
- Neighbor data is still needed later for proximity-based driver search.

## Keep

These parts are still useful:

- `HexGridVersions`
- `HexGridCells`
- `HexGridNeighbors`

These support:

- point-to-cell lookup
- future neighbor expansion
- future “find drivers near this address” workflows

## Reconsider Later

Review whether `HexGridSearchRings` should remain precomputed.

Reason:

- it may create very large row counts
- future proximity search could instead expand outward using the neighbor graph level by level
- configurable search depth fits neighbor traversal naturally

## Future Event Form Refactor

When ready, update the event form so it no longer depends on:

- `GET /api/locations/hex-grid?address=...`

Instead, use:

- geocode address
- then call `GET /api/hex-grid/cell?latitude=...&longitude=...`

Possible implementation options:

- Option A: keep `/api/hex-grid/cell` and read the returned cell `id`
- Option B: add a smaller endpoint such as `/api/hex-grid/id` that returns only the matching `HexGridId`

## Future Driver Search

Planned search approach:

1. Resolve address to `latitude`/`longitude`
2. Resolve `HexGridId`
3. Search drivers in the same cell
4. Expand through neighbor cells
5. Continue outward up to a configurable number of levels

## Suggested Next Steps When We Revisit This

1. Update the event form to use geocode result plus hex lookup by `latitude`/`longitude`
2. Stop calling the address-based hex lookup endpoint from the UI flow
3. Evaluate the size and necessity of `HexGridSearchRings`
4. Keep the neighbor table for future proximity search
