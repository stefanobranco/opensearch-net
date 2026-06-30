#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
#
# Regenerates src/OpenSearch.Client/Generated from the vendored API spec.
#
# This is the single source of truth for how the generated client is produced. CI runs it
# and fails if the working tree changes, so Generated/ must be fully reproducible from the
# spec + generator: it contains NO hand-maintained files. Types that the generator cannot
# produce (e.g. DTOs referenced only by a hand-written override such as Aggregate) live under
# src/OpenSearch.Client/Types, not here.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
spec_dir="$repo_root/src/OpenSearch.CodeGen/Spec"
out_dir="$repo_root/src/OpenSearch.Client/Generated"

# The committed namespace set. Shared "Common" types are emitted as a byproduct of these.
namespaces="_core,cat,cluster,dangling_indices,geospatial,indices,ingest,ingestion,ism,knn,ltr,ml,nodes,security"

# Generated/ is disposable: wipe first so renamed or removed types leave no stale files.
rm -rf "$out_dir"
dotnet run --project "$repo_root/src/OpenSearch.CodeGen" -c Release -- \
  --spec-dir "$spec_dir" --output-dir "$out_dir" --namespaces "$namespaces"
