# Grouping & Routing Improvements

Based on competitor analysis of **Nodify-Avalonia** and **Blazor.Diagrams**.

## Community Version (FlowGraph)

### High Priority - UX Parity

- [x] **Group Movement Mode Toggle** ✅ COMPLETED
  - Add `GroupMovementMode` enum: `MoveWithChildren` (default), `MoveGroupOnly`
  - Shift+Drag on group header moves group without children (Nodify behavior)
  - Configurable via `FlowCanvasSettings.GroupMovementModeToggleKey`
  - Files: `GroupMovementMode.cs`, `FlowCanvasSettings.cs`, `DraggingState.cs`, `IdleState.cs`

- [x] **Visible Resize Handle Option** ✅ COMPLETED
  - Add optional corner resize thumb for groups
  - `ShowGroupResizeHandle` setting in `FlowCanvasSettings` (default: false for minimalism)
  - Triangle indicator in bottom-right corner when enabled
  - Files: `FlowCanvasSettings.cs`, `GroupNodeRenderer.cs`

- [x] **Group ZIndex Improvements** ✅ COMPLETED
  - Groups render behind their children by default (ZIndex = 150, below nodes at 300)
  - Added `CanvasElement.ZIndexGroups` constant
  - `GroupNodesCommand` sets ZIndex automatically
  - `FlowCanvasSettings.GroupDefaultZIndex` for customization
  - Files: `CanvasElement.cs`, `GroupCommands.cs`, `FlowCanvasSettings.cs`

### Medium Priority - Visual Polish

- [ ] **Group Header Customization**
  - `FlowCanvasTheme.GroupHeaderBackground` property
  - Distinct visual separation between header and content area
  - Option for header-only drag area vs entire group drag

- [ ] **Selection Helpers for Groups**
  - `SelectNodesInArea(Rect bounds)` method
  - `UnselectNodesInArea(Rect bounds)` method
  - Useful for group header click → select all children behavior

### Low Priority - Nice to Have

- [ ] **Group Content Padding Setting**
  - Per-group padding customization
  - `Node.GroupPadding` property (nullable, falls back to default)

---

## Pro Version (FlowGraph.Pro)

### High Priority - Advanced Routing

- [ ] **Circuit Connection Style Router**
  - Angle-controlled bezier breaks (like Nodify's `CircuitConnection`)
  - `CircuitRouter` with `Angle` property (0-90 degrees)
  - Creates professional "circuit board" aesthetic

- [ ] **Enhanced Orthogonal Router Options**
  - Configurable corner radius for rounded bends
  - Direction preference settings (horizontal-first vs vertical-first)
  - Minimum segment length constraints

### Medium Priority - Power User Features

- [ ] **Group Templates**
  - Save/load group configurations
  - Predefined group styles (Comment, Region, Subflow)
  - Custom group renderers per template

- [ ] **Smart Group Auto-Layout**
  - Auto-arrange children within group bounds
  - Layout algorithms: Grid, Horizontal, Vertical, Force-directed
  - Triggered on group resize or via context menu

- [ ] **Group Constraints**
  - Lock group size (prevent auto-resize)
  - Minimum/maximum size constraints
  - Aspect ratio lock

### Low Priority - Enterprise Features

- [ ] **Group Permissions**
  - Read-only groups (view but not edit)
  - Locked groups (cannot add/remove nodes)
  - Useful for collaborative editing scenarios

---

## Already Unique to FlowGraph (Preserve!)

These features are NOT in competitors - keep and enhance:

- ✅ **Collapse/Expand Groups** - Neither Nodify nor Blazor.Diagrams has this
- ✅ **DirectCanvasRenderer** - Major performance advantage
- ✅ **SmartBezierRouter** - Obstacle-avoiding curves is unique
- ✅ **FlowGraph.3D** - Completely unique in the ecosystem
- ✅ **Guided/Manual Edge Routing** - User waypoint support

---

## Reference Links

- Nodify-Avalonia GroupingNode: https://github.com/BAndysc/nodify-avalonia/blob/main/Nodify/Nodes/GroupingNode.cs
- Blazor.Diagrams GroupModel: https://github.com/Blazor-Diagrams/Blazor.Diagrams/blob/main/src/Blazor.Diagrams.Core/Models/GroupModel.cs
- Blazor.Diagrams OrthogonalRouter: https://github.com/Blazor-Diagrams/Blazor.Diagrams/blob/main/src/Blazor.Diagrams.Core/Routers/OrthogonalRouter.cs
