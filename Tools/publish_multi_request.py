#!/usr/bin/env python3

import argparse
import requests
import os
import subprocess
from typing import Iterable


def required_env(name: str) -> str:
    value = os.environ[name].strip()
    if not value:
        raise RuntimeError(f"Environment variable {name} is empty")
    return value


PUBLISH_TOKEN = required_env("PUBLISH_TOKEN")
VERSION = required_env("GITHUB_SHA")

RELEASE_DIR = "release"

#
# CONFIGURATION PARAMETERS
# Forks should change these to publish to their own infrastructure.
#
ROBUST_CDN_URL = os.environ.get("ROBUST_CDN_URL", "https://ss14.noxliquid.ru/cdn/").strip()
FORK_ID = os.environ.get("ROBUST_CDN_FORK_ID", "noxliquid-respiral").strip()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--fork-id", default=FORK_ID)

    args = parser.parse_args()
    fork_id = args.fork_id
    robust_cdn_url = ROBUST_CDN_URL.rstrip("/") + "/"

    session = requests.Session()
    session.headers = {
        "Authorization": f"Bearer {PUBLISH_TOKEN}",
    }

    print(f"Starting publish on Robust.Cdn for version {VERSION}")

    data = {
        "version": VERSION,
        "engineVersion": get_engine_version(),
    }
    headers = {
        "Content-Type": "application/json"
    }
    resp = session.post(f"{robust_cdn_url}fork/{fork_id}/publish/start", json=data, headers=headers)
    resp.raise_for_status()
    print("Publish successfully started, adding files...")

    for file in get_files_to_publish():
        print(f"Publishing {file}")
        with open(file, "rb") as f:
            headers = {
                "Content-Type": "application/octet-stream",
                "Robust-Cdn-Publish-File": os.path.basename(file),
                "Robust-Cdn-Publish-Version": VERSION
            }
            resp = session.post(f"{robust_cdn_url}fork/{fork_id}/publish/file", data=f, headers=headers)

        resp.raise_for_status()

    print("Successfully pushed files, finishing publish...")

    data = {
        "version": VERSION
    }
    headers = {
        "Content-Type": "application/json"
    }
    resp = session.post(f"{robust_cdn_url}fork/{fork_id}/publish/finish", json=data, headers=headers)
    resp.raise_for_status()

    print("SUCCESS!")


def get_files_to_publish() -> Iterable[str]:
    for file in os.listdir(RELEASE_DIR):
        yield os.path.join(RELEASE_DIR, file)


def get_engine_version() -> str:
    proc = subprocess.run(["git", "describe","--tags", "--abbrev=0"], stdout=subprocess.PIPE, cwd="RobustToolbox", check=True, encoding="UTF-8")
    tag = proc.stdout.strip()
    assert tag.startswith("v")
    return tag[1:] # Cut off v prefix.


if __name__ == '__main__':
    main()
