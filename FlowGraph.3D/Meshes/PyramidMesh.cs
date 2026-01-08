using System.Numerics;

namespace FlowGraph.ThreeD.Meshes;

/// <summary>
/// Generates pyramid mesh data (4-sided pyramid with square base).
/// </summary>
public sealed class PyramidMesh : IMesh
{
  private readonly float[] _vertices;
  private readonly uint[] _indices;

  /// <summary>
  /// Creates a pyramid mesh with the specified size.
  /// </summary>
  /// <param name="size">The base size of the pyramid (default 1.0).</param>
  public PyramidMesh(float size = 1.0f)
  {
    var h = size / 2f;
    var height = size;

    // Apex point
    var apex = new Vector3(0, height / 2f, 0);

    // Base corners
    var bl = new Vector3(-h, -height / 2f, -h);
    var br = new Vector3(h, -height / 2f, -h);
    var fr = new Vector3(h, -height / 2f, h);
    var fl = new Vector3(-h, -height / 2f, h);

    // Calculate face normals
    var frontNormal = CalculateNormal(fl, fr, apex);
    var rightNormal = CalculateNormal(fr, br, apex);
    var backNormal = CalculateNormal(br, bl, apex);
    var leftNormal = CalculateNormal(bl, fl, apex);
    var bottomNormal = new Vector3(0, -1, 0);

    // Each triangular face has 3 vertices, base has 4 vertices
    // 4 side faces * 3 vertices + 1 base * 4 vertices = 16 vertices
    _vertices = new float[]
    {
            // Front face
            fl.X, fl.Y, fl.Z,   frontNormal.X, frontNormal.Y, frontNormal.Z,   0, 0,
            fr.X, fr.Y, fr.Z,   frontNormal.X, frontNormal.Y, frontNormal.Z,   1, 0,
            apex.X, apex.Y, apex.Z, frontNormal.X, frontNormal.Y, frontNormal.Z,   0.5f, 1,
            
            // Right face
            fr.X, fr.Y, fr.Z,   rightNormal.X, rightNormal.Y, rightNormal.Z,   0, 0,
            br.X, br.Y, br.Z,   rightNormal.X, rightNormal.Y, rightNormal.Z,   1, 0,
            apex.X, apex.Y, apex.Z, rightNormal.X, rightNormal.Y, rightNormal.Z,   0.5f, 1,
            
            // Back face
            br.X, br.Y, br.Z,   backNormal.X, backNormal.Y, backNormal.Z,   0, 0,
            bl.X, bl.Y, bl.Z,   backNormal.X, backNormal.Y, backNormal.Z,   1, 0,
            apex.X, apex.Y, apex.Z, backNormal.X, backNormal.Y, backNormal.Z,   0.5f, 1,
            
            // Left face
            bl.X, bl.Y, bl.Z,   leftNormal.X, leftNormal.Y, leftNormal.Z,   0, 0,
            fl.X, fl.Y, fl.Z,   leftNormal.X, leftNormal.Y, leftNormal.Z,   1, 0,
            apex.X, apex.Y, apex.Z, leftNormal.X, leftNormal.Y, leftNormal.Z,   0.5f, 1,
            
            // Bottom face (square)
            bl.X, bl.Y, bl.Z,   bottomNormal.X, bottomNormal.Y, bottomNormal.Z,   0, 0,
            br.X, br.Y, br.Z,   bottomNormal.X, bottomNormal.Y, bottomNormal.Z,   1, 0,
            fr.X, fr.Y, fr.Z,   bottomNormal.X, bottomNormal.Y, bottomNormal.Z,   1, 1,
            fl.X, fl.Y, fl.Z,   bottomNormal.X, bottomNormal.Y, bottomNormal.Z,   0, 1,
    };

    // 4 triangular faces * 3 indices + 1 square base * 6 indices = 18 indices
    _indices = new uint[]
    {
            // Front
            0, 1, 2,
            // Right
            3, 4, 5,
            // Back
            6, 7, 8,
            // Left
            9, 10, 11,
            // Bottom (2 triangles)
            12, 13, 14, 14, 15, 12,
    };
  }

  private static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
  {
    var edge1 = b - a;
    var edge2 = c - a;
    return Vector3.Normalize(Vector3.Cross(edge1, edge2));
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
