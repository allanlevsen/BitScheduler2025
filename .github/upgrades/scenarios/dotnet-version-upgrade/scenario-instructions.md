# Scenario Instructions

## Scenario
- **Type**: .NET version upgrade
- **Target Framework**: net10.0
- **Description**: Upgrade all projects in the solution to .NET 10

## Strategy
**Selected**: All-At-Once
**Rationale**: 8 SDK-style projects are already on .NET 9, the dependency graph is manageable, package updates have clear target versions, and the assessment indicates a straightforward upgrade with low per-project difficulty.

### Execution Constraints
- Upgrade all projects together in a single pass rather than by dependency tier.
- Update project target frameworks before resolving package and code compatibility changes.
- Treat package and framework updates as one coordinated upgrade across the solution.
- Validate with a full solution build and relevant tests after the upgrade work is complete.

## Preferences
### Flow Mode
- **Mode**: Automatic

### Commit Strategy
- **Mode**: Single Commit at End

### Source Control
- **Source Branch**: main
- **Working Branch**: upgrade-to-NET10
- **Pending Changes Handling**: Commit existing changes before starting the upgrade

## User Preferences
### Technical Preferences
- Upgrade all projects in the solution to **.NET 10**

### Execution Style
- Proceed automatically and only pause if blocked

## Key Decisions Log
- Initialized the .NET version upgrade scenario on branch `upgrade-to-NET10` after committing existing pending changes from `main`.
- User chose to commit pending changes before starting the upgrade.
- Auto-selected the **All-At-Once** strategy based on the low-complexity 8-project solution and clear .NET 10 package upgrade path.