#!/usr/bin/env bash
set -e

# Install runtime tools needed for development
if ! command -v ipmitool >/dev/null 2>&1; then
  sudo apt-get update
  sudo apt-get install -y --no-install-recommends ipmitool
  sudo rm -rf /var/lib/apt/lists/*
fi

git config rerere.enabled true
git config pull.rebase true
git config rebase.autostash true

dotnet dev-certs https
dotnet tool restore
dotnet husky install
