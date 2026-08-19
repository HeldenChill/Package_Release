# Hung Sync Simulator (`com.hung.sync.simulator`)

**DEVELOPMENT AND TEST ONLY.** Never reference this from a production runtime assembly.

Stage S1 of the fake-server evolution. Implements `ISyncTransport` and `ISyncAuthProvider`
from `com.hung.sync` against deterministic in-memory state.

## Why a separate package

Package separation is the only mechanically enforceable barrier keeping simulator code out of
the production dependency closure. A folder plus a define constraint relies on convention;
a separate package makes a production dependency a visible, validator-catchable fact.
`Docs/audit/check_package_contracts.py` enforces this (gate G6).

## Capabilities

Multiple accounts, authoritative state and revision per stream, operation-id deduplication,
configurable latency, offline mode, auth expiry, business rejection rules, forced revision
conflicts, deterministic clock and fault scripts, reset and inspection APIs.

State is in memory only. A durable local host is Stage S2 and out of scope.
