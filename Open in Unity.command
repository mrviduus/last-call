#!/bin/sh
# Double-click in Finder: opens this project in its Unity version (from ProjectSettings) with the Main scene.
cd "$(dirname "$0")" || exit 1
version=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)
unity="/Applications/Unity/Hub/Editor/$version/Unity.app"
if [ ! -d "$unity" ]; then
    echo "Unity $version not found. Install it from Unity Hub."
    exit 1
fi
open -a "$unity" --args -projectPath "$PWD" -openfile "$PWD/Assets/Main.unity"
