# Operações

`scripts/up.sh` sempre executa `docker compose down` do projeto alvo antes de
subir novamente, sem `-v`, preservando dados em `.runtime`.

Arquivos privados ficam em `.secrets/{uat,production}` com `0600` e diretórios
`0700` gerados por `scripts/bootstrap.sh`.

Backups locais são criados em `artifacts/`. Cópia local não é backup off-host.
