"""Run the pinned backend's portable validation functions on the real bundle.

fcntl/publication/HTTP code is not imported or emulated on Windows. The selected
function bodies are unchanged AST nodes from the reviewed backend commit.
"""
import argparse
import ast
import datetime
import hashlib
import json
import pathlib
import re
import subprocess
from typing import Any

parser = argparse.ArgumentParser()
parser.add_argument("backend_source", type=pathlib.Path)
parser.add_argument("bundle", type=pathlib.Path)
parser.add_argument("--export-from", type=pathlib.Path)
args = parser.parse_args()
if args.export_from:
    exported = subprocess.check_output(["git", "-C", str(args.export_from), "show",
        "2f67ddc6f5382b286e3039677a05677fe4d7d16f:scripts/beta_release_publish.py"])
    assert hashlib.sha256(exported).hexdigest() == "127b7e08c3259112fdccd8bf17ade34c45d4cbe690fbca1478fcf25d426002e7", "Pinned backend source differs"
    args.backend_source.parent.mkdir(parents=True, exist_ok=True)
    args.backend_source.write_bytes(exported)
raw = args.backend_source.read_bytes()
assert hashlib.sha256(raw).hexdigest() == "127b7e08c3259112fdccd8bf17ade34c45d4cbe690fbca1478fcf25d426002e7", "Pinned backend source differs"
source = ast.parse(raw.decode("utf-8-sig"))
names = {"PublishError", "fail", "parse_timestamp", "strict_object", "bounded_text", "notes",
         "safe_filename", "sha256_file", "read_json", "file_contract", "validate_manifest", "validate_stage"}
nodes = [node for node in source.body if isinstance(node, (ast.FunctionDef, ast.ClassDef)) and node.name in names]
assert {node.name for node in nodes} == names, "Pinned contract functions missing"
namespace = dict(dt=datetime, hashlib=hashlib, json=json, pathlib=pathlib, re=re, Any=Any,
                 MANIFEST_SCHEMA_VERSION=1, MAX_JSON_BYTES=131072,
                 SAFE_FILENAME=re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]{0,191}\Z"),
                 SHA256=re.compile(r"[a-f0-9]{64}\Z"), PLATFORMS={"macos", "windows"},
                 ARCHITECTURES={"x64", "arm64", "universal"},
                 STATUSES={"pending", "active", "withdrawn", "replaced"},
                 PACKAGE_TYPES={"macos": {"dmg"}, "windows": {"msix"}})
exec(compile(ast.Module(body=nodes, type_ignores=[]), str(args.backend_source), "exec"), namespace)
result = namespace["validate_stage"](args.bundle)
assert result["release"]["platform"] == "windows"
print("PASS pinned backend O-052 validate_stage on exact Windows bundle")
