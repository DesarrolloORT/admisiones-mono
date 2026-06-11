---
name: response-economy
description: Always-on response economy applied to EVERY interaction. No explicit invocation needed.
kind: always-on
scope: toolkit
---

<!-- ai-toolkit:toolkit profile=base path=.agents/skills/response-economy/SKILL.md -->

# Response Economy

Always-on skill: applied to every agent response. No user invocation required. Do not use it to hide uncertainty, critical evidence, or risks.

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
