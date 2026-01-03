using FlowGraph.Core;

namespace FlowGraph.Avalonia.Validation;

/// <summary>
/// Provides validation logic for connections between ports.
/// </summary>
public interface IConnectionValidator
{
    /// <summary>
    /// Validates whether a connection can be made between two ports.
    /// </summary>
    /// <param name="context">The connection context containing source and target information.</param>
    /// <returns>A validation result indicating whether the connection is allowed.</returns>
    ConnectionValidationResult Validate(ConnectionContext context);
}

/// <summary>
/// Context information for validating a connection.
/// </summary>
public class ConnectionContext
{
    /// <summary>
    /// The source node (output side).
    /// </summary>
    public required Node SourceNode { get; init; }

    /// <summary>
    /// The source port (output port).
    /// </summary>
    public required Port SourcePort { get; init; }

    /// <summary>
    /// The target node (input side).
    /// </summary>
    public required Node TargetNode { get; init; }

    /// <summary>
    /// The target port (input port).
    /// </summary>
    public required Port TargetPort { get; init; }

    /// <summary>
    /// The graph containing the nodes.
    /// </summary>
    public required Graph Graph { get; init; }
}

/// <summary>
/// Result of a connection validation.
/// </summary>
public class ConnectionValidationResult
{
    /// <summary>
    /// Whether the connection is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Optional message explaining why the connection is invalid.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ConnectionValidationResult Valid() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with an optional message.
    /// </summary>
    public static ConnectionValidationResult Invalid(string? message = null) => 
        new() { IsValid = false, Message = message };
}

/// <summary>
/// Default connection validator that allows all connections.
/// </summary>
public class DefaultConnectionValidator : IConnectionValidator
{
    public ConnectionValidationResult Validate(ConnectionContext context)
    {
        return ConnectionValidationResult.Valid();
    }
}

/// <summary>
/// Connection validator that checks port type compatibility.
/// </summary>
public class TypeMatchingConnectionValidator : IConnectionValidator
{
    public ConnectionValidationResult Validate(ConnectionContext context)
    {
        // Allow connection if port types match or if either is "any"
        var sourceType = context.SourcePort.Type?.ToLowerInvariant() ?? "any";
        var targetType = context.TargetPort.Type?.ToLowerInvariant() ?? "any";

        if (sourceType == "any" || targetType == "any" || sourceType == targetType)
        {
            return ConnectionValidationResult.Valid();
        }

        return ConnectionValidationResult.Invalid(
            $"Type mismatch: cannot connect '{sourceType}' to '{targetType}'");
    }
}

/// <summary>
/// Connection validator that prevents duplicate connections.
/// </summary>
public class NoDuplicateConnectionValidator : IConnectionValidator
{
    public ConnectionValidationResult Validate(ConnectionContext context)
    {
        var exists = context.Graph.Edges.Any(e =>
            e.Source == context.SourceNode.Id &&
            e.Target == context.TargetNode.Id &&
            e.SourcePort == context.SourcePort.Id &&
            e.TargetPort == context.TargetPort.Id);

        if (exists)
        {
            return ConnectionValidationResult.Invalid("Connection already exists");
        }

        return ConnectionValidationResult.Valid();
    }
}

/// <summary>
/// Connection validator that prevents self-connections (node connecting to itself).
/// </summary>
public class NoSelfConnectionValidator : IConnectionValidator
{
    public ConnectionValidationResult Validate(ConnectionContext context)
    {
        if (context.SourceNode.Id == context.TargetNode.Id)
        {
            return ConnectionValidationResult.Invalid("Cannot connect a node to itself");
        }

        return ConnectionValidationResult.Valid();
    }
}

/// <summary>
/// Connection validator that prevents cycles in the graph.
/// </summary>
public class NoCycleConnectionValidator : IConnectionValidator
{
    public ConnectionValidationResult Validate(ConnectionContext context)
    {
        // Check if adding this connection would create a cycle
        // by seeing if there's already a path from target to source
        if (HasPath(context.Graph, context.TargetNode.Id, context.SourceNode.Id))
        {
            return ConnectionValidationResult.Invalid("Connection would create a cycle");
        }

        return ConnectionValidationResult.Valid();
    }

    private static bool HasPath(Graph graph, string fromNodeId, string toNodeId)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(fromNodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == toNodeId)
                return true;

            if (!visited.Add(current))
                continue;

            // Find all nodes that this node connects to
            foreach (var edge in graph.Edges.Where(e => e.Source == current))
            {
                queue.Enqueue(edge.Target);
            }
        }

        return false;
    }
}

/// <summary>
/// Composite validator that combines multiple validators.
/// All validators must pass for the connection to be valid.
/// </summary>
public class CompositeConnectionValidator : IConnectionValidator
{
    private readonly List<IConnectionValidator> _validators = [];

    public CompositeConnectionValidator() { }

    public CompositeConnectionValidator(IEnumerable<IConnectionValidator> validators)
    {
        _validators.AddRange(validators);
    }

    /// <summary>
    /// Adds a validator to the composite.
    /// </summary>
    public CompositeConnectionValidator Add(IConnectionValidator validator)
    {
        _validators.Add(validator);
        return this;
    }

    public ConnectionValidationResult Validate(ConnectionContext context)
    {
        foreach (var validator in _validators)
        {
            var result = validator.Validate(context);
            if (!result.IsValid)
                return result;
        }

        return ConnectionValidationResult.Valid();
    }

    /// <summary>
    /// Creates a standard validator with common rules.
    /// </summary>
    public static CompositeConnectionValidator CreateStandard() =>
        new CompositeConnectionValidator()
            .Add(new NoSelfConnectionValidator())
            .Add(new NoDuplicateConnectionValidator())
            .Add(new TypeMatchingConnectionValidator());

    /// <summary>
    /// Creates a strict validator that also prevents cycles.
    /// </summary>
    public static CompositeConnectionValidator CreateStrict() =>
        new CompositeConnectionValidator()
            .Add(new NoSelfConnectionValidator())
            .Add(new NoDuplicateConnectionValidator())
            .Add(new TypeMatchingConnectionValidator())
            .Add(new NoCycleConnectionValidator());
}
