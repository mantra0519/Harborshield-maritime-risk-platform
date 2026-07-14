#!/usr/bin/env bash
set -euo pipefail

MODEL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/models"
mkdir -p "$MODEL_DIR"

download_if_missing() {
  local url="$1"
  local file="$MODEL_DIR/$2"

  if [ -f "$file" ]; then
    echo "Model already present at $file — skipping download."
    return 0
  fi

  echo "Downloading $2 into $MODEL_DIR ..."
  curl -L --fail --progress-bar -o "$file" "$url"
  echo "Done: $file"
}

download_if_missing \
  "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf" \
  "Phi-3-mini-4k-instruct-q4.gguf"

download_if_missing \
  "https://huggingface.co/ggml-org/bge-small-en-v1.5-Q8_0-GGUF/resolve/main/bge-small-en-v1.5-q8_0.gguf" \
  "bge-small-en-v1.5-q8_0.gguf"
