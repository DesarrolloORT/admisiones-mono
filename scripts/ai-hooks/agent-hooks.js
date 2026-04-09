// ai-toolkit:toolkit profile=agent-hooks path=scripts/ai-hooks/agent-hooks.js
import { cwd, stdin, stdout } from "node:process";
import {
  buildCompactSummary,
  buildFullGuardrailContext,
  buildInitialContext,
  buildPreToolResponse,
  buildRepoSummary,
  buildSubagentContext,
  classifyPromptGuardrail,
  classifyToolGuardrail,
  cleanupExpiredState,
  classifyToolUse,
  collectToolTargets,
  evaluatePreToolUse,
  isConcretePrompt,
  loadSessionState,
  noteCheckExecution,
  recordSearchFingerprint,
  removeSessionState,
  resolveToolkitSignals,
  saveSessionState,
  saveSessionSummary,
  setActiveGoal,
  updateSessionFiles,
  updateToolCounters,
} from "./agent-hooks-shared.js";

function readInput() {
  return new Promise((resolvePromise) => {
    let input = "";
    stdin.setEncoding("utf8");
    stdin.on("data", (chunk) => {
      input += chunk;
    });
    stdin.on("end", () => {
      resolvePromise(input);
    });
  });
}

function parseInput(raw) {
  if (!raw || raw.trim() === "") {
    return {};
  }

  try {
    return JSON.parse(raw);
  } catch {
    return {};
  }
}

function ensureSessionMetadata(root, state, signals = null) {
  if (!state.repoSummary) {
    state.repoSummary = buildRepoSummary(root);
  }

  if (signals) {
    state.repoScale = signals.repoScale;
    state.mode = signals.mode;
  }
}

function writeJson(value) {
  stdout.write(JSON.stringify(value));
}

async function main() {
  const eventName = process.argv[2] ?? "";
  const root = cwd();
  const parsed = parseInput(await readInput());
  const sessionIdDefaults = {
    SessionStart: "session-start",
    UserPromptSubmit: "prompt-submit",
    PreToolUse: "pre-tool-use",
    PostToolUse: "post-tool-use",
    SubagentStart: "subagent-start",
    PreCompact: "pre-compact",
    Stop: "stop",
  };
  const sessionId =
    typeof parsed.sessionId === "string" && parsed.sessionId.trim() !== ""
      ? parsed.sessionId
      : (sessionIdDefaults[eventName] ?? "agent-hooks");

  switch (eventName) {
    case "SessionStart": {
      cleanupExpiredState();
      const state = loadSessionState(root, sessionId);
      const signals = resolveToolkitSignals(root);
      ensureSessionMetadata(root, state, signals);
      setActiveGoal(state, typeof parsed.initialPrompt === "string" ? parsed.initialPrompt : "");
      state.lastGuardrailClass = "general";
      saveSessionState(root, sessionId, state);

      writeJson({
        hookSpecificOutput: {
          hookEventName: "SessionStart",
          additionalContext: buildInitialContext(state, signals),
        },
      });
      return;
    }

    case "UserPromptSubmit": {
      const state = loadSessionState(root, sessionId);
      const signals = resolveToolkitSignals(root);
      ensureSessionMetadata(root, state, signals);
      const prompt = typeof parsed.prompt === "string" ? parsed.prompt : "";
      const currentClass = classifyPromptGuardrail(prompt);
      const previousClass = state.lastGuardrailClass;
      const messages = [];

      setActiveGoal(state, prompt);

      if (currentClass !== "general" && currentClass !== previousClass) {
        messages.push(buildFullGuardrailContext(signals, currentClass));
      }

      if (
        currentClass === "general" &&
        signals.mode === "standard" &&
        prompt.trim() !== "" &&
        !isConcretePrompt(prompt)
      ) {
        messages.push(
          "Antes de expandir el contexto, fija archivo, path, workflow, job o feature concreto y reutiliza el estado ya explorado.",
        );
      }

      state.lastGuardrailClass = currentClass;
      saveSessionState(root, sessionId, state);

      writeJson({
        continue: true,
        ...(messages.length > 0 ? { systemMessage: messages.join(" ") } : {}),
      });
      return;
    }

    case "PreToolUse": {
      cleanupExpiredState();
      const state = loadSessionState(root, sessionId);
      const signals = resolveToolkitSignals(root);
      ensureSessionMetadata(root, state, signals);
      const currentClass = classifyToolGuardrail(parsed);
      const previousClass = state.lastGuardrailClass;
      const guardrailContext =
        currentClass !== "general" && currentClass !== previousClass
          ? buildFullGuardrailContext(signals, currentClass)
          : "";
      const evaluation = evaluatePreToolUse(state, parsed);

      if (evaluation.kind === "search" && evaluation.searchPreview?.isGlobal) {
        state.toolCounters.globalSearches = Number(state.toolCounters.globalSearches ?? 0) + 1;
        if (evaluation.warningLevel === "ask") {
          state.toolCounters.searchPrompts = Number(state.toolCounters.searchPrompts ?? 0) + 1;
        }
        if (evaluation.warningLevel === "notice" || evaluation.warningLevel === "strong") {
          state.toolCounters.searchWarnings = Number(state.toolCounters.searchWarnings ?? 0) + 1;
        }
      }

      if (evaluation.kind === "edit" && evaluation.shouldAsk) {
        state.toolCounters.editPrompts = Number(state.toolCounters.editPrompts ?? 0) + 1;
      }

      state.lastGuardrailClass = currentClass;
      saveSessionState(root, sessionId, state);
      writeJson(buildPreToolResponse(state, evaluation, guardrailContext));
      return;
    }

    case "PostToolUse": {
      const state = loadSessionState(root, sessionId);
      ensureSessionMetadata(root, state);
      const kind = classifyToolUse(parsed);
      const targets = collectToolTargets(parsed);

      updateToolCounters(state, kind);
      updateSessionFiles(state, targets, kind);
      noteCheckExecution(state, parsed);

      if (kind === "search") {
        recordSearchFingerprint(state, parsed);
      }

      saveSessionState(root, sessionId, state);
      writeJson({
        hookSpecificOutput: {
          hookEventName: "PostToolUse",
        },
      });
      return;
    }

    case "SubagentStart": {
      const state = loadSessionState(root, sessionId);
      ensureSessionMetadata(root, state);
      state.toolCounters.subagents = Number(state.toolCounters.subagents ?? 0) + 1;
      saveSessionState(root, sessionId, state);
      writeJson({
        hookSpecificOutput: {
          hookEventName: "SubagentStart",
          additionalContext: buildSubagentContext(state),
        },
      });
      return;
    }

    case "PreCompact": {
      const state = loadSessionState(root, sessionId);
      ensureSessionMetadata(root, state);
      state.toolCounters.compactions = Number(state.toolCounters.compactions ?? 0) + 1;
      state.latestCompactSummary = buildCompactSummary(state);
      saveSessionState(root, sessionId, state);
      writeJson({ continue: true });
      return;
    }

    case "Stop": {
      const state = loadSessionState(root, sessionId);
      ensureSessionMetadata(root, state);
      saveSessionSummary(root, sessionId, state);
      removeSessionState(root, sessionId);
      cleanupExpiredState();
      writeJson({ continue: true });
      return;
    }

    default: {
      writeJson({ continue: true });
    }
  }
}

main();
