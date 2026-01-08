using System.Numerics;

namespace FlowGraph.ThreeD.Meshes;

/// <summary>
/// Interface for a 3D mesh that can be rendered.
/// </summary>
public interface IMesh
{
  /// <summary>
  /// Gets the vertex data (position + normal + uv interleaved).
  /// Layout: [X, Y, Z, NX, NY, NZ, U, V] per vertex.
  /// </summary>
  ReadOnlySpan<float> Vertices { get; }

  /// <summary>
  /// Gets the index data for indexed drawing.
  /// </summary>
  ReadOnlySpan<uint> Indices { get; }

  /// <summary>
  /// Gets the number of floats per vertex (stride / sizeof(float)).
  /// </summary>
  int VertexSize { get; }

  /// <summary>
  /// Gets the number of indices.
  /// </summary>
  int IndexCount { get; }
}
