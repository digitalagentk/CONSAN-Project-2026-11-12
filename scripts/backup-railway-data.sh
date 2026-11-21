#!/usr/bin/env bash
# Downloads the live site's data from Railway (/app/data): content, map pins, timeline,
# gallery and every uploaded photo. Needs the Railway CLI logged in (railway login).
#
#   ./scripts/backup-railway-data.sh                # -> ~/Desktop/repos/CONSAN-data-backups/<date-time>
#   ./scripts/backup-railway-data.sh /path/to/dir   # -> /path/to/dir/<date-time>
#
# Secrets (protection-keys, .admin-reset) are skipped on purpose.
set -euo pipefail

SERVICE="consan-project"
PROJECT="e1f6772f-2906-41c7-8fce-838439ae5ecd"
ROOT="${1:-$HOME/Desktop/repos/CONSAN-data-backups}"
DEST="$ROOT/$(date +%Y-%m-%d_%H%M)"
SKIP='^(protection-keys|\.admin-reset|lost\+found|\.partial)$'

cd "$(dirname "$0")/.."
railway link --project "$PROJECT" >/dev/null 2>&1
railway service link "$SERVICE" >/dev/null 2>&1
mkdir -p "$DEST"

# Lists a remote folder as "type<TAB>name" lines.
list() {
    railway service files list "$1" --json | python3 -c '
import json, sys
d = json.load(sys.stdin)
for f in (d["files"] if isinstance(d, dict) else d):
    print(f["type"] + "\t" + f["name"])'
}

copy_dir() {   # remote dir, local dir
    mkdir -p "$2"
    list "$1" | while IFS=$'\t' read -r type name; do
        [[ "$name" =~ $SKIP ]] && continue
        if [ "$type" = "directory" ]; then
            copy_dir "$1/$name" "$2/$name"
        else
            railway service files download "$1/$name" "$2/$name" --json >/dev/null
            echo "  $1/$name"
        fi
    done
}

echo "Backing up $SERVICE /app/data -> $DEST"
copy_dir /app/data "$DEST"
echo "Done: $(find "$DEST" -type f | wc -l | tr -d ' ') files in $DEST"
