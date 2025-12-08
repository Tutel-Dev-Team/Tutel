#!/bin/bash

root="$1"

src="$root"".githooks/pre-commit"
dest="$root"".git/hooks/pre-commit"

if [[ ! -f "$dst" ]]
then
  cp "$src" "$dest"
  chmod +x "$dest"
fi 