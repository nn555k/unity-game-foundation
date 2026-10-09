#!/usr/bin/env python3
"""Run one Unity test mode and handle editors that hang after writing results."""

from __future__ import annotations

import argparse
import subprocess
import time
import xml.etree.ElementTree as element_tree
from pathlib import Path


def parse_arguments() -> argparse.Namespace:
    """Parse the Unity executable, project and output locations."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--editor", required=True)
    parser.add_argument("--project", required=True)
    parser.add_argument("--mode", choices=("EditMode", "PlayMode"), required=True)
    parser.add_argument("--result", required=True)
    parser.add_argument("--log", required=True)
    parser.add_argument("--timeout", type=int, default=900)
    parser.add_argument("--exit-grace", type=int, default=10)
    return parser.parse_args()


def result_is_complete(result_path: Path) -> bool:
    """Return true only after Unity has closed the XML test-run element."""
    if not result_path.is_file():
        return False
    return "</test-run>" in result_path.read_text(encoding="utf-8", errors="replace")


def stop_process(process: subprocess.Popen[bytes]) -> None:
    """Terminate the owned batchmode process and escalate only if it ignores termination."""
    process.terminate()
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=5)


def test_result_passed(result_path: Path) -> bool:
    """Read the NUnit root result rather than trusting editor shutdown status."""
    root = element_tree.parse(result_path).getroot()
    return root.attrib.get("result") == "Passed" and root.attrib.get("failed") == "0"


def run(arguments: argparse.Namespace) -> int:
    """Run Unity until normal exit, timeout, or a completed test result plus grace period."""
    result_path = Path(arguments.result).resolve()
    log_path = Path(arguments.log).resolve()
    result_path.parent.mkdir(parents=True, exist_ok=True)
    log_path.parent.mkdir(parents=True, exist_ok=True)
    result_path.unlink(missing_ok=True)

    command = [
        arguments.editor,
        "-batchmode",
        "-nographics",
        "-projectPath",
        str(Path(arguments.project).resolve()),
        "-runTests",
        "-testPlatform",
        arguments.mode,
        "-testResults",
        str(result_path),
        "-logFile",
        str(log_path),
    ]
    process = subprocess.Popen(command)
    deadline = time.monotonic() + arguments.timeout
    completed_at: float | None = None

    while time.monotonic() < deadline:
        exit_code = process.poll()
        if exit_code is not None:
            if not result_is_complete(result_path):
                return exit_code or 1
            break

        if result_is_complete(result_path):
            completed_at = completed_at or time.monotonic()
            if time.monotonic() - completed_at >= arguments.exit_grace:
                stop_process(process)
                break

        time.sleep(0.25)
    else:
        stop_process(process)
        print(f"Unity {arguments.mode} tests timed out after {arguments.timeout} seconds.")
        return 124

    if not result_is_complete(result_path):
        print(f"Unity {arguments.mode} did not produce a complete result: {result_path}")
        return 1

    passed = test_result_passed(result_path)
    print(f"Unity {arguments.mode}: {'passed' if passed else 'failed'} ({result_path})")
    return 0 if passed else 1


def main() -> int:
    """Run one requested Unity test mode."""
    return run(parse_arguments())


if __name__ == "__main__":
    raise SystemExit(main())
