# FlowGraph vNext Architecture: Canvas-First, Element-Driven Editor

> Date: 2026-01-10  
> Scope: **FlowGraph community (foundation)** with a migration path that keeps FlowGraph.Pro viable  
> Status: Proposal (intended for discussion)

## 1. Problem Statement

FlowGraph began as a graph editor (nodes + ports + edges) inspired by ReactFlow. During development it became clear that the “ReactFlow mental model” is broader than graphs: it is a **viewport + coordinate system** that hosts many diagram semantics (sequence diagrams, logic builders, swimlanes, annotations, freeform shapes, etc.).

Today FlowGraph has made an important pivot toward canvas-first modeling via `Graph.Elements` and `ICanvasElement`, plus first-class shape elements. However, several subsystems remain node/edge-centric:

- Rendering is still mostly “render nodes/edges” with shapes as a separate pass.
- Input, hit testing, selection, and commands are primarily node/edge oriented.
- Serialization only round-trips nodes/edges, not general elements.
- The type system currently forces non-node concepts to pretend they are nodes or backgrounds.

This proposal defines an **element-driven architecture** that makes the canvas the product and graphs a specialization.

## 2. Goals

### 2.1 Product goals

- Support multiple diagram styles on the same canvas:
  - node graphs (ReactFlow-like)
  - sequence diagrams (lifelines, messages)
  - swimlanes/regions
  - freeform annotations and shapes
  - “logic builder” style blocks
- Make new diagram primitives addable without editing core rendering/input logic.

### 2.2 Engineering goals

- Make **elements** the first-class unit across:
  - rendering
  - hit testing
  - selection
  - commands / undo
  - serialization
  - collaboration-ready change tracking
- Keep core (FlowGraph.Core) UI-agnostic.
- Keep Avalonia layer (FlowGraph.Avalonia) as a host that can be swapped.
- Provide migration path from current `Node`/`Edge` APIs.

## 3. Non-goals

- Not building a full diagramming DSL in core.
- Not forcing all renderers into a single monolithic renderer.
- Not solving real-time collaboration in this proposal (but avoid blocking it).

## 4. Current State (as of 2026-01-10)

- Core introduces `ICanvasElement`, `CanvasElement`, `ElementCollection`, `Rect`, and several `ShapeElement` implementations.
- `Node` and `Edge` implement `ICanvasElement` pragmatically for compatibility.
- Avalonia layer has shape rendering (`ShapeVisualManager` + `ShapeRendererRegistry`) plus the existing node/edge renderers.
- Input uses an explicit input state machine.

This is the correct direction but not yet consistent end-to-end.

## 5. Key Architectural Issues to Resolve

### 5.1 Contract mismatch in `ICanvasElement`

`ICanvasElement` attempts to represent everything as positioned and sized. In practice:

- Nodes and shapes are naturally positioned/sized.
- Edges are “connections” with derived geometry.
- Some future elements are better represented as anchors + layout rules (sequence messages, lifelines).

**Risk:** forcing all elements into `Position/Width/Height` causes no-op setters, fake values, and complicated invariants.

### 5.2 Multiple sources of truth

`Graph` currently duplicates state across `Elements` and legacy `Nodes/Edges` collections.

**Risk:** drift, inconsistent notifications, and long-term maintenance cost.

### 5.3 Node/edge-centric subsystems

Selection, commands, box selection, and hit testing primarily work for nodes/edges.

**Risk:** every new element type requires scattering special cases throughout input and command code.

### 5.4 Serialization only round-trips nodes/edges

Shapes and future elements are lost on save/load.

**Risk:** canvas-first cannot be a real abstraction until serialization is element-based.

### 5.5 Rendering pipeline not element-sorted

Shapes are rendered in a separate pass; nodes/edges render in a fixed order.

**Risk:** Z-order rules become inconsistent and hard to reason about as more element types appear.

### 5.6 Debug/diagnostics leaks into hot paths

Render-path file IO or heavy logging can break performance and platform portability.

## 6. Proposed Architecture (Canvas-First, Element-Driven)

### 6.1 Domain model (FlowGraph.Core)

#### 6.1.1 “Element” is the universal unit

Introduce an element contract focused on what the editor needs:

- identity and type discrimination
- visibility, selection, z-order
- bounds for hit testing and viewport operations

Proposed base interface:

```csharp
public interface IElement
{
    string Id { get; }
    string Kind { get; }   // stable discriminator for serialization + registries

    bool IsVisible { get; set; }
    bool IsSelected { get; set; }
    int ZIndex { get; set; }

    Rect GetBounds();
}
```

Then add capability interfaces, instead of forcing every element to share all properties:

```csharp
public interface IPositionedElement : IElement
{
    Point Position { get; set; }
}

public interface ISizedElement : IPositionedElement
{
    double? Width { get; set; }
    double? Height { get; set; }
}

public interface IConnectableElement : IElement
{
    // Endpoint model is intentionally flexible
    Endpoint Source { get; }
    Endpoint Target { get; }
}

public interface IDeletableElement : IElement
{
    bool IsDeletable { get; }
}
```

Notes:

- `Kind` should be stable and versioned (e.g. `"node"`, `"edge"`, `"shape.rectangle"`).
- “Capabilities” let elements participate in features without lying.

#### 6.1.2 ElementCollection is the single source of truth

`Graph` becomes a thin wrapper around an `ElementCollection`:

- `Elements` is canonical.
- Legacy `Nodes` and `Edges` become computed views and/or removed in a future version.

#### 6.1.3 Graph is a specialization (constraints + convenience)

Graph semantics are expressed via constraints and helper APIs:

- A `Node` remains a useful element type.
- An `Edge` is a connectable element type.
- Sequence diagrams and swimlanes are just different element kinds.

### 6.2 Serialization (FlowGraph.Core)

Move from:

- `GraphDto { nodes: [...], edges: [...] }`

to:

- `GraphDto { elements: [...] }`

With a discriminator and per-kind payload:

```json
{
  "version": 2,
  "elements": [
    { "id": "n1", "kind": "node", "data": { ... } },
    { "id": "e1", "kind": "edge", "data": { ... } },
    { "id": "s1", "kind": "shape.rectangle", "data": { ... } }
  ]
}
```

Key mechanism: **element serializer registry**:

- `IElementSerializer` handles one `kind`.
- Core ships serializers for built-in kinds.
- Pro/custom can register additional serializers.

This mirrors the renderer registry pattern and prevents the core DTO from growing endlessly.

### 6.3 Commands and Undo (FlowGraph.Core)

Shift commands from node/edge-specific to element-based:

- `MoveElementsCommand` (for `IPositionedElement`)
- `ResizeElementsCommand` (for `ISizedElement`)
- `RemoveElementsCommand` (for `IDeletableElement`)
- `SelectElementsCommand`

Keep wrappers (`MoveNodesCommand`, etc.) for a deprecation window.

### 6.4 Rendering (FlowGraph.Avalonia)

#### 6.4.1 Element render pipeline

Replace the current mixed pipeline with:

1. Render grid and background layers
2. Render `elements` sorted by `ZIndex`

Introduce `ElementVisualManager`:

- Maintains mapping: `elementId -> Control`
- Dispatches to the appropriate renderer based on `Kind`

Renderer registry:

```csharp
public interface IElementRenderer
{
    Control CreateVisual(IElement element, ElementRenderContext ctx);
    void UpdateVisual(Control visual, IElement element, ElementRenderContext ctx);
    void UpdateSelection(Control visual, IElement element, ElementRenderContext ctx);
}

public interface IElementRendererRegistry
{
    IElementRenderer Get(string kind);
}
```

Nodes/edges/shapes can keep their specialized renderers internally, but they should be invoked through the element layer.

#### 6.4.2 Hit testing must return elements

All visuals should set `Tag = element` (or a uniform hit-test payload).

- Visual-tree rendering: click returns the tagged element.
- Direct rendering mode: hit testing returns an `IElement` hit result.

### 6.5 Input and Interaction (FlowGraph.Avalonia)

Keep the state machine, but change the decision points:

- The input system should interpret user actions as operations on elements:
  - select
  - move
  - connect
  - resize
  - edit label
- State transitions should be based on capability interfaces:
  - if hit is `IConnectableElement` → connection logic
  - if hit is `ISizedElement` and handle hit → resize
  - otherwise default selection behavior

Box selection should operate on `Elements.FindInBounds(...)` rather than only nodes.

### 6.6 Validation and Constraints

Graph-specific constraints (valid connections, port type rules, etc.) should not be hardcoded into `Graph.AddEdge`.

Instead:

- Core provides a validation service contract (`IConnectionValidator`, plus optional `IConstraint` pipeline).
- UI layer calls validation on attempted connect operations.
- Batch loading can bypass validation intentionally.

### 6.7 Layers and Z-order

Codify default z-order semantics in one place:

- backgrounds: 0
- regions/shapes: 100
- connections (edges/messages): 200
- nodes: 300
- overlays/handles: 1000+

Avoid rendering in hardcoded phases that conflict with ZIndex.

## 7. Breaking Changes (Proposed)

This section intentionally lists breaking changes so the project can converge on a clean abstraction before v1.

### 7.1 API changes

- Replace or evolve `ICanvasElement` into `IElement` + capability interfaces.
- `Graph.Nodes` and `Graph.Edges` become read-only computed views (or removed).
- Introduce `Graph.Elements` as the single canonical collection.

### 7.2 Serialization format

- Replace `nodes/edges` DTO with `elements` DTO.
- Add versioning and element kind discriminators.

### 7.3 Commands

- Prefer element-based commands. Keep node/edge commands as deprecated wrappers.

### 7.4 Rendering

- FlowCanvas renders via `ElementVisualManager` and registry rather than fixed node/edge passes.

## 8. Migration Plan (Incremental)

Suggested order to reduce churn:

1. **Remove debug file IO in render loops**; keep logging behind flags.
2. Add shape hit testing/selection by tagging visuals with the shape element.
3. Add element-based serialization (versioned). Maintain legacy deserialize support temporarily.
4. Introduce element-based commands (move/resize/remove) and update selection manager.
5. Add `ElementVisualManager` and unify render pipeline by ZIndex.
6. Convert direct-rendering hit testing to return `IElement`.
7. Deprecate legacy graph-centric APIs.

## 9. Open Questions (for LLM debate)

1. Should `Node` inherit from `CanvasElement` (class hierarchy) or implement capability interfaces (composition)?
2. Should an “edge/message” be modeled as:
   - an element with endpoints (connectable)
   - a route produced by a routing engine
   - or a view-only artifact derived from other elements?
3. Where should selection state live?
   - on element runtime state
   - or in a separate `SelectionModel` owned by the canvas/editor?
4. How should z-order be handled?
   - per element `ZIndex`
   - plus optional layers
   - or layers only?
5. How should element-kind versioning work for long-term compatibility?

## 10. Decision Summary

- **Canvas-first is correct**: the viewport and coordinate system are foundational.
- **Elements must be first-class** across rendering, input, commands, and serialization.
- Use **registries** for both rendering and serialization to keep core extensible.
- Prefer **capability interfaces** over a single “everything is positioned/sized” interface.

---

## 11. Peer Review (Claude Opus 4.5 — Senior Engineering Perspective)

> This section provides a code-grounded critique of the above proposal after re-analyzing the actual FlowGraph codebase.

### 11.1 What the Proposal Gets Right

**Diagnosis is accurate.** The identified issues are real:

- `Edge` implements `ICanvasElement.Position` with `get => Point.Zero; set { }` — a no-op lie.
- `Graph` explicitly syncs `Elements` ↔ `Nodes`/`Edges` in every add/remove method.
- `GraphSerializer` only serializes `GraphDto { Nodes, Edges }` — shapes are lost on round-trip.
- All commands are node/edge-specific (`MoveNodesCommand`, `AddEdgeCommand`, etc.).

**Capability interfaces are the right abstraction.** The current `ICanvasElement` forces everything into `Position/Width/Height`. The proposed split (`IElement` + `IPositionedElement` + `ISizedElement` + `IConnectableElement`) eliminates the semantic lie that edges "have a position."

**ZIndex constants already exist.** `CanvasElement` defines `ZIndexBackground = 0`, `ZIndexShapes = 100`, `ZIndexEdges = 200`, `ZIndexNodes = 300`. The groundwork is laid.

### 11.2 What the Proposal Oversimplifies

**1. Renderer registries already exist — this is refactoring, not greenfield.**

The codebase already has:

- `INodeRenderer` + `NodeRendererRegistry`
- `IEdgeRenderer` + `EdgeRendererRegistry`
- `IShapeRenderer` + `ShapeRendererRegistry`

The proposal implies creating `IElementRenderer` from scratch. Reality: unify existing registries under a common interface.

**2. `CanvasElement` already does most of what `IElement` proposes.**

`CanvasElement` already provides:

- Settable `IsVisible`, `IsSelected`, `ZIndex` with `PropertyChanged`
- Virtual `GetBounds()` method
- `OnBoundsChanged()` hook

The problem isn't that `CanvasElement` doesn't exist — it's that `Node` and `Edge` don't inherit from it. They implement `ICanvasElement` independently with incompatible behavior.

**3. The `Kind` discriminator conflicts with existing `Type` property.**

`ICanvasElement.Type` already exists and is used by renderer registries. The proposal adds `Kind` without addressing how it differs from `Type` or proposing a rename.

**4. Definition+State pattern is ignored.**

`Node` and `Edge` use a **Definition+State composition pattern** (immutable `NodeDefinition` + mutable `NodeState`). Any element-first refactor must decide:

- Do shapes also get Definition+State?
- How does this affect command structure and collaboration?

**5. Input system complexity is underestimated.**

The current system uses different code paths for direct rendering vs visual tree, tags controls inconsistently (`Tag = Node`, `Tag = Edge`, `Tag = (Node, Port, bool)`), and has type-specific handlers. "Changing decision points" is a larger undertaking than implied.

**6. Debug file I/O is a blocking bug, not a migration step.**

`FlowCanvas.Rendering.cs` line 193 contains:

```csharp
System.IO.File.AppendAllText(@"C:\temp\flowgraph_debug.log", ...);
```

This is inside `RenderElements()` — a hot path. This should be removed immediately, not scheduled in a migration plan.

### 11.3 What the Proposal Gets Wrong

**1. "Graph becomes a thin wrapper" is backwards.**

`Graph` already exposes `Elements` as the primary collection with `Nodes`/`Edges` marked `[Obsolete]`. The issue isn't making `Graph` thinner — it's that legacy code still uses `Nodes`/`Edges` and `AddEdge()` validates against the legacy collection.

**2. "Shapes render in a separate pass" is misleading.**

Shapes are rendered with ZIndex sorting within their pass. The real problem is `RenderElements()` only renders nodes/edges despite its generic name, and shapes aren't interleaved in final Z-order. The fix is unifying the render loop, not "adding" ZIndex support.

**3. Serialization registry pattern is overkill.**

`System.Text.Json` already supports polymorphic serialization with `$type` discriminators. A separate serializer registry adds complexity without clear benefit.

### 11.4 Recommended Approach (Incremental)

1. **Immediate: Remove debug file I/O** — this is a correctness bug.

2. **Phase 1: Make `Node`/`Edge` inherit from `CanvasElement`**

   - Unifies runtime behavior
   - `Edge` overrides `GetBounds()` to compute from endpoints
   - Remove explicit interface implementations that lie

3. **Phase 2: Rename `Type` → `Kind` (or vice versa)** — pick one concept.

4. **Phase 3: Extend existing renderer contracts**

   - Add `IElementRenderer<T>` base that existing renderers implement
   - Unify visual managers into one element-ordered loop

5. **Phase 4: Extend serialization incrementally**

   - Add `shapes` array to `GraphDto` first
   - Full polymorphic element serialization later

6. **Phase 5: Generalize commands**

   - `MoveElementsCommand` that works on `IPositionedElement`
   - Keep node/edge commands as convenience wrappers

7. **Document Definition+State decision** — extend pattern to shapes or explain why not.

### 11.5 Bottom Line

The proposal is **directionally correct** but written as if designing from scratch rather than refactoring. The existing code is ~60% of the way there:

| Component                     | Status     |
| ----------------------------- | ---------- |
| `CanvasElement` base class    | ✅ Exists  |
| `ElementCollection`           | ✅ Exists  |
| Shape elements                | ✅ Exist   |
| Shape renderers               | ✅ Exist   |
| ZIndex constants              | ✅ Exist   |
| Node/Edge using CanvasElement | ❌ Not yet |
| Unified render loop           | ❌ Not yet |
| Element-based serialization   | ❌ Not yet |
| Element-based commands        | ❌ Not yet |

The real work is **integration and unification**, not building new abstractions.

---

If you want, create a companion document describing a concrete element set for sequence diagrams (lifeline, message, activation bar, note) using the element+capabilities approach, plus suggested default z-index and routing behaviors.

## 12. Adjustments After Peer Review (Reconciled Recommendation)

This section captures the changes I would make after considering the peer review and re-checking the community codebase.

### 12.1 Prefer capability-based composition for `Node`/`Edge` (do not assume inheritance)

The peer review suggests making `Node`/`Edge` inherit from `CanvasElement` as a fast unification step. That can work, but it collides with the existing **Definition + State** composition pattern:

- `CanvasElement` currently stores its own backing fields (`Position`, `Width`, `Height`, `IsSelected`, `IsVisible`, `ZIndex`).
- `Node`/`Edge` already store runtime state in `NodeState` / `EdgeState`.

If `Node : CanvasElement` naively, you end up with _two competing sources of runtime truth_ unless `CanvasElement` is refactored to delegate storage.

Adjusted recommendation:

- Keep shapes as `CanvasElement`-derived (they already benefit from the backing fields).
- For `Node`/`Edge`, prefer implementing capability interfaces (composition) and extend `NodeState`/`EdgeState` as needed (e.g., add `IsVisible`, `ZIndex`) so runtime state remains centralized.
- If you still want inheritance, make it an explicit design: refactor `CanvasElement` into a “storage-agnostic base” (or introduce a new base) so nodes/edges don’t duplicate state.

### 12.2 Clarify `Type` vs `Kind` and avoid introducing two parallel identifiers

Today, `ICanvasElement.Type` already exists and is used for renderer selection. The proposal introduced `Kind` for stable discrimination.

Adjusted recommendation:

- Use **one** canonical string discriminator in the public model.
- Either:
  - Rename `Type` → `Kind` (breaking, but clearer), or
  - Keep `Type` and treat it as the discriminator for both rendering and serialization.

If both concepts are needed (rare), document the difference explicitly (e.g., `Kind` = serialized semantic kind; `Type` = renderer style variant). But default should be one.

### 12.3 Serialization: keep a stable `kind` discriminator, but implement it pragmatically

The peer review calls the serializer registry “overkill” because `System.Text.Json` supports polymorphism. That is true for closed-world type sets.

However FlowGraph’s explicit goal is _open-world extensibility_ (custom element kinds added by consumers). In that world:

- Polymorphism requires known CLR types at deserialize-time.
- Type-name-based `$type` formats tend to be brittle across refactors/renames and are not a great long-term interchange format.

Adjusted recommendation:

- Keep a versioned, string-discriminated format (`version`, `elements[]`, `kind`).
- Implement incrementally:
  1. Short-term: extend the existing DTO to serialize shapes (e.g., add `Shapes` list) to stop data loss.
  2. Mid-term: move to `elements[]` with `kind`.
  3. Only introduce a serializer registry when you actually need third-party kinds at runtime.

This preserves the format stability benefits without front-loading infrastructure.

### 12.4 Rendering unification: build an orchestrator over existing registries/managers

The proposal reads like a greenfield `IElementRenderer` system. The codebase already has node/edge/shape renderer interfaces and registries.

Adjusted recommendation:

- Introduce an _orchestrator_ (call it `ElementVisualManager` if you like) that:
  - iterates `Graph.Elements.ByZIndex`
  - routes each element to the appropriate existing subsystem (node renderer, edge renderer, shape renderer)
  - standardizes lifecycle (create/update/remove) and tagging for hit testing

This yields “element-first rendering” without rewriting every renderer contract immediately.

### 12.5 Input/hit-testing: standardize on a single hit payload

Rather than tagging visuals with multiple unrelated payloads (`Node`, `Edge`, `(Node, Port, bool)`), standardize on a single hit result type, e.g.:

- `ElementHitInfo { ICanvasElement Element; string? Handle; string? PortId; }`

Adjusted recommendation:

- Visual-tree mode: `Control.Tag = ElementHitInfo`
- Direct-render mode: return `ElementHitInfo`

This removes a lot of branching in pointer handlers and makes adding new element kinds feasible.

### 12.6 Debug file I/O: treat as immediate defect

Strongly agree with the peer review: any file writes in `RenderElements()` are a blocking performance and portability bug. This should be removed immediately (or guarded behind a compile-time debug flag and off by default).

---

## 13. Cross-Review Analysis (Claude Sonnet 4.5)

After re-examining the codebase with GPT's adjustments in mind, here's where I agree, disagree, or would refine further.

### 13.1 Strong Agreement: Definition+State collision with inheritance (12.1)

**Agree completely.** Looking at the actual code:

- `NodeState` has: `X`, `Y`, `Width`, `Height`, `IsSelected`, `IsDragging`, `IsCollapsed`
- `CanvasElement` stores backing fields: `_position`, `_width`, `_height`, `_isSelected`, `_isVisible`, `_zIndex`

If `Node : CanvasElement`, you'd have:

- `Node.Position` (from `CanvasElement`) vs `Node.State.X/Y` (from `NodeState`)
- `Node.IsSelected` (from `CanvasElement`) vs `Node.State.IsSelected` (from `NodeState`)

This is a **runtime state duplication disaster**. GPT is right: you need to pick one storage location.

**Refined recommendation:**

1. Make `NodeState`/`EdgeState` implement the missing `ICanvasElement` properties (`IsVisible`, `ZIndex`)
2. Keep `Node`/`Edge` as interface implementers that delegate to `State`
3. For shapes, continuing to use `CanvasElement` directly is fine since they don't need Definition+State complexity

### 13.2 Partial Agreement: Type vs Kind naming (12.2)

**Agree on the problem, but would go further.** The current `ICanvasElement.Type` is semantically overloaded:

- For `Node`: returns `Definition.Type` (business semantic like "process", "decision")
- For shapes: returns shape type ("line", "rectangle", "text")
- For edges: returns `EdgeType.ToString().ToLowerInvariant()`

This inconsistency is already problematic. GPT suggests "pick one canonical discriminator" but I'd argue you need _both_:

- `Kind` = category for polymorphic handling ("node", "edge", "shape.rectangle")
- `Type` = user-facing semantic type within that category ("process", "decision", "start")

**Proposed:**

```csharp
public interface ICanvasElement {
    string Kind { get; }        // "node", "edge", "shape.line" - stable discriminator
    string? Type { get; }       // user semantic - "process", "bezier", etc. (optional)
}
```

This is clearer than overloading one property for both purposes.

### 13.3 Strong Agreement: Incremental serialization (12.3)

**Agree completely.** GPT correctly identifies that:

- Open-world extensibility benefits from stable discriminators
- Polymorphic `$type` is brittle for interchange formats
- But you don't need a registry until you actually have third-party element types

The phased approach (add `Shapes` first, migrate to `elements[]` second, registry third) is pragmatic and correct.

### 13.4 Strong Agreement: Rendering orchestrator (12.4)

**Agree.** The existing renderer infrastructure is:

- `INodeRenderer` + `NodeRendererRegistry`
- `IEdgeRenderer` + `EdgeRendererRegistry`
- `IShapeRenderer` + `ShapeRendererRegistry`

Creating an orchestrator that dispatches to these based on element kind is the right incremental path. You get "element-first rendering" without rewriting renderer contracts.

**Implementation note:** The orchestrator needs type-switching logic:

```csharp
foreach (var element in graph.Elements.ByZIndex) {
    if (element is Node n) nodeRenderer.Render(n);
    else if (element is Edge e) edgeRenderer.Render(e);
    else if (element is ShapeElement s) shapeRenderer.Render(s);
}
```

This is fine for now. Later, if you add `IElement.Kind`, you can make it registry-based.

### 13.5 Strong Agreement: ElementHitInfo unification (12.5)

**Agree completely.** The current hit-test tagging is a mess:

- `Control.Tag = Node`
- `Control.Tag = Edge`
- `Control.Tag = (Node, Port, bool)` for ports

A single `ElementHitInfo { ICanvasElement Element; string? Handle; string? PortId; }` standardizes this and makes the input state machine cleaner.

### 13.6 Strong Agreement: Debug file I/O is a bug (12.6)

**Agree.** This should be fixed immediately, not in a migration plan. It's a blocking I/O call in a render loop.

### 13.7 One Disagreement: "Serializer registry is overkill" claim

GPT pushes back on Opus's claim that a serializer registry is overkill, citing open-world extensibility needs. I partially disagree with GPT's framing.

**The nuance:**

- For FlowGraph _community edition_ (this repo), you control all element types. JSON polymorphism with `JsonDerivedType` is fine.
- For FlowGraph _Pro_ or third-party extensions that add custom elements, a registry becomes necessary.

**Better approach:** Use `JsonDerivedType` for known types now, but design the DTO format to be registry-compatible later:

```csharp
// Short term (community)
[JsonDerivedType(typeof(NodeDto), "node")]
[JsonDerivedType(typeof(EdgeDto), "edge")]
[JsonDerivedType(typeof(ShapeDto), "shape")]
public abstract class ElementDto { }

// Long term (extensibility)
interface IElementSerializer {
    string Kind { get; }
    ElementDto Serialize(ICanvasElement element);
    ICanvasElement Deserialize(ElementDto dto);
}
```

This gives you simple built-in serialization now with an extension path later.

### 13.8 Bottom Line

GPT's adjustments are **solid and code-grounded**. The Definition+State insight is critical and would have been an expensive refactor if missed. The incremental phasing is realistic.

**Priority order I'd recommend:**

1. Remove debug file I/O (immediate bug)
2. Add `IsVisible`/`ZIndex` to `NodeState`/`EdgeState` interfaces and implementations
3. Implement `ElementHitInfo` for unified hit testing
4. Add shapes to serialization (stop data loss)
5. Build rendering orchestrator over existing registries
6. Migrate to `elements[]` DTO format
7. Add element-based commands
