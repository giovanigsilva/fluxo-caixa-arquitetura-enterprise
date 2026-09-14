# Evidencia De Performance - Consolidado Diario 50 RPS

Data: 2026-09-14.

## Objetivo

Validar o requisito nao funcional do desafio: em dias de pico, o servico de
consolidado diario deve suportar 50 requisicoes por segundo com no maximo 5% de
perda.

## Ambiente

- Alvo: UAT local.
- Base URL: `http://127.0.0.1:6210`.
- Endpoint: `GET /api/consolidated/daily`.
- Script: `tests/performance/consolidated-50rps.k6.js`.
- Executor: Docker `grafana/k6:latest`.
- Digest da imagem usada: `sha256:5221b620a4f874faff6e32ba597aa667c058391fe4898b1c6f6377f062c6cdec`.

## Comando Executado

```bash
docker run --rm --network host \
  -v "$PWD:/work:ro" \
  -e BASE_URL=http://127.0.0.1:6210 \
  grafana/k6 run /work/tests/performance/consolidated-50rps.k6.js
```

## Configuracao Do Cenario

- Executor: `constant-arrival-rate`.
- Taxa: `50` iteracoes por segundo.
- Duracao: `10m`.
- VUs pre-alocados: `50`.
- Max VUs: `120`.

## Thresholds

| Metrica | Limite | Resultado | Status |
| --- | ---: | ---: | --- |
| `http_req_failed` | `rate<=0.05` | `0.00%` | Passou |
| `http_req_duration` p95 | `<=200ms` | `807.38us` | Passou |
| `http_req_duration` p99 | `<=400ms` | `1.24ms` | Passou |

## Resultado Total

| Metrica | Resultado |
| --- | ---: |
| Requisicoes HTTP | `30001` |
| Taxa media | `50.001571/s` |
| Falhas HTTP | `0 out of 30001` |
| Falhas HTTP percentual | `0.00%` |
| Checks totais | `60002` |
| Checks com sucesso | `60002 out of 60002` |
| Checks falhos | `0 out of 60002` |
| Duracao media HTTP | `566.13us` |
| Duracao maxima HTTP | `5.99ms` |
| p90 HTTP | `716.38us` |
| p95 HTTP | `807.38us` |
| Iteracoes | `30001` |

Checks executados:

- `status 200`.
- `read model returned`.

## Conclusao

O benchmark formal atende o requisito nao funcional do desafio no ambiente UAT
local: 50 RPS por 10 minutos, com 0% de perda, abaixo do limite maximo de 5%.

## Observacoes

- Este resultado mede a UAT local file-backed.
- Nao substitui ensaio produtivo com banco gerenciado, replicas, rede publica,
  observabilidade real e janela estatistica maior.
- O menu `Teste de carga` da SPA continua sendo uma simulacao visual de
  observabilidade; o benchmark real esta documentado neste arquivo.

