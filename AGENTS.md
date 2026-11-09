# Repository guide

## What this repository does

This is a .NET 6 worker-service CLI that reads collection data from EMu through the IMu .NET API and exports it to external systems. The application has two command-selected exports:

- `ala` creates an Atlas of Living Australia Darwin Core Archive (CSV files, images, and `meta.xml`), either for a destination directory or for upload.
- `agn` synchronizes AusGeochem records through its REST API.

The solution is `ImuExports.sln`. Application code is in `src/ImuExports`; tests are in `src/ImuExports.Tests`. IMu is an external dependency referenced from `dep/IMu/imu-1.0.03.dll`. Export resources include `src/ImuExports/Tasks/AtlasOfLivingAustralia/Resources/meta.xml` and `src/ImuExports/Tasks/AusGeochem/Resources/material-name-pairs.csv`; the project copies them to build and publish output.

## Responsibilities and flow

- `Program.cs` loads configuration, configures Serilog and the host, initializes the Simple Injector container, and runs the hosted `Infrastructure/TaskRunner`.
- `Configuration/CommandOptions.cs` parses the command-line options. Each export implements `Infrastructure/ITask`; `TaskRunner` initializes its options, runs it, and performs cleanup.
- A task coordinates its export: querying EMu, processing records, and writing or sending results. Shared IMu querying behavior is in `Tasks/ImuTaskBase.cs`.
- `Factories` transform IMu `Map` data into export-domain models. `Mappers` translate domain models into API DTOs. `Endpoints` handle API resources and requests; `Handlers` coordinate operations involving related records such as images or sample properties. Keep those responsibilities distinct when changing an export.
- `Config` holds export search configuration and constants. ALA class maps define CSV output. AusGeochem contract DTOs and request/response types are under `Contracts`.
- `AppSettings` and its model are in `src/ImuExports/appsettings*.json` and `src/ImuExports/Configuration/AppSettings.cs`. The CLI uses `src/ImuExports.Tests` with xUnit; tests include focused extension checks and image processing.

## Change and validation guidance

Preserve external contracts: ALA CSV columns and serialization, archive metadata, AusGeochem DTO/API behavior, and the material-name mapping resource. Changes to record selection, filters, mapped values, or output shape can affect downstream consumers; use the relevant task, factory, mapper, endpoint, or handler rather than mixing these concerns.

AusGeochem runs and automated ALA exports maintain incremental-run state in LiteDB (`LiteDbFilename`) by updating their previous-run date after export work. Preserve its meaning and the corresponding date/search behavior when changing incremental processing. Do not remove or regenerate bundled mapping or metadata resources without understanding their consumer.

Pass the supplied `CancellationToken` through asynchronous work and check it in long-running loops, following the task, IMu-fetch, endpoint, and factory patterns. Use structured Serilog logging (`Log.Logger`) for operational progress and failures; report unexpected failures rather than silently treating them as successful or empty results.

For code changes, inspect and run only relevant local tests when safe. The test project is `src/ImuExports.Tests/ImuExports.Tests.csproj`; a grounded targeted-project command is `dotnet test src/ImuExports.Tests/ImuExports.Tests.csproj`. This command was not run for this documentation-only change. Tests or builds do not validate connectivity or safety against external services.

## Configuration and operational safety

Configuration is loaded from `appsettings.json`, an optional `appsettings.{DOTNET_ENVIRONMENT}.json`, then environment variables. If `DOTNET_ENVIRONMENT` is unset, startup selects `Production`. Treat configuration files and environment values as potentially containing credentials or internal service addresses: do not copy secrets into source, documentation, logs, or test data, and do not overwrite existing user configuration.

The IMu library and reachable EMu service are required for export data access. Exports may also write to local paths, upload files, or create, update, and delete remote records. Do not run an export against live services unless the user has explicitly authorized that operation and its target. In particular, `agn --delete-all` deletes all samples and images in the configured AusGeochem data packages; never run it without explicit user authorization. Prefer offline, focused checks for ordinary development and documentation work.
