"""Clasifica el origen del texto de cada OperationResult.IsFailed del backend.

Uso (desde la raiz del monorepo):
    python errores-af/scripts/scan-backend-messages.py api-admisiones/WebApiAdmisiones

Imprime: conteo por origen, llamadas con texto fuera del .resx y llamadas que
responden 5xx. Excluye codigo generado (DevartDtos), tests y bin/obj.
"""

import collections
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")

EXCLUDED = ("obj", "bin", "DevartDtos", "UnitTesting")
CALL = re.compile(r"\bIsFailed\s*\(")
SERVER_STATUS = re.compile(r"\b5\d\d\b|Status5\d\d")


def split_args(body):
    args, depth, current = [], 0, ""
    for ch in body:
        if ch in "([{":
            depth += 1
        elif ch in ")]}":
            depth -= 1
        if ch == "," and depth == 0:
            args.append(current.strip())
            current = ""
        else:
            current += ch
    args.append(current.strip())
    return args


def find_calls(root):
    for dirpath, _, files in os.walk(root):
        if any(part in dirpath for part in EXCLUDED):
            continue
        for name in files:
            if not name.endswith(".cs"):
                continue
            path = os.path.join(dirpath, name)
            source = open(path, encoding="utf-8", errors="ignore").read()
            for match in CALL.finditer(source):
                start, depth, end = match.end(), 1, match.end()
                while end < len(source) and depth:
                    depth += {"(": 1, ")": -1}.get(source[end], 0)
                    end += 1
                line = source.count("\n", 0, match.start()) + 1
                yield os.path.relpath(path, root), line, split_args(source[start : end - 1])


def strip_comments(arg):
    return "\n".join(l for l in arg.splitlines() if not l.strip().startswith("//")).strip()


def classify(message_arg):
    arg = strip_comments(message_arg)
    if "ocalizer[" in arg:
        return "resx"
    if arg.startswith('"') or arg.startswith('$"'):
        return "literal"
    if ".Message" in arg:
        return "texto de otra capa (.Message)"
    return "variable (revisar origen)"


def one_line(text, limit=100):
    return " ".join(text.split())[:limit]


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    calls = [c for c in find_calls(root) if len(c[2]) >= 3]
    counts = collections.Counter(classify(args[2]) for _, _, args in calls)
    print(f"Llamadas IsFailed: {len(calls)}")
    for kind, total in counts.most_common():
        print(f"  {kind}: {total}")

    print("\n## Texto fuera del .resx\n")
    print("| archivo:linea | errorCode | texto |")
    print("| --- | --- | --- |")
    for path, line, args in sorted(calls):
        if classify(args[2]) != "resx":
            print(f"| {path}:{line} | {one_line(strip_comments(args[0]), 40)} | {one_line(strip_comments(args[2]))} |")

    print("\n## Respuestas 5xx\n")
    print("| archivo:linea | errorCode | status | texto |")
    print("| --- | --- | --- | --- |")
    for path, line, args in sorted(calls):
        status = next((a for a in args[3:5] if SERVER_STATUS.search(a)), None)
        if status:
            print(f"| {path}:{line} | {one_line(strip_comments(args[0]), 40)} | {status} | {one_line(strip_comments(args[2]))} |")


if __name__ == "__main__":
    main()
