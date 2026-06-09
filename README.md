# XR UI UX Team Speed - VR Racing Game

This repository is a submission-focused code archive for a Unity VR racing game project.

The original Unity project contains large third-party assets, vehicle models, track assets, textures, audio files, Unity cache folders, and VR package data. To keep the GitHub repository lightweight for assignment review, this repository includes the core implementation scripts and documentation rather than the full executable Unity project.

## Project Summary

This project is a VR racing game built with Unity, Universal Render Pipeline, Meta Quest 3, and Logitech G29 Driving Force Racing Wheel.

Players start in a VR lobby, choose either Practice or Quick Match, select Automatic or Manual transmission, and then drive in a cockpit-style racing experience. The project includes a tutorial track, AI opponent race, G29 wheel/pedal/button input, race HUD, minimap, lap timing, ranking, pause menu, result UI, and manual transmission behavior.

## Included Contents

- `Scripts/`: Core C# scripts used in the Unity project
- `Scripts/Editor/`: Unity editor helper scripts used during scene setup
- `PROJECT_PRESENTATION_BRIEF.md`: Detailed project explanation for PPT generation
- `Packages/manifest.json`: Unity package dependency reference

## Not Included

The following are intentionally excluded because this repository is for assignment/code review, not full project distribution:

- Unity `Library/`, `Temp/`, `Logs/`, `obj/` cache folders
- Large models, textures, audio, and third-party asset packages
- Build output files
- Full scene asset dependency tree

Because of this, cloning this repository alone will not reproduce the full playable Unity project. It is intended to document and review the implementation.

## Main Features

- Meta Quest 3 VR cockpit racing experience
- Logitech G29 steering wheel, pedal, D-pad, and button support
- Lobby selection for Practice and Quick Match
- Automatic and manual transmission modes
- Manual clutch + gear up/down system
- WheelCollider-based player vehicle controller
- AI opponent vehicles using waypoint path following
- Race countdown, lap counter, timer, lap time list, ranking, and finish UI
- World-space VR HUD and pause menu controlled by G29 inputs
- In-car minimap displayed on a vehicle Quad through RenderTexture
- Tutorial mode with sequential driving instructions
- Vehicle reset system for off-road recovery
- Engine sound and BGM handling

## Key Scripts

- `CarController.cs`: Player vehicle physics, G29 input, acceleration, braking, steering, reverse, gauges, off-road power penalty
- `CarTransmissionController.cs`: Automatic/manual transmission selection, clutch input, gear limits, manual torque calculation
- `RaceSessionManager.cs`: Race flow, HUD, lap timing, ranking, result UI, tutorial mode, minimap setup, audio master control
- `CarControllerWaypointAi.cs`: AI car waypoint following and race start/finish behavior
- `RaceMinimapController.cs`: Runtime minimap camera, RenderTexture, player/opponent dots
- `G29PauseMenu.cs`: G29/keyboard pause menu input and UI control
- `LobbyLicensePrompt.cs`: Automatic/manual selection prompt before scene loading
- `WheelNavManager.cs`: Lobby card selection with G29 D-pad and submit button
- `CarResetController.cs`: Vehicle respawn/reset behavior

## Scene Flow

```text
LobbyScene
  -> Practice card
    -> Automatic / Manual prompt
    -> MainTrack tutorial scene

  -> Quick Match card
    -> Automatic / Manual prompt
    -> RealMainTrack race scene
```

## Presentation Brief

For a full project explanation and suggested PPT slide structure, see:

`PROJECT_PRESENTATION_BRIEF.md`
