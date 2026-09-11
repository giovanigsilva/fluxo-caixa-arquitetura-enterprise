# Baseline Delta

Baseline PDF `Arquitetura_Enterprise_CashFlow_C4_ENTERPRISE_FINAL.pdf` não foi
localizado no workspace durante a descoberta pontual. O original deverá ser
preservado em `docs/reference/` quando disponibilizado.

## Deltas deste pedido

- Adição de cadastros e permissões granulares.
- Adição de aprovação de login por celular como controle first-party pós-identidade.
- IA 3D adiada com `AssistantEnabled=false`.
- OpenTelemetry Collector, Prometheus, Grafana e Loki limitados a artefatos
  Docker/configuração; operação padrão usa telemetria sintética rotulada.

## Esclarecimentos técnicos

- Relações C4 mostram quem chama, lê, publica ou consome; bancos não iniciam chamadas.
- A transação de lançamento não depende do consolidado, mas autenticação, sessão e
  autorização têm dependências próprias.
- RPO zero é condicionado à preservação da persistência durável local validada; não
  cobre perda do único host/disco.
- Consolidado é reconstruível pela fonte autoritativa de lançamentos, não por fila
  RabbitMQ tratada como histórico permanente.
- Telemetria sintética é fixture operacional rotulada, distinta de evidência medida.
