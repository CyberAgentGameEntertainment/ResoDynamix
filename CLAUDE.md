# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ResoDynamix is a Unity package that renders a scene's base camera at a reduced resolution while keeping overlay cameras (typically UI) at full resolution, then composites the two. It targets Unity 6 with URP's **Render Graph** pipeline. Unity 2022 and URP "Compatible mode" support lives on the `compatible-mode` branch — do not port that path into `main`.

- Unity: `6000.0.61f1` (see `ProjectSettings/ProjectVersion.txt`)
- URP: `com.unity.render-pipelines.universal` 17.0.4 (Render Graph only)
- Published package source: `Assets/ResoDynamix/` (package id `jp.co.cyberagent.reso-dynamix`)
- Demo scenes: `Assets/Demo/Demo01..Demo04*.unity`

## Architecture

Three pieces cooperate; the names matter because the renderer feature looks up the controller by camera identity each frame.

1. **`ResoDynamix`** (`Assets/ResoDynamix/Runtime/Scripts/ResoDynamix.cs`) — scene singleton (`Instance`). Holds the list of `ResoDynamixController`s and exposes `FindController(Camera)` used by the render pass to map any camera (base **or** stacked overlay) back to its controller.
2. **`ResoDynamixController`** — owns the per-base-camera state: two scale knobs (`BaseCameraRenderScale`, `ResultRenderScale`), the allocated `RTHandle`s, and the discovered overlay-camera list. `Update()` rediscovers the camera stack every frame (calls `Disable()` first to release last frame's allocations and restore overlay culling masks), then allocates intermediate textures and **zeroes the overlay cameras' `cullingMask`** when `UseResultRTHandle` is true so URP's normal stack pass doesn't draw them — they will be drawn manually into the result texture instead.
3. **`CreateHybridResolutionImageFeature`** (`ScriptableRendererFeature`) — must be added to the URP Renderer Data asset used by the scene. `AddRenderPasses` branches on whether the current camera is the base camera or an overlay camera of an active controller.

### Render pass flow per frame

For the **base camera**, two passes run:
- `SetupBaseCameraRenderingPass` (event configurable, default `BeforeRenderingOpaques`) — rebinds `UniversalResourceData.cameraColor/cameraDepth` to the controller's downscaled `BaseCameraColorRTHandle`/`BaseCameraDepthRTHandle` and writes `_ScaledScreenParams` so shaders that read it see the reduced resolution. The original handles are saved into a `BaseCameraContextItem` for the next pass.
- `BlitBaseCameraImageToResultTexturePass` (event `AfterRendering`) — blits the downscaled result either to the **shared result texture** (when `ResultRenderScale < 1`, i.e. `UseResultRTHandle`) or back to the original camera color. Then restores `cameraColor/cameraDepth` to the originals via the context item.

For **overlay cameras** (only when `UseResultRTHandle`):
- `DrawOverlayCameraImageToResultTexturePass` (event `BeforeRenderingTransparents`) — re-enables the saved overlay culling mask, manually runs culling via `ResoDynamixController.ScriptableRenderContext` (captured from `RenderPipelineManager.beginCameraRendering`), builds opaque + transparent renderer lists, draws into the shared `ResultRTHandle` (optionally with a depth attachment if `UseDepthTextureWithOverlayCamera`), then blits the result to the final back buffer.

### Why the two scale parameters differ

- `BaseCameraRenderScale < 1` is the cheap path: only the base camera renders downscaled, overlays render directly at full resolution.
- `ResultRenderScale < 1` additionally allocates a result intermediate texture and forces overlays through the manual draw pass — useful for lowering UI cost but **costs an extra full-res-ish RT**.

If both scales are `>= 1`, `IsEnable` returns false and the feature short-circuits — no allocations, no passes.

### `AddBlitPassCustom`

All blits go through `CommonHelper.AddBlitPassCustom` (`RenderPipeline/CommonHelper.cs`) which wraps `Blitter.BlitTexture` inside a raster render pass with a named `passName`. Use this — not `renderGraph.AddBlitPass` — so the pass shows up with a meaningful name in Render Graph Viewer; pass names are how this codebase keeps the graph readable. (The recent rename from `AddBlitPass` to `AddBlitPassCustom` was the point of commit `cd03b17`.)

## Tests

Tests live in `Assets/Tests/` and are gated by the `UNITY_INCLUDE_TESTS` define (see `Tests.asmdef`). They are **image-regression tests**: each demo scene is loaded, screen-captured, and compared against a reference PNG using NVIDIA's **FLIP** binary bundled under `Assets/Tests/bin/{Windows,Mac}/`.

- Run via Unity Test Runner (PlayMode tests). The parametric cases in `AverageTest.cs` cover `Demo01_Standard` through `Demo04_MultiCamera` at 1920x1080.
- **macOS first-time setup**: run `./setup.sh` from the repo root to strip `com.apple.quarantine` from `Assets/Tests/bin/Mac/flip`, otherwise the process launch fails silently.
- Reference images are platform-specific. They live under `Assets/Tests/SuccessfulImages/Linear/{WindowsEditor/Direct3D11,OSXEditor_AppleSilicon/Metal}/None/`. To refresh references after an intentional rendering change, run the tests once (failures dump actuals to `Assets/ActualImages/...`), then use the editor menu **Window > ResoDynamix > Test > Copy AverageTest Result** (defined in `AverageTest.cs`) to copy actuals into `SuccessfulImages/`.
- The pass criterion is `result.mean < settings.AverageCorrectnessThreshold` (currently `0.01`) where `mean` is the FLIP weighted mean of the perceptual diff in the Jzazbz color space — not pixel-equality.

## Common pitfalls

- `ResoDynamix` enforces a single instance via `Debug.Assert` in `Awake`. Adding a second one to a scene is a silent test failure waiting to happen.
- Every controller placed in a scene must be registered in the singleton's `Controllers` list — `FindController` only walks that list; an unregistered controller renders normally with no resolution change.
- `ResoDynamixController.Update` calls `Disable()` unconditionally at the top, so overlay-camera mutations (`targetTexture`, `cullingMask`) are applied for one frame at a time. Don't cache references into `_overlayCameras` from outside.
- The `[Obsolete]` `RenderScale` property forwards to `BaseCameraRenderScale` and exists only for migration; new code should use the new name.
- `DeprecationMessage.CompatibilityScriptingAPIObsolete` is the canonical string for marking APIs that only exist in the compatibility/non-RenderGraph path — use it on `[Obsolete]` attributes rather than inventing new messages.
