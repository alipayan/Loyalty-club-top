# Packaging Building Blocks as NuGet (for internal Nexus)

This guide standardizes how every project under `src/BuildingBlocks` is packed and published to the internal Nexus NuGet repository.

## What is already wired in this repository

A shared `Directory.Build.props` exists at `src/BuildingBlocks/Directory.Build.props`.
That file applies to every building block project and enforces a consistent packaging baseline:

- Marks projects as packable.
- Adds common NuGet metadata.
- Enables symbol package generation (`.snupkg`).
- Uses `Version` from CI (or defaults to `0.1.0-local` for local packs).
- Injects a shared package README file.

Also, two automation scripts are available:

- `tools/scripts/pack-building-blocks.sh`
- `tools/scripts/publish-building-blocks.sh`

## 1) Set your internal metadata (one-time)

Set these values in CI/MSBuild so package metadata points to your real internal endpoints:

- `PackageProjectUrl`
- `RepositoryUrl`
- `Authors` / `Company` (if needed)

Example:

```bash
dotnet pack src/BuildingBlocks -c Release \
  -p:Version=1.2.3 \
  -p:PackageProjectUrl=https://nexus.<company>.local \
  -p:RepositoryUrl=https://git.<company>.local/customer-club/loyalty-club-top.git
```

## 2) Build and pack locally

### Option A - script (recommended)

```bash
VERSION=1.2.3 ./tools/scripts/pack-building-blocks.sh
```

### Option B - direct dotnet command

```bash
dotnet pack src/BuildingBlocks --configuration Release -p:Version=1.2.3 -o ./artifacts/nuget
```

Output includes one `.nupkg` per building block plus `.snupkg` files.

## 3) Add Nexus as a NuGet source

```bash
dotnet nuget add source "https://<NEXUS_HOST>/repository/<NUGET_REPO>/index.json" \
  --name nexus-internal \
  --username "$NEXUS_USERNAME" \
  --password "$NEXUS_PASSWORD" \
  --store-password-in-clear-text
```

> Prefer CI secrets over local credential storage.

## 4) Publish all produced packages

### Option A - script (recommended)

```bash
NEXUS_API_KEY=<token> ./tools/scripts/publish-building-blocks.sh
```

Optional script environment variables:

- `PACKAGE_DIR` (default: `./artifacts/nuget`)
- `NUGET_SOURCE` (default: `nexus-internal`)

### Option B - direct loop

```bash
for pkg in ./artifacts/nuget/*.nupkg; do
  dotnet nuget push "$pkg" \
    --source nexus-internal \
    --api-key "$NEXUS_API_KEY" \
    --skip-duplicate
done
```

## 5) Consume from microservices

In each microservice project file:

```xml
<ItemGroup>
  <PackageReference Include="CustomerClub.BuildingBlocks.Messaging" Version="1.2.3" />
</ItemGroup>
```

Also ensure the service can restore from Nexus (for example with a repository-level `NuGet.config` in the microservice repo).

## CI recommendation

For each merge to `main`:

1. Compute semantic version (`<major>.<minor>.<patch>`).
2. Run `VERSION=<computed-version> ./tools/scripts/pack-building-blocks.sh`.
3. Run `NEXUS_API_KEY=<secret> ./tools/scripts/publish-building-blocks.sh`.
4. Keep `--skip-duplicate` enabled for safe reruns.

This creates immutable, versioned building blocks that any microservice can consume independently.
