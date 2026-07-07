# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.1] - 2026-07-07

### Changed

- Ignore BenchmarkDotNet artifact directories in `.gitignore`.

## [1.2.0] - 2026-07-07

### Changed

- Migrated pipeline chaining from `Task` to `ValueTask` to reduce allocations on synchronous steps.
- Replaced `RopResult` / `RopResult<TData>` records with `readonly struct` for lower allocation pressure.
- Split `RailwayHelper.Internal.cs` into focused partial files and added dedicated sync paths for `DoEach` / `PeekEach` overloads.

### Added

- BenchmarkDotNet project with pipeline benchmarks and a script to join benchmark reports.
- Tests for error context on `DoEach` / `PeekEach` failures.

## [1.0.0] - 2026-07-06

### Added

- Initial NuGet release extracted from `RailwayHelper` in marking-worker.
- Railway-oriented pipeline API: `Do`, `Next`, `Peek`, `DoEach`, `NextEach`, `PeekEach`, `IfNoData`, `OnFailure`.
- Labeled step context via `ParametrizedError` and `WhenCallData`.
- Built-in error types: `CancelledError`, `NoDataError`, `SequenceAbortedError`, `PipelineTerminatedError`, `HandledFailureError`.
- Unit test suite and console samples.
