Moth Hunt (Unity • 2.5D • URP)

A student team project built in Unity using 3D environments + 2D sprites with the Universal Render Pipeline (URP).
This document is written for everyone on the team, including artists and designers who may be new to Git or Unity project conventions.

✅ Requirements

Unity: 6000.2.4f1 (everyone must install this exact version)

Git + Git LFS installed (LFS = “Large File Storage” for big art/audio files)

GitHub account with access to this repository

Why exact Unity version?
Unity projects change format between versions. If we all use the same version, we avoid import errors and weird pink shaders.

🧠 What is Git? What is Git LFS? (Plain-English)

Git = shared history for our project. It saves snapshots (commits) so we can go back in time and work together safely.

GitHub = the website that hosts our Git repository so everyone can download (clone) and contribute.

Git LFS = an add-on that stores big files (PNGs, PSDs, FBXs, WAVs, scenes, prefabs, etc.) efficiently.

Mental model:
Git = the shared notebook • GitHub = the bookshelf • LFS = the big plastic folder for heavy art so the notebook doesn’t tear.

🚀 Getting Started (Clone & Open)
Option A — GitHub Desktop (recommended for artists/designers)

Install GitHub Desktop and log in.

Click “Clone a repository from the Internet…”.

URL: https://github.com/crimsonbeluga/Moth-Hunt.git

Choose a local folder → Clone.

Open a terminal in the repo folder and run:

git lfs install

git lfs fetch --all

git lfs checkout
(Ensures large art files download, not just tiny “pointer files.”)

Open Unity Hub → Add → select your local Moth-Hunt folder → Open.

First import may take a few minutes. (Normal!)

Option B — Command line

Clone the repository:
git clone https://github.com/crimsonbeluga/Moth-Hunt.git
cd Moth-Hunt

Install LFS (first time): git lfs install

Fetch and checkout large files:
git lfs fetch --all
git lfs checkout

Open the folder in Unity Hub → Open.

If Unity reports missing URP settings: make sure Packages/ and ProjectSettings/ exist. Reopen Unity.
If still pink, check Project Settings → Graphics/Quality and verify URP assets are assigned.

🗂️ Project Structure (What goes where and why)


Assets/_Project/


  Art/              # All visual assets
    Characters_2D/  # Player & allies sprites
    Enemies_2D/     # Human & trap sprites
    Environment_3D/ # Meshes, textures, materials for world
    Props_3D/       # Small placeable 3D items
    UI/             # Icons, buttons, HUD sprites
    Materials/      # Shared materials
    Shaders/        # Shader graphs or custom shaders

  Audio/            # Music, SFX, AudioMixer

  Code/
    Runtime/        # Gameplay code that ships in builds
    Editor/         # Tools that only run in the Unity Editor
    Tests/          # Automated checks

  Prefabs/          # Reusable game objects (Characters, Enemies, Environment, Props, UI)

  Scenes/
    Game.unity      # Main game level
    Sandbox.unity   # Dev testing only (never shipped)
    MainMenu.unity  # Added later

  Settings/
    InputActions.inputactions
    URP/            # URP pipeline asset, renderers, global settings, volume profiles
    Quality.asset, Physics.asset

  UI/
    Screens/        # Full screens (HUD, Pause, etc.)
    Widgets/        # Reusable buttons, prompts
    Fonts/          # TextMesh Pro font assets + source TTF/OTF
    Styles/         # Shared color/typography settings

  VFX/              # Particle systems, special visual effects


Rule of thumb:

Raw art (sprites/textures) → Art/

Gameplay objects made from art → Prefabs/

Place prefabs into Scenes/

🎭 Scenes We Use (and the simple rule)

Game.unity — main gameplay. Only one person edits this at a time.

Sandbox.unity — your playground to test work. Keep it out of Build Settings.

MainMenu.unity — we’ll add later.

Why one person per scene?
Unity scenes are huge text files. If two people edit the same scene, Git can’t merge cleanly and things break. Using Prefabs lets many people work in parallel safely.

Scene Ownership Policy

Announce in chat when you “take” Game.unity.

Pull before editing.

Make changes → save → push → say “Game.unity is free.”

Everyone else works in Prefabs or Sandbox.

🔁 Version Control Basics (What to do every day)
Daily workflow

Pull latest changes

GitHub Desktop: Fetch origin → Pull

CLI: git pull

Create a feature branch for your task

Desktop: Branch → New Branch… (feature/enemy-ai)

CLI: git checkout -b feature/enemy-ai

Make small, focused changes

Commit

Desktop: write a short summary → Commit to feature/enemy-ai

CLI: git add . → git commit -m "Add enemy patrol and vision cone"

Push & open a PR (Pull Request) on GitHub

Desktop: Push origin → Create Pull Request

CLI: git push -u origin feature/enemy-ai, then open GitHub and create PR

After review & merge, sync and clean up:

git checkout main

git pull

git branch -d feature/enemy-ai

Why PRs? A teammate reviews your changes before they land in main so we keep the game stable and avoid surprises.
Never commit directly to main. Always use a branch + PR.

🌿 Branching Model (Names & purpose)

main — protected, stable; we demo/build from this

develop — optional integration branch if needed

feature/... — task branches (e.g., feature/player-movement, feature/enemy-ai)

hotfix/... — urgent fixes off main

Naming tips: short, descriptive, use dashes (e.g., feature/moth-dash, feature/human-guard-sprite).

✍️ Commit Messages (What to write & why)

Use short, imperative subjects; optional detail in the body.

Good examples:

Add enemy patrol with vision cone

Hook up light collectible to UI counter

Fix URP renderer assignment in Quality settings

Why? Clean history helps everyone understand what changed quickly.

🔒 LFS: Locking Large Art Files (optional but helpful)

If you’re editing a binary asset others might touch (PSD, PNG, FBX, WAV), you can lock it so nobody else edits it at the same time.

Lock: git lfs lock Assets/_Project/Art/Characters_2D/Sprites/Player/Idle/Player_Idle.png

Unlock: git lfs unlock Assets/_Project/Art/Characters_2D/Sprites/Player/Idle/Player_Idle.png

Avoids “last write wins” on critical art files.

🎛️ Our Project Conventions (Input, Layers, Rendering)

Input System: New Input System (file at Settings/InputActions.inputactions)
Map keyboard/mouse & controllers in one place.

Physics Layers (examples): Player, Enemy, Interactable, Collectible, Environment, Ground, Projectile, Triggers, Climbable, Raycast, UI, LightingFX
Purpose: collisions & raycasts (who bumps into who; what an interaction ray can hit).

Sorting Layers (2D draw order):
Background → Environment_Back → Ground → Environment_Front → Interactable → Collectible → Enemy → Player → Projectile → VFX_Back → UI_World → VFX_Front → UI
Purpose: which sprites render in front/behind.

URP Rendering Layers (lighting control):
Default, Player, Enemy, Environment, Collectible, Interactable, UI_World, FX/Lighting
Purpose: which URP lights affect which objects (e.g., collectibles glow without lighting the whole level).

Rule of thumb:
Sorting Layers = visual draw order • Rendering Layers = lighting control • Physics Layers = gameplay collisions/raycasts

✅ Do’s and Don’ts (to prevent headaches)

Do

Convert objects to Prefabs before placing in a scene

Keep assets in the correct folders

Commit small chunks with clear messages

Communicate when you’re taking Game.unity

Pull before you work

Use branches and PRs

Don’t

Edit Game.unity at the same time as someone else

Commit Library/, Temp/, or other caches (our .gitignore blocks them)

Rename/move shared assets without telling the team

Push directly to main

🏗️ Building the Game (tests/demos)

Unity: File → Build Settings

Scenes in Build: Game (and later MainMenu)

Exclude: Sandbox (dev only)

If the build is pink/wrong: check Project Settings → Graphics/Quality and assign URP assets.

🆘 Troubleshooting (Common fixes)

“file > 100MB” on push

That file type might not be tracked by LFS. Add extension to .gitattributes and migrate:

git lfs migrate import --include="*.EXT"

git push --force-with-lease

I see pointer files, not real art

Run: git lfs install → git lfs fetch --all → git lfs checkout

CRLF/LF warnings on Windows

Run once: git config core.autocrlf true

Pink materials / missing pipeline

Ensure Packages/ and ProjectSettings/ exist

Reopen Unity; verify URP assets in Graphics and Quality

Reassign the URP Render Pipeline Asset if needed

Pull failed: local changes

You edited a file someone else changed.

Commit your work on a branch (recommended)

Or stash: git stash → git pull → git stash pop

If it’s a scene conflict, pick one version, then re-apply the other change via prefabs

Commit error: “switch -m requires a value”

You forgot the message quotes. Example: git commit -m "Add collectible glow VFX"




🧾 Quick Commands (reference)

Clone:
git clone https://github.com/crimsonbeluga/Moth-Hunt.git
cd Moth-Hunt

First-time LFS setup:
git lfs install
git lfs fetch --all
git lfs checkout

New task branch:
git checkout -b feature/thing

Save work:
git add .
git commit -m "Do the thing"

Push branch:
git push -u origin feature/thing
(Open PR on GitHub, get review, merge.)

Sync & clean up after merge:
git checkout main
git pull
git branch -d feature/thing

Lock big art file (optional):
git lfs lock path/to/file
Unlock when done:
git lfs unlock path/to/file
