<html><body><h1>Quantum Ouija Board</h1></body></html>

# Quantum Ouija

Quantum Ouija is a MonoGame/.NET 8 desktop prototype that turns quantum random values into wandering planchette paths over a logical Ouija board grid.

## Why MonoGame

MonoGame DesktopGL was selected over WPF because the prototype needs a real update/render loop, delta-time movement, smooth sprite animation, trail rendering, and a clean path toward future audio, glow, particle, and shader effects. WPF can draw this, but MonoGame matches the requested game-loop architecture directly.

## Current prototype systems

- Board model with configurable grid spacing.
- Region mapping with priority-based polygon hit testing.
- Milton-Bradley-style board layout scaled from the included `assets/ouija_board.jpg`.
- Quantum RNG abstraction with CURBy API provider, pulse salt/SHA-512 derivation, retry/rate-limit handling, and local fallback RNG.
- Replay-friendly generated path records containing direction and distance arrays.
- Grid movement engine with boundary clamping and optional wraparound.
- Edge reflection so outward movement bounces the planchette back into the board instead of sticking in corners.
- Animated planchette movement over all generated nodes.
- Fading path trail.
- Debug toggles for grid, regions, and path nodes.
- Bitmap text overlay for question/response UI without requiring a MonoGame content pipeline font.

## Controls

- Type a question.
- `Enter`: ask / start a session.
- `Esc`: cancel an active session, or quit when idle.
- `F1`: toggle grid overlay.
- `F2`: toggle region overlay.
- `F3`: toggle generated path nodes.

## Build/run

This repository targets .NET 8+.

```bash
dotnet restore QuantumOuija.sln
dotnet run --project src/QuantumOuija/QuantumOuija.csproj
```

The Cursor cloud machine used for the initial implementation did not include the `dotnet` SDK. The solution was verified after installing .NET SDK 8.0.421 into the agent home directory.

## Architecture notes

See `docs/ARCHITECTURE.md`.

