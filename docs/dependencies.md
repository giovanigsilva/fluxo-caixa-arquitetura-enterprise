# Dependências

Verificado em 2026-09-11 com documentação oficial e registros de pacotes.

## Backend

- .NET SDK local: 10.0.112; `global.json` fixa 10.0.100 com roll-forward.
- Runtime ASP.NET container: `mcr.microsoft.com/dotnet/aspnet:10.0.12`.
- Microsoft.AspNetCore.OpenApi: 10.0.12.
- Microsoft.Extensions.Hosting: 10.0.12.
- Npgsql: 10.0.3.
- EF Core: 10.0.12.
- RabbitMQ.Client: 7.2.2.
- StackExchange.Redis: 3.2.0.
- YARP: 2.3.0.
- Microsoft.Playwright: 1.62.0.
- xUnit: 2.9.3.

## Frontend

- Node local: 20.20.2.
- pnpm: 12.4.1.
- React/React DOM: 19.3.0.
- Vite: 7.3.6 por compatibilidade com `@vitejs/plugin-react` 5.1.3.
- Tailwind CSS: 4.3.3.
- TanStack Query: 5.102.8.
- TanStack Table: 9.2.4.
- React Hook Form: 7.87.0.
- Zod: 4.6.2.
- Lucide React: 1.45.0.
- Recharts: 3.10.1.

## Imagens verificadas

- `node:20.20.2-bookworm-slim`
- `postgres:18.1-bookworm`
- `redis:8.4.0-alpine`
- `rabbitmq:4.2.1-management-alpine`
- `quay.io/keycloak/keycloak:26.4.7`
- `hashicorp/vault:1.21.1`
- `otel/opentelemetry-collector-contrib:0.142.0`
- `prom/prometheus:v3.8.1`
- `grafana/grafana:13.1.3`
- `grafana/loki:3.6.3`

## Referências oficiais consultadas

- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- https://react.dev/versions
- https://ui.shadcn.com/docs/installation/vite
- https://tailwindcss.com/docs/installation/using-vite
- https://www.postgresql.org/docs/current/
- https://www.rabbitmq.com/docs/confirms
- https://www.keycloak.org/docs/latest/server_admin/
- https://developer.hashicorp.com/vault/docs/internals/security
- https://docs.docker.com/compose/how-tos/profiles/
