Moth Hunt (Unity • 2.5D • URP)

A student team project built in Unity using 3D environments + 2D sprites with the Universal Render Pipeline (URP).

This document is written for everyone on the team, including artists and designers who may be new to Git or Unity project conventions.

Requirements

Unity: 6000.2.4f1 (everyone must install this exact version)
Git + Git LFS installed (LFS = “Large File Storage” for big art/audio files)
GitHub account with access to this repository

Why exact Unity version? Unity projects change format between versions. If we all use the same version, we avoid import errors and weird pink shaders.

What is Git? What is Git LFS? (Plain-English)

Git is shared history for our project. It saves snapshots (commits) of files so we can go back in time and work together safely.

GitHub is the website that hosts our Git repository so everyone can download (clone) and contribute.

Git LFS (Large File Storage) is an add-on that stores big files (PNGs, PSDs, FBXs, WAVs, scenes, prefabs, etc.) efficiently. Without LFS, pushes can fail (files too big), or teammates download tiny “pointer files” instead of the real assets.

Mental model:

Git = the shared notebook.

GitHub = the bookshelf where the notebook lives.

LFS = the big plastic folder where we keep heavy art files so the notebook doesn’t tear.

Getting Started (Clone & Open)

Option A — GitHub Desktop (recommended for artists/designers)

Install GitHub Desktop and log in.

Click “Clone a repository from the Internet…”.

URL: https://github.com/crimsonbeluga/Moth-Hunt.git

Choose a local folder and click “Clone”.

After cloning, open a terminal from the repository and run:
git lfs install
git lfs fetch --all
git lfs checkout
(Why? This ensures large art files are actually downloaded, not just pointers.)

Open Unity Hub → Add → select your local “Moth-Hunt” folder → Open.

First import may take a few minutes. That’s normal.

Option B — Command line (for anyone comfortable with commands)

Clone the repository:
git clone https://github.com/crimsonbeluga/Moth-Hunt.git

cd Moth-Hunt

Install LFS (first time on each machine):
git lfs install

Fetch and checkout large files:
git lfs fetch --all
git lfs checkout

Open the folder in Unity Hub → Open.

If Unity reports missing URP settings: make sure Packages/ and ProjectSettings/ exist in your local clone (they are committed). Reopen Unity. If still pink, check Project Settings → Graphics/Quality to verify URP assets are assigned.

Project Structure (What goes where and why)

Assets/_Project/
• Art/ — All visual assets.
– Characters_2D/ (sprites for player, allies)
– Enemies_2D/ (sprites for humans, traps)
– Environment_3D/ (meshes, textures, materials for world)
– Props_3D/ (small placeable 3D items)
– UI/ (icons, buttons, HUD sprites)
– Materials/ (shared materials)
– Shaders/ (shader graphs or custom shaders)
Why: Artists know exactly where to put new art. Programmers can quickly find assets.

• Audio/ — Music, SFX, and audio mixer.
Why: Keeps sound assets separate and easy to mix.

• Code/ — All scripts and tests.
– Runtime/ (gameplay code that ships in builds)
– Editor/ (tools that only run inside Unity Editor)
– Tests/ (automated checks)
Why: Separating Runtime vs Editor avoids build issues and speeds up compile.

• Prefabs/ — Reusable game objects.
– Characters/, Enemies/, Environment/, Props/, UI/
Why: “Everything reusable is a Prefab” prevents scene conflicts and keeps iteration fast.

• Scenes/ — Unity scene files.
– Game.unity (main game level)
– Sandbox.unity (dev testing only, never shipped)
– MainMenu.unity (added later)
Why: One main scene to keep things simple; Sandbox to test your work safely.

• Settings/ — Project-wide configuration.
– InputActions.inputactions (new Input System)
– URP/ (URP pipeline asset, renderers, global settings, volume profiles)
– Quality.asset, Physics.asset
Why: One place for all project settings so nothing gets lost.

• UI/ — Our UI prefabs and assets.
– Screens/ (full screens like HUD, Pause)
– Widgets/ (reusable buttons, prompts)
– Fonts/ (TextMesh Pro font assets + source TTF/OTF)
– Styles/ (shared color/typography settings)
Why: Clean separation makes UI easy to theme and reuse.

• VFX/ — Particle systems, special visual effects.
Why: Central folder for visual polish.

Rule of thumb:

Raw sprites/textures go in Art/.

Gameplay objects made from art go in Prefabs/.

Place prefabs into Scenes/.

Scenes We Use (and the simple rule)

Game.unity — main gameplay. Only one person edits this at a time.
Sandbox.unity — for testing your work (your playground). Keep it out of Build Settings.
MainMenu.unity — we’ll add later.

Why one person per scene? Scene files are huge text files. If two people edit the same scene, Git can’t merge cleanly and things break. Working in Prefabs lets many people work in parallel safely.

Scene ownership policy:

Announce in chat when you “take” Game.unity.

git pull before you start.

Make changes, save, push, then say “Game.unity is free.”
Everyone else: work in Prefabs or Sandbox.

Version Control Basics (What to click/do every day)

Daily workflow (concept):

Pull latest changes before you start work.
GitHub Desktop: “Fetch origin” then “Pull”.
Command line: git pull

Create a feature branch for your task.
GitHub Desktop: Branch → New Branch… (name it like feature/enemy-ai)
Command line: git checkout -b feature/enemy-ai

Make small, focused changes.

Save and commit.
GitHub Desktop: Write a short summary → Commit to feature/enemy-ai.
Command line: git add . then git commit -m "Add enemy patrol and vision cone"

Push and open a Pull Request (PR) on GitHub.
GitHub Desktop: Push origin → “Create Pull Request”.
Command line: git push -u origin feature/enemy-ai, then open GitHub and create PR.

After your PR is reviewed and merged, clean up:
git checkout main
git pull
git branch -d feature/enemy-ai

Why Pull Requests (PRs)? Another teammate reviews your changes before they land in main so we keep the game stable and avoid surprises.

Never commit directly to main. Always use a feature branch and PR.

Branching Model (Names and purpose)

main — protected, stable; we demo/build from this.
develop — optional integration branch if needed.
feature/... — your task branches (feature/player-movement, feature/enemy-ai).
hotfix/... — urgent fixes off main if something breaks after a merge.

Naming tips:

Keep it short and descriptive.

Use dashes, not spaces (feature/moth-dash, feature/human-guard-sprite).

Commit Messages (What to write and why)

Use short, imperative subjects (a short action phrase), optional detail in the body.

Good examples:
Add enemy patrol with vision cone
Hook up light collectible to UI counter
Fix URP renderer assignment in Quality settings

Why? Clean history helps everyone understand what changed without opening files.

LFS: Locking Large Art Files (Optional but helpful)

If you’re editing a binary asset that others might also touch (PSD, PNG, FBX, WAV), you can “lock” it so nobody else edits it at the same time.

Lock example:
git lfs lock Assets/_Project/Art/Characters_2D/Sprites/Player/Idle/Player_Idle.png

Unlock when done:
git lfs unlock Assets/_Project/Art/Characters_2D/Sprites/Player/Idle/Player_Idle.png

This avoids “last write wins” confusion on critical art files.

Our Project Conventions (Input, Layers, Rendering)

Input System: New Input System (file lives at Settings/InputActions.inputactions). This lets us map keyboard/mouse and controllers in one place.

Physics Layers (examples): Player, Enemy, Interactable, Collectible, Environment, Ground, Projectile, Triggers, Climbable, Raycast, UI, LightingFX
Purpose: collision and raycasting rules (who bumps into who or what an interaction ray can hit).

Sorting Layers (2D draw order): Background → Environment_Back → Ground → Environment_Front → Interactable → Collectible → Enemy → Player → Projectile → VFX_Back → UI_World → VFX_Front → UI
Purpose: controls which sprites appear in front or behind others.

URP Rendering Layers (lighting control): Default, Player, Enemy, Environment, Collectible, Interactable, UI_World, FX/Lighting
Purpose: decide which lights affect which things (e.g., collectibles glow without lighting the whole level).

Rule of thumb:

Sorting Layers = draw order (what’s in front visually).

Rendering Layers = who gets lit by which lights (URP feature).

Physics Layers = collisions and raycasts (gameplay).

Do’s and Don’ts (to prevent headaches)

Do
• Convert objects to Prefabs before placing them in a scene.
• Keep assets in the correct folders (don’t dump files at the root).
• Commit small chunks with clear messages.
• Communicate when you’re taking Game.unity.
• Pull before you work.
• Use branches and PRs.

Don’t
• Edit Game.unity at the same time as someone else.
• Commit Library/, Temp/, or other cache folders (our .gitignore blocks them).
• Rename or move shared assets without telling the team (Unity references can break).
• Push directly to main.

Building the Game (for tests/demos)

Unity: File → Build Settings
• Scenes in Build: Game (and later MainMenu)
• Sandbox stays out of the build (dev only)

If the build is pink or looks wrong: check that URP assets are assigned in Project Settings → Graphics and Quality.

Troubleshooting (Common problems and fixes)

I pushed and GitHub says “file > 100MB”
• The file type may not be tracked by LFS yet. Add the extension to .gitattributes and migrate:
git lfs migrate import --include="*.EXT"
git push --force-with-lease

I see tiny “pointer files” instead of real art
• You forgot to install or fetch with LFS. Run:
git lfs install
git lfs fetch --all
git lfs checkout

CRLF/LF warnings on Windows
• Set this once:
git config core.autocrlf true

Unity shows pink materials or missing pipeline
• Ensure Packages/ and ProjectSettings/ are committed (they are).
• Reopen Unity; verify URP assets in Graphics and Quality.
• If needed, reassign the URP Render Pipeline Asset under Project Settings → Graphics.

Pull failed because I have local changes
• You probably edited the same file someone changed upstream. Options:
– Commit your work on a branch first (recommended).
– Or stash your changes: git stash, then git pull, then git stash pop.
– If it’s a scene conflict, pick one version, then re-apply the other change via prefabs.

Git says “nothing to commit” or “switch -m requires a value”
• git commit needs a message in quotes. Example:
git commit -m "Add collectible glow VFX"


Quick Commands (reference)

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

Open PR on GitHub, get review, merge.

Sync and clean up after merge:
git checkout main
git pull
git branch -d feature/thing

Lock a big art file while editing (optional):
git lfs lock path/to/file
When done:
git lfs unlock path/to/file
