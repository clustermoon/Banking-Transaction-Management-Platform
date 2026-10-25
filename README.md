# Avenor Banking & Transaction Management Platform
This repository was intentionally created using very limited to no external help including AI agents or LLMS.

![C#](https://img.shields.io/badge/C%23-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-512BD4?logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?logo=microsoftsqlserver&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-512BD4?logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)
![CI](https://github.com/clustermoon/Banking-Transaction-Management-Platform/actions/workflows/ci.yml/badge.svg)

## Highlights

- **One-command database:** SQL Server 2022 in Docker, with nothing to configure.
- **Money handled carefully:** `decimal(18,2)` everywhere, with database check constraints that reject negative balances and non-positive amounts.
- **Append-only ledger and audit log:** database triggers reject `UPDATE` and `DELETE`, so you can try to tamper with it.
- **One error format:** every failure is an RFC 7807 `application/problem+json` response.
- **Gated by CI:** each pull request is built on Linux and format-checked before it can merge.

## Overview

This project began as a C# banking application I developed during my computer science coursework. The original assignment gave me a functioning foundation, but I wanted to take it further.

Rather than simply polishing the assignment, I decided to rebuild the application under my own architectural direction while reusing the business logic I'd already developed. This gives me the opportunity to make deliberate decisions about the application's structure, reliability, security, and maintainability.

I'm expanding the original scope into a **Banking & Transaction Management Platform**, with particular attention to financial correctness, concurrency, testing, and realistic application behavior.

My intention isn't to replicate every feature of a production banking system. I want to demonstrate how I approach software engineering problems, why I choose certain solutions over others, and how I balance technical quality against the time and complexity involved.

A full-stack banking and transaction management platform built with ASP.NET Core, Entity Framework Core, and SQL Server. Customers will manage checking and savings accounts, move money, and review their history, while administrators freeze and close accounts under an audit trail.

## Getting Started

**Prerequisites:** Docker Desktop. For local development and tests you also need the .NET 10 SDK.

```bash
docker compose up --build -d
```

The first start takes a minute while SQL Server initializes. Then:

| Service | URL |
| --- | --- |
| Client | http://localhost:5002 |
| API readiness | http://localhost:5001/health/ready |

Startup is ordered by health checks: the API starts once SQL Server accepts queries, and the client starts once the API reports ready.

### Resetting

```bash
docker compose down -v
```

This removes the database volume, so the next `up` starts from an empty database.

### Port conflicts

The stack binds ports 1433, 5001, and 5002 on `127.0.0.1`. If one is in use (a local SQL Server instance is the usual cause), stop that service or change the host side of the port mapping in `docker-compose.yml`.

### Database password

SQL Server runs with a documented development-only SA password, defined in `docker-compose.yml`. The ports are bound to loopback, so the database isn't reachable from other machines. To use your own password, set `MSSQL_SA_PASSWORD` before starting the stack. It must meet SQL Server's complexity rules. SQL Server keeps the password from the volume's first start, so if you change it, run `docker compose down -v` first.

## Local Development

Run the database in Docker and the apps from your IDE or the CLI:

```bash
docker compose up -d sqlserver
dotnet run --project BankingAPI/BankingAPI --launch-profile http
dotnet run --project BankingClient/BankingClient --launch-profile http
```

## Usage

Start the database and the API:

```bash
docker compose up -d && dotnet run --project BankingAPI/BankingAPI --launch-profile http
```

Try to tamper with the ledger. The database rejects it with `Msg 51000`:

```bash
docker exec banking-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P Banking_Dev_Passw0rd -C -d BankingDb -Q "UPDATE dbo.Transactions SET Amount = Amount"
```

Run tests

```bash
dotnet test BankingAPI/BankingAPI.slnx
```

## Authors
Hi, my name is [David Wommack](https://github.com/clustermoon). I created this project as a portfolio piece from the incredible advice of the amazing university faculty and staff that have supported me as I shifted from outside industries into the computer science industry. I have 16 years of hobbyist programming, but as of 3 years ago(2023) shifted into treating my passion as my profession, seeking to attain a degree and pursuing my life's passion.

## Feedback and Contributing

Questions and ideas are welcome in [Discussions](https://github.com/clustermoon/Banking-Transaction-Management-Platform/discussions), and bugs or feature requests in [Issues](https://github.com/clustermoon/Banking-Transaction-Management-Platform/issues). 