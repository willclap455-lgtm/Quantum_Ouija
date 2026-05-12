# Quantum Ouija Architecture

## Rendering framework recommendation

Use **MonoGame DesktopGL** for the prototype.

The requested feel depends on continuous movement, delta-time interpolation, visual trails, later audio hooks, and atmospheric effects. MonoGame provides an explicit `Update`/`Draw` loop, GPU-backed sprite rendering, texture control, and a natural upgrade path for particles, glow, shaders, and sound effects. WPF is useful for UI-heavy tools, but a custom WPF canvas would recreate many game-loop concerns that MonoGame already solves.

## Project structure

```text
src/QuantumOuija
  Audio/          Future ambient, whisper, movement sound hooks
  Board/          Board coordinate system, semantic regions, hit testing
  Input/          Question text input buffer
  Persistence/    Replay/session export records
  Rendering/      Board, trail, planchette, debug, bitmap text rendering
  Rng/            CURBy client, fallback/seeded RNG, provider abstraction
  Simulation/     Grid movement, path generation, response rules
```

## Board model

The board image is treated as a logical coordinate space, not just a background. The `BoardModel` owns:

- board pixel dimensions,
- grid spacing,
- maximum grid node extents,
- semantic regions,
- a default token for undefined board space.

The current `MiltonBradleyBoardLayout` defines normalized rectangles for YES, NO, GOODBYE, A-Z, and 0-9. These normalized regions scale to the actual `ouija_board.jpg` dimensions. `BoardRegion` stores polygon vertices, priority, token, bounds, and point-in-polygon hit testing, so future custom boards can use arbitrary polygons.

Undefined board coordinates currently resolve to `SPACE`, which makes off-letter landings become word separators.

## RNG abstraction

`IQuantumRandomProvider` exposes async integer generation:

- `NextIntAsync(min, max, ct)`
- `NextIntsAsync(count, min, max, ct)`

Implementations:

- `CurbyQuantumRandomProvider`
  - async HTTP,
  - cancellation,
  - retry with exponential backoff,
  - request rate limiting,
  - entropy byte buffering/caching,
  - JSON/base64/hex/octet-stream payload normalization,
  - unbiased integer mapping by rejection sampling,
  - fallback provider on network failure.
- `FallbackRandomProvider`
  - cryptographic local RNG by default,
  - deterministic `Random` when constructed with a seed for replay.

This keeps gameplay systems independent of the quantum source and allows later providers such as saved-session replay, mock testing providers, hardware RNGs, or alternate web beacons.

## Quantum path generation

For each question:

1. Generate a quantum path count in `[70, 100]`.
2. Generate paths one at a time without blocking the render loop.
3. For each path:
   - quantum length in `[100, 500]`,
   - direction array length `N`, values `[1, 8]`,
   - distance array length `N`, values `[3, 8]`,
   - movement engine expands these arrays into grid nodes.

Direction mapping:

```text
1 North
2 East
3 South
4 West
5 Northwest
6 Northeast
7 Southwest
8 Southeast
```

`GridMovementEngine` clamps to board boundaries by default and can be configured for wraparound.

## Response rules

`ResponseBuilder` implements the special first-path rule:

- If path 0 resolves to YES, NO, or GOODBYE, response generation terminates immediately and the response is exactly that token.
- If path 0 resolves to a letter, number, or space, generation continues.
- Later YES/NO/GOODBYE hits are converted to spaces.
- Consecutive spaces are collapsed for readable emergent text.

## Render/update loop

`QuantumOuijaGame` owns runtime orchestration:

- accepts text input when idle,
- starts an async RNG count request,
- creates async path generation tasks as the animation advances,
- animates every generated node through `PlanchetteAnimator`,
- updates a fading `TrailRenderer`,
- appends semantic tokens only after the planchette rests at the final coordinate,
- exposes debug overlays.

Network work never runs on the draw/update path. The loop polls task completion, making CURBy latency visible as status text rather than a frozen window.

## Persistence and replay design

`SessionRecord` captures:

- question,
- final response,
- requested path count,
- RNG provider metadata,
- every generated path's direction and distance arrays,
- start/end nodes and resolved token.

Those records are enough to replay a session exactly without calling CURBy again. A later `ReplayRandomProvider` or direct `SessionReplayController` can feed saved arrays back into the movement engine.

## Future extension points

- Replace the current normalized Milton Bradley rectangles with a board calibration editor.
- Add real polygon overlays for curved text arcs.
- Save `SessionRecord` JSON from the UI.
- Add audio loops and movement/settling sounds through `AudioHooks`.
- Add shader-based glow and planchette shadowing.
- Add path prefetching with bounded queues if CURBy latency becomes visible.
- Add automated tests for region lookup, unbiased RNG mapping, first-token response rules, and movement boundary behavior.
