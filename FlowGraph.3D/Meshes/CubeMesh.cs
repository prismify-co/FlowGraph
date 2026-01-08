using System.Numerics;

namespace FlowGraph.ThreeD.Meshes;

/// <summary>
/// Generates cube mesh data.
/// </summary>
public sealed class CubeMesh : IMesh
{
  private readonly float[] _vertices;
  private readonly uint[] _indices;

  /// <summary>
  /// Creates a cube mesh with the specified size.
  /// </summary>
  /// <param name="size">The size of the cube (default 1.0).</param>
  public CubeMesh(float size = 1.0f)
  {
    var h = size / 2f;

    // Each face has 4 vertices with position (3) + normal (3) + uv (2) = 8 floats
    // 6 faces * 4 vertices = 24 vertices
    _vertices = new float[]
    {
            // Front face (Z+)
            -h, -h,  h,   0,  0,  1,   0, 0,
             h, -h,  h,   0,  0,  1,   1, 0,
             h,  h,  h,   0,  0,  1,   1, 1,
            -h,  h,  h,   0,  0,  1,   0, 1,
            
            // Back face (Z-)
             h, -h, -h,   0,  0, -1,   0, 0,
            -h, -h, -h,   0,  0, -1,   1, 0,
            -h,  h, -h,   0,  0, -1,   1, 1,
             h,  h, -h,   0,  0, -1,   0, 1,
            
            // Top face (Y+)
            -h,  h,  h,   0,  1,  0,   0, 0,
             h,  h,  h,   0,  1,  0,   1, 0,
             h,  h, -h,   0,  1,  0,   1, 1,
            -h,  h, -h,   0,  1,  0,   0, 1,
            
            // Bottom face (Y-)
            -h, -h, -h,   0, -1,  0,   0, 0,
             h, -h, -h,   0, -1,  0,   1, 0,
             h, -h,  h,   0, -1,  0,   1, 1,
            -h, -h,  h,   0, -1,  0,   0, 1,
            
            // Right face (X+)
             h, -h,  h,   1,  0,  0,   0, 0,
             h, -h, -h,   1,  0,  0,   1, 0,
             h,  h, -h,   1,  0,  0,   1, 1,
             h,  h,  h,   1,  0,  0,   0, 1,
            
            // Left face (X-)
            -h, -h, -h,  -1,  0,  0,   0, 0,
            -h, -h,  h,  -1,  0,  0,   1, 0,
            -h,  h,  h,  -1,  0,  0,   1, 1,
            -h,  h, -h,  -1,  0,  0,   0, 1,
    };

    // 6 faces * 2 triangles * 3 indices = 36 indices
    _indices = new uint[]
    {
            // Front
            0, 1, 2, 2, 3, 0,
            // Back
            4, 5, 6, 6, 7, 4,
            // Top
            8, 9, 10, 10, 11, 8,
            // Bottom
            12, 13, 14, 14, 15, 12,
            // Right
            16, 17, 18, 18, 19, 16,
            // Left
            20, 21, 22, 22, 23, 20,
    };
  }

  /// <inheritdoc />
  public ReadOnlySpan<float> Vertices => _vertices;

  /// <inheritdoc />
  public ReadOnlySpan<uint> Indices => _indices;

  /// <inheritdoc />
  public int VertexSize => 8; // 3 pos + 3 normal + 2 uv

  /// <inheritdoc />
  public int IndexCount => _indices.Length;
}
