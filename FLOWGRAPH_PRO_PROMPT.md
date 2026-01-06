# FlowGraph Pro Generation Prompt

Use this prompt with GitHub Copilot to generate the FlowGraph Pro project.

---

Create a FlowGraph.Pro solution that extends the open-source FlowGraph library (FlowGraph.Core and FlowGraph.Avalonia) with premium features. This is a commercial add-on that references the community packages via NuGet.

## Solution Structure

Create the following projects:

```
FlowGraph.Pro/
??? FlowGraph.Pro.sln
??? FlowGraph.Pro/
?   ??? FlowGraph.Pro.csproj          # Core pro features (no UI)
??? FlowGraph.Pro.Avalonia/
?   ??? FlowGraph.Pro.Avalonia.csproj # Avalonia UI pro features
??? FlowGraph.Pro.Tests/
?   ??? FlowGraph.Pro.Tests.csproj    # Unit tests
??? FlowGraph.Pro.Demo/
    ??? FlowGraph.Pro.Demo.csproj     # Demo application
```

## Package References

FlowGraph.Pro.csproj:
- Reference FlowGraph.Core from NuGet (version 0.1.0)

FlowGraph.Pro.Avalonia.csproj:
- Reference FlowGraph.Avalonia from NuGet (version 0.1.0)
- Reference FlowGraph.Pro project

## License System

Create a simple license validation system:

```csharp
namespace FlowGraph.Pro;

public static class LicenseManager
{
    private static string? _licenseKey;
    private static bool _isValid;
    
    public static void SetLicenseKey(string key);
    public static bool IsLicensed { get; }
    public static void ValidateOrThrow(); // Throws LicenseRequiredException if not licensed
}

public class LicenseRequiredException : Exception
{
    public LicenseRequiredException(string feature);
}
```

## Pro Features to Implement

### 1. Auto-Layout Package (FlowGraph.Pro/Layout/)

Implement automatic graph layout algorithms:

```csharp
public interface ILayoutAlgorithm
{
    Dictionary<string, Point> Calculate(Graph graph, LayoutOptions options);
}

public class DagreLayoutAlgorithm : ILayoutAlgorithm
{
    // Hierarchical/directed graph layout (like Dagre.js)
    // - Supports rank direction: TB, BT, LR, RL
    // - Node separation, rank separation options
    // - Handles edges with proper routing
}

public class TreeLayoutAlgorithm : ILayoutAlgorithm
{
    // Tree layout for hierarchical data
    // - Horizontal or vertical orientation
    // - Compact or spread modes
}

public class ForceDirectedLayoutAlgorithm : ILayoutAlgorithm
{
    // Physics-based layout
    // - Configurable spring force, repulsion
    // - Iterative settling with callback
}

public class GridLayoutAlgorithm : ILayoutAlgorithm
{
    // Simple grid arrangement
    // - Configurable columns, spacing
}
```

Extension methods for FlowCanvas:

```csharp
public static class FlowCanvasLayoutExtensions
{
    public static void AutoLayout(this FlowCanvas canvas, ILayoutAlgorithm algorithm);
    public static Task AutoLayoutAnimated(this FlowCanvas canvas, ILayoutAlgorithm algorithm, double duration = 0.5);
}
```

### 2. Export Package (FlowGraph.Pro.Avalonia/Export/)

```csharp
public static class FlowCanvasExportExtensions
{
    // PNG export
    public static Task ExportToPngAsync(this FlowCanvas canvas, string filePath, ExportOptions? options = null);
    public static Task<byte[]> ExportToPngBytesAsync(this FlowCanvas canvas, ExportOptions? options = null);
    
    // SVG export
    public static Task ExportToSvgAsync(this FlowCanvas canvas, string filePath, SvgExportOptions? options = null);
    public static Task<string> ExportToSvgStringAsync(this FlowCanvas canvas, SvgExportOptions? options = null);
    
    // PDF export (optional, may require additional dependency)
    public static Task ExportToPdfAsync(this FlowCanvas canvas, string filePath, PdfExportOptions? options = null);
}

public class ExportOptions
{
    public double Scale { get; set; } = 2.0;           // Resolution multiplier
    public bool IncludeBackground { get; set; } = true;
    public Thickness Padding { get; set; } = new(50);
    public bool TransparentBackground { get; set; } = false;
    public Rect? Region { get; set; } = null;          // null = entire graph
}

public class SvgExportOptions : ExportOptions
{
    public bool EmbedFonts { get; set; } = true;
    public bool Minify { get; set; } = false;
}
```

### 3. Subflows (FlowGraph.Pro/Subflows/)

Enable nested graphs within nodes:

```csharp
public class SubflowNode : Node
{
    public Graph SubGraph { get; set; }
    
    // Interface ports that map to internal nodes
    public List<SubflowPortMapping> InputMappings { get; set; }
    public List<SubflowPortMapping> OutputMappings { get; set; }
}

public class SubflowPortMapping
{
    public string ExternalPortId { get; set; }    // Port on the subflow node
    public string InternalNodeId { get; set; }    // Node inside the subgraph
    public string InternalPortId { get; set; }    // Port on the internal node
}

public class SubflowManager
{
    public SubflowNode CreateSubflow(Graph parentGraph, IEnumerable<Node> nodesToEncapsulate);
    public void ExpandSubflow(Graph parentGraph, SubflowNode subflow);
    public void OpenSubflowEditor(SubflowNode subflow); // Event for UI to handle
}
```

### 4. Advanced Performance (FlowGraph.Pro.Avalonia/Performance/)

Optimizations for 5,000+ nodes:

```csharp
public class VirtualizingFlowCanvas : FlowCanvas
{
    // Only renders nodes in viewport + buffer zone
    // Uses spatial indexing (R-tree) for fast queries
    // Level-of-detail rendering (simplified nodes when zoomed out)
}

public class PerformanceOptions
{
    public bool EnableVirtualization { get; set; } = true;
    public int VirtualizationBuffer { get; set; } = 200;  // pixels
    public double SimplifiedRenderingZoomThreshold { get; set; } = 0.3;
    public bool EnableEdgeBundling { get; set; } = false; // Group parallel edges
}
```

### 5. Advanced Routing (FlowGraph.Pro/Routing/)

```csharp
public class SmartEdgeRouter : IEdgeRouter
{
    // A* pathfinding that avoids node intersections
    // Automatic waypoint generation
    // Edge bundling for cleaner visuals
}

public class EdgeBundler
{
    // Groups edges with similar paths
    public void BundleEdges(Graph graph, BundlingOptions options);
}
```

### 6. History/Versioning (FlowGraph.Pro/Versioning/)

```csharp
public class GraphVersionManager
{
    public void CreateSnapshot(string label = null);
    public IReadOnlyList<GraphSnapshot> Snapshots { get; }
    public void RestoreSnapshot(GraphSnapshot snapshot);
    public Graph GetDiff(GraphSnapshot a, GraphSnapshot b);
}
```

## Project Configuration

FlowGraph.Pro.csproj:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>0.1.0</Version>
    <PackageId>FlowGraph.Pro</PackageId>
    <Title>FlowGraph Pro</Title>
    <Description>Professional features for FlowGraph: auto-layout, export, subflows, and more.</Description>
    <Authors>Prismify LLC</Authors>
    <Company>Prismify LLC</Company>
    <Copyright>Copyright 2026 Prismify LLC. All rights reserved.</Copyright>
    <PackageLicenseExpression></PackageLicenseExpression>
    <PackageLicenseFile>LICENSE.md</PackageLicenseFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlowGraph.Core" Version="0.1.0" />
  </ItemGroup>
</Project>
```

FlowGraph.Pro.Avalonia.csproj:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>0.1.0</Version>
    <PackageId>FlowGraph.Pro.Avalonia</PackageId>
    <Title>FlowGraph Pro for Avalonia</Title>
    <Description>Professional Avalonia UI features for FlowGraph: export to PNG/SVG, advanced rendering, and more.</Description>
    <Authors>Prismify LLC</Authors>
    <Company>Prismify LLC</Company>
    <Copyright>Copyright 2026 Prismify LLC. All rights reserved.</Copyright>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlowGraph.Avalonia" Version="0.1.0" />
    <PackageReference Include="Avalonia" Version="11.2.2" />
    <PackageReference Include="SkiaSharp" Version="2.88.7" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\FlowGraph.Pro\FlowGraph.Pro.csproj" />
  </ItemGroup>
</Project>
```

## Usage Example

```csharp
using FlowGraph.Pro;
using FlowGraph.Pro.Layout;
using FlowGraph.Pro.Avalonia.Export;

// Set license key (required for pro features)
LicenseManager.SetLicenseKey("XXXX-XXXX-XXXX-XXXX");

// Auto-layout
var dagre = new DagreLayoutAlgorithm { Direction = LayoutDirection.LeftToRight };
await canvas.AutoLayoutAnimated(dagre, duration: 0.5);

// Export
await canvas.ExportToPngAsync("graph.png", new ExportOptions { Scale = 2.0 });
await canvas.ExportToSvgAsync("graph.svg");
```

## Implementation Priority

1. License system (gate all features)
2. Export to PNG/SVG (high customer value)
3. Dagre auto-layout (most requested)
4. Tree and force-directed layouts
5. Subflows
6. Performance optimizations

Start with the solution structure, license system, and PNG export feature.
