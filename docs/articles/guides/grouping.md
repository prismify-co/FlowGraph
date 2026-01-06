# Node Grouping

Groups allow you to organize related nodes together. Groups can be collapsed, expanded, and nested.

## Creating Groups

### From Selected Nodes

```csharp
// Group currently selected nodes
canvas.GroupSelected();

// With a custom label
canvas.GroupSelected("Processing Pipeline");
```

### Programmatically

```csharp
var group = new Node
{
    Type = "group",
    IsGroup = true,
    Label = "My Group",
    Position = new Point(100, 100),
    Width = 400,
    Height = 300
};

graph.AddNode(group);

// Add nodes to the group
canvas.GroupManager.AddNodeToGroup(group.Id, node1.Id);
canvas.GroupManager.AddNodeToGroup(group.Id, node2.Id);
```

## Collapse and Expand

```csharp
// Toggle collapse state
canvas.ToggleGroupCollapse(groupId);

// Explicit collapse/expand
canvas.CollapseGroup(groupId);
canvas.ExpandGroup(groupId);

// With animation
canvas.AnimateGroupCollapse(groupId, duration: 0.5);
canvas.AnimateGroupExpand(groupId, duration: 0.5);
```

## Group Ports

Groups can have input and output ports, allowing connections between the group and external nodes:

```csharp
var group = new Node
{
    Type = "group",
    IsGroup = true,
    Label = "Pipeline",
    Position = new Point(100, 100),
    Width = 400,
    Height = 300,
    Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }],
    Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
};
```

When a group is collapsed, edges connected to nodes inside the group are automatically re-routed to proxy ports on the group boundary.

## Managing Group Membership

```csharp
// Add node to group
canvas.GroupManager.AddNodeToGroup(groupId, nodeId);

// Remove node from group
canvas.GroupManager.RemoveNodeFromGroup(nodeId);

// Get group children
var children = graph.GetGroupChildren(groupId);

// Get all descendants (recursive)
var allDescendants = graph.GetGroupChildrenRecursive(groupId);
```

## Ungrouping

```csharp
// Ungroup selected groups
canvas.UngroupSelected();

// Programmatically
canvas.GroupManager.Ungroup(groupId);
```

## Nested Groups

Groups can contain other groups for hierarchical organization:

```csharp
var outerGroup = new Node { IsGroup = true, Label = "Outer" };
var innerGroup = new Node { IsGroup = true, Label = "Inner" };

graph.AddNode(outerGroup);
graph.AddNode(innerGroup);

canvas.GroupManager.AddNodeToGroup(outerGroup.Id, innerGroup.Id);
canvas.GroupManager.AddNodeToGroup(innerGroup.Id, someNode.Id);
```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+G` | Group selected nodes |
| `Ctrl+Shift+G` | Ungroup selected group |
