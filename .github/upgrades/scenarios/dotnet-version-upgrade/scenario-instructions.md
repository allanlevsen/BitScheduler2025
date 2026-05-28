# Scenario Instructions

## Scenario
- **Type**: .NET version upgrade
- **Target Framework**: net10.0
- **Description**: Upgrade all projects in the solution to .NET 10

## Preferences
### Flow Mode
- **Mode**: Automatic

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