"""Standard-library-only startup probe, compatible with Python 3.8+.

No installs or writes: returns the interpreter and dependency health as JSON.
"""
import contextlib
import importlib
import importlib.metadata
import importlib.util
import io
import json
from pathlib import Path
import re
import sys


def inspect_runtime(requirements_path):
    supported = sys.version_info >= (3, 12)
    result = dict(executable=sys.executable,
                  baseExecutable=getattr(sys, "_base_executable", sys.executable),
                  version=".".join(str(n) for n in sys.version_info[:3]),
                  supported=supported, ready=False, missing=[], hasPip=False)
    if not supported:
        return result
    requirements = []
    for line in Path(requirements_path).read_text(encoding="utf-8-sig").splitlines():
        line = line.split("#", 1)[0].strip()
        if not line:
            continue
        match = re.fullmatch(r"([A-Za-z0-9_.-]+)==([A-Za-z0-9_.+!-]+)", line)
        if match is None:
            raise ValueError("Formato dipendenza non supportato: " + line)
        requirements.append(match.groups())
    if not requirements:
        raise ValueError("Il file requirements.txt non contiene dipendenze")
    for name, version in requirements:
        try:
            installed = importlib.metadata.version(name)
            if installed != version:
                result["missing"].append("%s: richiesta %s, installata %s" % (name, version, installed))
        except importlib.metadata.PackageNotFoundError:
            result["missing"].append(name + "==" + version)
    # Metadata alone does not prove imports or native extension loading work.
    modules = ["fmsave", "zstandard"]
    if sys.version_info < (3, 14):
        modules.append("backports.zstd")
    for name in modules:
        try:
            with contextlib.redirect_stdout(io.StringIO()):
                importlib.import_module(name)
        except Exception as exc:
            result["missing"].append("%s: %s: %s" % (name, type(exc).__name__, exc))
    result["hasPip"] = importlib.util.find_spec("pip") is not None
    result["ready"] = not result["missing"]
    return result


if __name__ == "__main__":
    try:
        result = inspect_runtime(sys.argv[1])
    except Exception as exc:
        result = dict(error="%s: %s" % (type(exc).__name__, exc))
    print(json.dumps(result, ensure_ascii=True, separators=(",", ":")))
