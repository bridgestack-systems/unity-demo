# Nexus Arena

A Unity 6 multiplayer arena tech demo showcasing advanced gameplay systems, networking, XR support, and real-time data visualization. Built as a comprehensive client pitch prototype.

## Features

### Player & Camera
- Third-person character controller with sprint, jump buffering, and coyote time
- Cinemachine FreeLook camera with collision handling
- Inverse kinematics for realistic limb positioning
- Procedurally generated robot character prefab

### Physics
- Grab-and-throw system with velocity sampling and hover highlighting
- Destructible objects with fragment spawning
- Explosion force propagation
- Physics-based projectiles

### Multiplayer Networking
- Unity Netcode for GameObjects (host/client/server modes)
- Up to 8 players at 64 Hz tick rate
- Connection approval with password support
- In-game chat system and lobby management
- Player state synchronization via NetworkVariables and RPCs

### Animation
- StateMachineBehaviour-based animation controller with event callbacks
- Procedural animation generation
- Ragdoll physics system

### Environment
- Dynamic day/night cycle with configurable sun, ambient, and skybox gradients
- Procedural arena generation
- Particle effects manager

### XR (VR / AR)
- VR hand interaction with grab, throw, and haptic feedback (OpenXR)
- VR player rig with continuous move, snap turn, and teleportation
- AR surface detection and object placement (ARFoundation)

### Data Visualization
- Real-time stats tracking (score, kills, deaths, distance, FPS)
- Runtime graph renderer
- Leaderboard system with time-series data

### UI
- HUD with health bar, score, timer, crosshair, minimap, FPS counter, and notification queue
- Main menu, pause menu, and settings screens
- UI animation utilities

### Custom Shaders (URP / HLSL)
- **ArenaGrid** — Procedural grid floor with animated pulse and edge glow
- **Hologram** — Sci-fi holographic effect with scan lines, flicker, fresnel rim, and vertex glitch

## Tech Stack

| Component | Version |
|---|---|
| Unity | 6000.3.11f1 |
| URP | 17.0.3 |
| Input System | 1.11.2 |
| Netcode for GameObjects | 2.1.1 |
| Cinemachine | 2.10.1 |
| XR Interaction Toolkit | 3.0.7 |
| AR Foundation | 6.0.4 |
| OpenXR | 1.12.1 |
| ProBuilder | 6.0.4 |
| TextMeshPro | 3.2.0-pre.12 |

## Scenes

| Scene | Description |
|---|---|
| MainMenu | Title screen with settings, credits, and rotating preview platform |
| GameArena | Primary gameplay arena (100x100) with 4 spawn points, HUD, and Cinemachine camera |
| Lobby | Multiplayer lobby with player list, chat, ready system, and room codes |
| ARScene | AR experience with plane detection and object placement |
| VRScene | VR experience with hand controllers and locomotion |

## Building

### From the Unity Editor

Open the project in Unity 6, then use the menu:

**NexusArena > Build > [Target]**

### From the Command Line

```bash
# macOS
/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath . \
  -executeMethod NexusArena.Editor.BuildManager.BuildMacOS \
  -logFile build.log
```

Replace `BuildMacOS` with any target method:

| Method | Output | Backend |
|---|---|---|
| `BuildWindows` | `Build/Windows/NexusArena.exe` | IL2CPP |
| `BuildMacOS` | `Build/macOS/NexusArena.app` | Mono |
| `BuildLinux` | `Build/Linux/NexusArena` | IL2CPP |
| `BuildWebGL` | `Build/WebGL/` | — |
| `BuildAndroid` | `Build/Android/NexusArena.apk` | IL2CPP (ARM64) |
| `BuildIOS` | `Build/iOS/` | IL2CPP |

## First-Time Setup

On first editor launch, `NexusArenaSetup` automatically:
- Creates required tags (Player, Destructible, Interactable, SpawnPoint)
- Configures layers (Player, Interactable, Destructible, Projectile, UI, Environment)
- Prompts to generate all 5 scenes if they don't exist

Additional editor tools under the **NexusArena** menu:
- **Generate All Scenes** — Creates all 5 gameplay scenes
- **Generate Player Character** — Builds the robot prefab and places it in GameArena
- **Visual Polish > Setup All** — Configures materials, URP pipeline, lighting, and skybox

## Project Structure

```
Assets/
  Editor/           # Build pipeline, scene/character generators, setup tools
  Materials/        # URP materials (floor, walls, player, hologram, skybox)
  Prefabs/          # Player robot prefab
  Rendering/        # URP pipeline and renderer data assets
  Scenes/           # 5 gameplay scenes
  Scripts/
    Animation/      # State machine, procedural animation, ragdoll
    Core/           # GameManager, AudioManager, SceneController, GameConfig
    DataVisualization/ # Stats, graphs, leaderboard
    Environment/    # Day/night cycle, arena generation, particles
    Networking/     # Netcode manager, player sync, chat, lobby
    Physics/        # Grab/throw, destructibles, explosions, projectiles
    Player/         # Controller, animation, IK, camera
    UI/             # HUD, menus, settings, animator
    XR/             # VR hands, VR rig, AR placement
  Shaders/          # ArenaGrid.shader, Hologram.shader
```

## Download

Pre-built binaries are available on the [Releases](https://github.com/bridgestack-systems/unity-demo/releases) page.

Each release includes builds for:
- **Windows** (x64) — `NexusArena-StandaloneWindows64.zip`
- **macOS** — `NexusArena-StandaloneOSX.zip`
- **Linux** (x64) — `NexusArena-StandaloneLinux64.zip`
- **WebGL** — `NexusArena-WebGL.zip`

### Build Artifacts

Every push to `main` and every pull request triggers a CI build via GitHub Actions. You can download build artifacts from the [Actions](https://github.com/bridgestack-systems/unity-demo/actions) tab — click any workflow run and scroll to the **Artifacts** section.

### Creating a Release

Push a version tag to trigger a release build:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The workflow will build all 4 platforms, package them as zip files, and create a GitHub Release with the binaries attached.

> **Note:** The CI pipeline requires Unity license secrets (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`) configured in the repository's Settings > Secrets and variables > Actions.

## Game Configuration

Default values in `GameConfig.cs`:

| Setting | Value |
|---|---|
| Player Speed | 8 m/s |
| Jump Force | 12 |
| Grab Range | 3 m |
| Max Players | 8 |
| Network Tick Rate | 64 Hz |
| Round Duration | 300 s |
| Respawn Delay | 3 s |
| Projectile Speed | 40 m/s |
| Destruction Force | 500 |
