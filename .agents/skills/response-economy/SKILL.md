---
name: response-economy
description: Compact a reply when the user asks for "less tokens", "be brief", "caveman mode", "short answer", "tl;dr", or an actionable summary without losing technical precision.
argument-hint: "[request, text, or optional diff]"
kind: workflow
scope: toolkit
inputs:
  - request or answer to compact
outputs:
  - brief answer with facts and next actions
---

<!-- ai-toolkit:toolkit profile=base path=.agents/skills/response-economy/SKILL.md -->

# Response Economy

Use this skill when Codex should reduce output length. Keep technical accuracy higher priority than compression.

## Process

1. Choose level: `lite` by default, `full` for caveman mode, `ultra` only when maximum compression is requested.
2. Remove greetings, filler, apologies, repeated prompt wording, and unrequested justification.
3. Preserve exact file names, commands, errors, APIs, contracts, limits, and decisions.
4. If compression creates ambiguity around security, irreversible actions, operational ordering, or compliance, use normal prose.
5. For code, commits, PR text, and formal docs, write normal content; compact only surrounding explanation.

## Output

- direct answer, usually 1 to 3 bullets or one short paragraph
- finding or decision before background
- next action only when useful
