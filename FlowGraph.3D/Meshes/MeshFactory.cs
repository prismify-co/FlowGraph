using FlowGraph.ThreeD.Abstractions;

namespace FlowGraph.ThreeD.Meshes;

/// <summary>
/// Factory for creating mesh instances.
/// </summary>
public static class MeshFactory
{
  private static readonly CubeMesh SharedCube = new();
  private static readonly PyramidMesh SharedPyramid = new();

  /// <summary>
  /// Gets a mesh for the specified shape type.
  /// Returns shared instances for efficiency.
  /// </summary>
  /// <param name="shapeType">The type of shape.</param>
  /// <returns>The mesh instance.</returns>
  public static IMesh GetMesh(ShapeType shapeType) => shapeType switch
  {
    ShapeType.Cube => SharedCube,
    ShapeType.Pyramid => SharedPyramid,
    _ => SharedCube
  };
}
