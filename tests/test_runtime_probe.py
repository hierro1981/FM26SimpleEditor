"""Startup probe regression tests; run with python -m unittest discover -s tests."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("runtime_probe", ROOT / "runtime_probe.py")
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class RuntimeProbeTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.requirements = Path(self.directory.name) / "requirements.txt"
        self.requirements.write_text("fmsave==0.5.11\nzstandard==0.25.0\n", encoding="utf-8")

    def inspect(self, version=(3, 12, 1), installed=None, import_error=None, pip=True):
        versions = installed if installed is not None else {"fmsave": "0.5.11", "zstandard": "0.25.0"}
        def metadata(name):
            if name not in versions:
                raise probe.importlib.metadata.PackageNotFoundError(name)
            return versions[name]
        with patch.object(probe.sys, "version_info", version), \
             patch.object(probe.importlib.metadata, "version", side_effect=metadata), \
             patch.object(probe.importlib, "import_module", side_effect=import_error) as imports, \
             patch.object(probe.importlib.util, "find_spec", return_value=object() if pip else None):
            result = probe.inspect_runtime(self.requirements)
            return result, [call.args[0] for call in imports.call_args_list]

    def test_old_python_rejected_before_reading_requirements(self):
        self.requirements.unlink()
        result, imports = self.inspect(version=(3, 11, 9))
        self.assertFalse(result["supported"])
        self.assertFalse(result["ready"])
        self.assertEqual(imports, [])

    def test_ready_on_312_includes_backport(self):
        result, imports = self.inspect()
        self.assertTrue(result["ready"])
        self.assertIn("backports.zstd", imports)
        self.assertTrue(result["executable"])

    def test_314_does_not_require_backport(self):
        result, imports = self.inspect(version=(3, 14, 0))
        self.assertTrue(result["ready"])
        self.assertNotIn("backports.zstd", imports)

    def test_missing_packages(self):
        result, _ = self.inspect(installed={})
        self.assertFalse(result["ready"])
        self.assertIn("fmsave==0.5.11", result["missing"])
        self.assertIn("zstandard==0.25.0", result["missing"])

    def test_wrong_version_requires_repair(self):
        result, _ = self.inspect(installed={"fmsave": "0.5.9", "zstandard": "0.25.0"})
        self.assertFalse(result["ready"])
        self.assertIn("0.5.9", result["missing"][0])

    def test_native_import_failure_requires_repair(self):
        result, _ = self.inspect(import_error=ImportError("DLL non caricabile"))
        self.assertFalse(result["ready"])
        self.assertTrue(any("DLL non caricabile" in item for item in result["missing"]))

    def test_pip_not_required_when_packages_work(self):
        result, _ = self.inspect(pip=False)
        self.assertTrue(result["ready"])
        self.assertFalse(result["hasPip"])

    def test_empty_requirements_rejected(self):
        self.requirements.write_text("# empty\n", encoding="utf-8")
        with self.assertRaises(ValueError):
            self.inspect()

    def test_unpinned_requirements_rejected(self):
        self.requirements.write_text("fmsave>=0.5\n", encoding="utf-8")
        with self.assertRaises(ValueError):
            self.inspect()

    def test_cli_missing_requirements_returns_json_error(self):
        if sys.version_info < (3, 12):
            self.skipTest("CLI requirement errors need Python 3.12+")
        run = subprocess.run([sys.executable, "-E", str(ROOT / "runtime_probe.py"), str(self.requirements) + ".missing"], capture_output=True, text=True, check=True)
        self.assertIn("FileNotFoundError", json.loads(run.stdout)["error"])


if __name__ == "__main__":
    unittest.main()
