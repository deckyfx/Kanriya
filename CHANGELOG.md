# Changelog

All notable changes to the GraphQL Server project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-08-23

### Added
- Standardized subscription event structure for all entities
- Generic `SubscriptionEvent<T>` base class for reusability
- Subscription standards documentation in CLAUDE.md

### Changed
- **BREAKING**: Renamed subscription fields for standardization:
  - `eventType` → `event` (now returns CREATED/UPDATED/DELETED)
  - `eventTimestamp` → `time`
  - `greetLog` → `document`
  - `previousContent` → `_previous` (now returns full object)
  - Removed `id` field (use `document.id` instead)
- Event types renamed: Added→Created, Updated→Updated, Deleted→Deleted

### Improved
- Consistent subscription payload structure across all future entities
- Better field naming following GraphQL conventions
- Full object in `_previous` instead of just content

## [1.0.2] - 2025-08-22

### Added
- Unified subscription `onGreetLogChanged` that combines all event types
- `GreetLogEvent` type with event type indicator and metadata
- Development philosophy documentation: "Live on the Edge"

### Changed
- **BREAKING**: Removed individual subscriptions (onAdded, onUpdated, onDeleted)
- Consolidated all subscription events into single efficient channel
- Updated mutations to use unified event system

### Improved
- Resource efficiency by reducing subscription channels from 3 to 1
- Cleaner codebase following "no backward compatibility" philosophy

## [1.0.1] - 2025-08-22

### Added
- Version tracking system with `AppVersion` class
- System queries for `version` and `health` endpoints
- Version display at server startup
- Comprehensive README with setup instructions
- CHANGELOG for tracking version history

### Changed
- Updated GraphQL configuration to include system queries
- Enhanced console output formatting with version information

### Fixed
- Database naming convention using snake_case globally

## [1.0.0] - 2025-08-22

### Added
- Initial release with GreetLog management system
- PostgreSQL database integration with Entity Framework Core
- Service layer architecture with singleton pattern
- GraphQL queries: list, getById, getRecent, search, dateRange, count
- GraphQL mutations: add, update, delete, bulkAdd, deleteOld
- GraphQL subscriptions: onAdded, onUpdated, onDeleted
- JWT authentication support
- Authorization policies
- Docker Compose setup with PostgreSQL and pgAdmin
- Database migration helper scripts
- Server runner script with database checks
- Snake_case naming convention for PostgreSQL