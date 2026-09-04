#!/usr/bin/env bash
set -euo pipefail

TARGET="all"
OLDER_THAN_DAYS=""
EXECUTE="false"
SERVICE="postgres"
DB_USER="${POSTGRES_USER:-ichat}"
DB_NAME="${POSTGRES_DB:-ichat}"

usage() {
  cat <<'EOF'
Usage:
  scripts/cleanup-postgres.sh [options]

Options:
  --target all|documents|conversations
      Data group to clean. Default: all.

  --older-than DAYS
      Only clean rows whose created_at is older than DAYS.
      Without this option, the selected target is fully cleaned.

  --execute
      Actually delete data. Without this flag the script only prints counts.

  --service NAME
      Docker Compose Postgres service name. Default: postgres.

  -h, --help
      Show this help.

Connection:
  By default this runs psql inside the Docker Compose service:
    docker compose exec -T postgres psql -U ichat -d ichat

  If DATABASE_URL is set, the script uses local psql with that URL instead.

Examples:
  scripts/cleanup-postgres.sh
  scripts/cleanup-postgres.sh --target documents --older-than 30
  scripts/cleanup-postgres.sh --target all --older-than 7 --execute
  DATABASE_URL=postgres://ichat:ichat@localhost:5432/ichat scripts/cleanup-postgres.sh --execute
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --target)
      TARGET="${2:-}"
      shift 2
      ;;
    --older-than)
      OLDER_THAN_DAYS="${2:-}"
      shift 2
      ;;
    --execute)
      EXECUTE="true"
      shift
      ;;
    --service)
      SERVICE="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

case "$TARGET" in
  all|documents|conversations) ;;
  *)
    echo "--target must be one of: all, documents, conversations" >&2
    exit 2
    ;;
esac

if [[ -n "$OLDER_THAN_DAYS" && ! "$OLDER_THAN_DAYS" =~ ^[0-9]+$ ]]; then
  echo "--older-than must be a positive number of days" >&2
  exit 2
fi

if [[ "$OLDER_THAN_DAYS" == "0" ]]; then
  echo "--older-than must be greater than 0" >&2
  exit 2
fi

run_psql() {
  local sql="$1"

  if [[ -n "${DATABASE_URL:-}" ]]; then
    psql "$DATABASE_URL" \
      -v ON_ERROR_STOP=1 \
      -v target="$TARGET" \
      -v execute="$EXECUTE" \
      -v older_than_days="${OLDER_THAN_DAYS:-}" \
      -X \
      --echo-errors \
      <<<"$sql"
  else
    docker compose exec -T "$SERVICE" psql \
      -U "$DB_USER" \
      -d "$DB_NAME" \
      -v ON_ERROR_STOP=1 \
      -v target="$TARGET" \
      -v execute="$EXECUTE" \
      -v older_than_days="${OLDER_THAN_DAYS:-}" \
      -X \
      --echo-errors \
      <<<"$sql"
  fi
}

read -r -d '' SQL <<'SQL' || true
\pset pager off
\pset tuples_only off
\pset format aligned

CREATE TEMP TABLE cleanup_params AS
SELECT
  :'target'::text AS target,
  :'execute'::boolean AS execute,
  NULLIF(:'older_than_days', '')::integer AS older_than_days;

SELECT target, execute, older_than_days
FROM cleanup_params;

CREATE TEMP TABLE cleanup_candidates (
  table_name text PRIMARY KEY,
  rows_count bigint NOT NULL
);

INSERT INTO cleanup_candidates(table_name, rows_count)
SELECT 'documents', count(*)
FROM documents, cleanup_params
WHERE target IN ('all', 'documents')
  AND (
    older_than_days IS NULL
    OR created_at < now() - make_interval(days => older_than_days)
  );

INSERT INTO cleanup_candidates(table_name, rows_count)
SELECT 'conversations', count(*)
FROM conversations, cleanup_params
WHERE target IN ('all', 'conversations')
  AND (
    older_than_days IS NULL
    OR created_at < now() - make_interval(days => older_than_days)
  );

SELECT table_name, rows_count
FROM cleanup_candidates
WHERE rows_count > 0
ORDER BY table_name;

WITH deleted AS (
  DELETE FROM documents
  USING cleanup_params
  WHERE execute
    AND target IN ('all', 'documents')
    AND (
      older_than_days IS NULL
      OR created_at < now() - make_interval(days => older_than_days)
    )
  RETURNING 1
)
SELECT 'documents_deleted' AS result, count(*) AS rows_count
FROM deleted;

WITH deleted AS (
  DELETE FROM conversations
  USING cleanup_params
  WHERE execute
    AND target IN ('all', 'conversations')
    AND (
      older_than_days IS NULL
      OR created_at < now() - make_interval(days => older_than_days)
    )
  RETURNING 1
)
SELECT 'conversations_deleted' AS result, count(*) AS rows_count
FROM deleted;

SELECT
  CASE
    WHEN execute THEN 'cleanup_completed'
    ELSE 'dry_run_only_add_--execute_to_delete'
  END AS result
FROM cleanup_params;
SQL

run_psql "$SQL"
