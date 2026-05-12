# QuantumOuija agent instructions

This repository targets .NET 8. Cursor Cloud agents use `.cursor/environment.json` and `.cursor/Dockerfile` to install `dotnet-sdk-8.0`, so `dotnet` should be available on PATH at startup.

Common commands:

```bash
dotnet restore QuantumOuija.sln
dotnet build QuantumOuija.sln
dotnet run --project src/QuantumOuija/QuantumOuija.csproj
```
