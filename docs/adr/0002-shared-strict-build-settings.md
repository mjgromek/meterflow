# 2. Strict build settings shared through Directory.Build.props

Status: Accepted · Milestone: M0

## Context
Seven projects would otherwise each repeat the target framework and compiler settings, and
drift apart.

## Decision
`Directory.Build.props` sets `net11.0`, latest language version, nullable reference types,
implicit usings and `TreatWarningsAsErrors` for every project. Warnings are fixed, never
suppressed.

## Consequences
- A possible null dereference fails the build instead of failing at run time.
- A project can still override a property in its own `.csproj`.
- Rejected: per-project settings. Easy to forget in a new project.
