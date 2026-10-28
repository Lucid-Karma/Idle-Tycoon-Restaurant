#!/usr/bin/env bash
# Uploads a beyzosh.com web build (Tools/Chibi Cafe/Build for beyzosh.com, CrazyGamesBuild.BuildWeb) to R2:
# https://games.beyzosh.com/chibi-burger-cafe/index.html (bucket cozygameportaldeneme). Needs `npx wrangler login`
# (or CLOUDFLARE_API_TOKEN). usage: deploy-beyzosh-r2.sh "<build folder>"
# The .unityweb files are brotli: uploaded with "Content-Encoding: br" so the browser unpacks them natively (the
# build's JavaScript fallback is only a safety net - it is much slower). Hashed build files are cached for a year;
# StreamingAssets (music, shop pictures: fixed names) for an hour; index.html last, never cached, so a visitor
# only ever gets the new page once every file it names is already up.
set -euo pipefail
cd "$1"
BUCKET=cozygameportaldeneme
PREFIX=chibi-burger-cafe
YEAR="public, max-age=31536000, immutable"
HOUR="public, max-age=3600"
# Each upload is tried up to 4 times: the R2 API now and then answers "502: Bad Gateway" (2026-10-21, twice in a row).
put() { local f="$1" type="$2" cache="$3"; shift 3
        for try in 1 2 3 4; do
          npx wrangler r2 object put "$BUCKET/$PREFIX/$f" --file "$f" --content-type "$type" --cache-control "$cache" --remote "$@" && return 0
          echo "retrying $f ($try)"; sleep $((try * 5))
        done
        return 1; }
typeof() { case "$1" in
  *.png) echo image/png;; *.ico) echo image/x-icon;; *.css) echo "text/css; charset=utf-8";;
  *.js) echo "application/javascript; charset=utf-8";; *.m4a) echo audio/mp4;; *.json) echo application/json;;
  *) echo application/octet-stream;; esac; }

for f in Build/*; do
  case "$f" in
    *.data.unityweb) put "$f" application/octet-stream "$YEAR" --content-encoding br;;
    *.wasm.unityweb) put "$f" application/wasm "$YEAR" --content-encoding br;;
    *.framework.js.unityweb) put "$f" "application/javascript; charset=utf-8" "$YEAR" --content-encoding br;;
    *.loader.js) put "$f" "application/javascript; charset=utf-8" "$YEAR";;
    *) put "$f" "$(typeof "$f")" "$YEAR";;
  esac
done
find TemplateData StreamingAssets -type f ! -name "*.meta" | while read -r f; do put "$f" "$(typeof "$f")" "$HOUR"; done
put index.html "text/html; charset=utf-8" "no-cache"
echo "Deployed: https://games.beyzosh.com/$PREFIX/index.html"
