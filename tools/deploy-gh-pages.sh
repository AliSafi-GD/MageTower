#!/usr/bin/env bash
# Publish a WebGL build to the gh-pages branch of origin (GitHub Pages).
#   bash tools/deploy-gh-pages.sh [build-dir]      (default: Builds/WebGL)
# Build first with: Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL \
#   -executeMethod ArcaneSpire.EditorTools.WebGLBuild.Build
# One-time setup on GitHub: Settings -> Pages -> Source: "Deploy from a branch", branch gh-pages, folder / (root).
set -euo pipefail

BUILD_DIR="${1:-Builds/WebGL}"
REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

if [ ! -f "$BUILD_DIR/index.html" ]; then
  echo "deploy: $BUILD_DIR/index.html not found - build the WebGL player first" >&2
  exit 1
fi

SOURCE_SHA="$(git rev-parse --short HEAD)"
WORKTREE="$(mktemp -d)"
trap 'git worktree remove --force "$WORKTREE" >/dev/null 2>&1 || true' EXIT

git fetch origin gh-pages >/dev/null 2>&1 || true
if git show-ref --verify --quiet refs/remotes/origin/gh-pages; then
  git worktree add -B gh-pages "$WORKTREE" origin/gh-pages >/dev/null
else
  git worktree add --detach "$WORKTREE" >/dev/null
  git -C "$WORKTREE" checkout --orphan gh-pages >/dev/null
fi

# Replace the branch contents with the build (tracked files only; the worktree has nothing else).
git -C "$WORKTREE" rm -rfq --ignore-unmatch . >/dev/null
cp -R "$BUILD_DIR"/. "$WORKTREE"/
touch "$WORKTREE/.nojekyll"   # serve files/folders starting with '_' as-is

git -C "$WORKTREE" add -A
if git -C "$WORKTREE" diff --cached --quiet; then
  echo "deploy: build identical to the published one, nothing to push"
  exit 0
fi
git -C "$WORKTREE" commit -q -m "deploy: WebGL build from ${SOURCE_SHA}"
git -C "$WORKTREE" push origin gh-pages
echo "deploy: pushed gh-pages (build from ${SOURCE_SHA})"
