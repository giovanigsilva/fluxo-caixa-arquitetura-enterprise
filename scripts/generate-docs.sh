#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

mkdir -p "$ROOT/docs/exports"
cp "$ROOT/docs/architecture/definition.md" "$ROOT/docs/exports/Definicao_Arquitetura_Software_Fluxo_de_Caixa.md"
HTML="$ROOT/docs/exports/Definicao_Arquitetura_Software_Fluxo_de_Caixa.html"
PDF="$ROOT/docs/exports/Definicao_Arquitetura_Software_Fluxo_de_Caixa.pdf"
if command -v google-chrome >/dev/null 2>&1; then
  google-chrome --headless --disable-gpu --print-to-pdf="$PDF" "$HTML" >/dev/null
  echo "Markdown and PDF exports generated under docs/exports."
else
  echo "Markdown export generated. PDF rendering pending: google-chrome not found."
fi
