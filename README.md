# collaboration-in-local-communities

`docker-compose.yml` is the local dev stack for the monorepo. It runs Postgres, the Cosmos emulator, the backend, and the frontend together so `docker compose up` gives you the full app environment described in issue `#4`.

Note: the Cosmos DB Linux emulator image is commonly x86_64-only. The compose files pin the emulator service to `platform: linux/amd64`, which may rely on emulation or fail on ARM64 hosts without x86_64 compatibility.

`docker-compose.db.yml` is the backing-services-only variant. It starts just Postgres and the Cosmos emulator, which is useful if you want to run the backend and frontend directly from your IDE while still depending on containerized local databases.

`docker-compose.prod.yml` is the production-oriented compose file. It builds the app containers in their production targets and expects external runtime configuration such as the Cosmos endpoint/key instead of bundling the local emulator into that stack.

## Backend database persistence

The backend uses EF Core Code First with PostgreSQL, ASP.NET Core Identity, Npgsql, and NetTopologySuite/PostGIS. The local database compose services use `postgis/postgis:18-3.6-alpine` so the `postgis` extension required by the initial migration is available.

From the repository root, restore the local EF tool and start the backing PostgreSQL service:

```powershell
dotnet tool restore
docker compose -f docker-compose.db.yml up -d db
```

Create a new migration when the model changes:

```powershell
dotnet tool run dotnet-ef migrations add InitialCreate --project backend --startup-project backend --context AppDbContext --output-dir Infrastructure\Persistence\Migrations
```

Apply migrations to the configured database:

```powershell
dotnet tool run dotnet-ef database update --project backend --startup-project backend --context AppDbContext
```

If you use a globally installed EF tool instead, the equivalent commands are `dotnet ef migrations add InitialCreate ...` and `dotnet ef database update ...`.
