# Banking & Transaction Management Platform
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

## Usage

Start the database and the API:

```bash
docker compose up -d && dotnet run --project BankingAPI/BankingAPI --launch-profile http
```

Try to tamper with the ledger. The database rejects it with `Msg 51000`:

```bash
docker exec banking-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P Banking_Dev_Passw0rd -C -d BankingDb -Q "UPDATE dbo.Transactions SET Amount = Amount"
```

## Installation

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (the exact version is pinned in `global.json`), [Docker Desktop](https://www.docker.com/products/docker-desktop/), and Git. Developed on Windows and built on Linux in CI; macOS has not been verified yet.

```bash
git clone https://github.com/clustermoon/Banking-Transaction-Management-Platform.git
cd Banking-Transaction-Management-Platform
docker compose up -d        # wait until `docker compose ps` shows "healthy"
dotnet tool restore
dotnet ef database update --project BankingAPI/BankingAPI
```

## Authors
Hi, my name is [David Wommack](https://github.com/clustermoon). I created this project as a portfolio piece from the incredible advice of the amazing university faculty and staff that have supported me as I shifted from outside industries into the computer science industry. I have 16 years of hobbyist programming, but as of 3 years ago(2023) shifted into treating my passion as my profession, seeking to attain a degree and pursuing my life's passion.

## Feedback and Contributing

Questions and ideas are welcome in [Discussions](https://github.com/clustermoon/Banking-Transaction-Management-Platform/discussions), and bugs or feature requests in [Issues](https://github.com/clustermoon/Banking-Transaction-Management-Platform/issues). 