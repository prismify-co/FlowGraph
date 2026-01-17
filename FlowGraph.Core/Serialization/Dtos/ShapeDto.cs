using FlowGraph.Core.Elements;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Core.Serialization.Dtos;

/// <summary>
/// Legacy data transfer object for Shape serialization (Version 1 format).
/// Maintained for backward compatibility when deserializing old files.
/// Supports polymorphic shape types (rectangle, line, text, ellipse).
/// </summary>
public class ShapeDto
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public PointDto Position { get; set; } = new();
    public double? Width { get; set; }
    public double? Height { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double StrokeWidth { get; set; } = 1.0;
    public double Opacity { get; set; } = 1.0;
    public double Rotation { get; set; }
    public string? Label { get; set; }
    public bool IsVisible { get; set; } = true;
    public int ZIndex { get; set; } = CanvasElement.ZIndexShapes;

    // Rectangle-specific
    public double? CornerRadius { get; set; }

    // Line-specific
    public double? EndX { get; set; }
    public double? EndY { get; set; }
    public string? StrokeDashArray { get; set; }
    public string? StartCap { get; set; }
    public string? EndCap { get; set; }

    // Text-specific
    public string? Text { get; set; }
    public double? FontSize { get; set; }
    public string? FontFamily { get; set; }
    public string? FontWeight { get; set; }
    public string? TextAlignment { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }

    public static ShapeDto FromShape(ShapeElement shape)
    {
        var dto = new ShapeDto
        {
            Id = shape.Id,
            Type = shape.Type,
            Position = new PointDto { X = shape.Position.X, Y = shape.Position.Y },
            Width = shape.Width,
            Height = shape.Height,
            Fill = shape.Fill,
            Stroke = shape.Stroke,
            StrokeWidth = shape.StrokeWidth,
            Opacity = shape.Opacity,
            Rotation = shape.Rotation,
            Label = shape.Label,
            IsVisible = shape.IsVisible,
            ZIndex = shape.ZIndex
        };

        // Populate type-specific properties
        if (shape is RectangleElement rect)
        {
            dto.CornerRadius = rect.CornerRadius;
        }
        else if (shape is LineElement line)
        {
            dto.EndX = line.EndX;
            dto.EndY = line.EndY;
            dto.StrokeDashArray = line.StrokeDashArray;
            dto.StartCap = line.StartCap.ToString();
            dto.EndCap = line.EndCap.ToString();
        }
        else if (shape is TextElement text)
        {
            dto.Text = text.Text;
            dto.FontSize = text.FontSize;
            dto.FontFamily = text.FontFamily;
            dto.FontWeight = text.FontWeight.ToString();
            dto.TextAlignment = text.TextAlignment.ToString();
        }
        else if (shape is EllipseElement)
        {
            // Ellipse has no additional properties beyond base
        }

        return dto;
    }

    public ShapeElement? ToShape()
    {
        ShapeElement? shape = Type.ToLowerInvariant() switch
        {
            "rectangle" => new RectangleElement(Id)
            {
                CornerRadius = CornerRadius ?? 0
            },
            "line" => new LineElement(Id)
            {
                EndX = EndX ?? 0,
                EndY = EndY ?? 0,
                StrokeDashArray = StrokeDashArray,
                StartCap = Enum.TryParse<LineCapStyle>(StartCap, out var sc) ? sc : LineCapStyle.None,
                EndCap = Enum.TryParse<LineCapStyle>(EndCap, out var ec) ? ec : LineCapStyle.None
            },
            "text" => new TextElement(Id)
            {
                Text = Text ?? string.Empty,
                FontSize = FontSize ?? 14,
                FontFamily = FontFamily ?? "Segoe UI",
                FontWeight = Enum.TryParse<Elements.Shapes.FontWeight>(FontWeight, out var fw) ? fw : Elements.Shapes.FontWeight.Normal,
                TextAlignment = Enum.TryParse<Elements.Shapes.TextAlignment>(TextAlignment, out var ta) ? ta : Elements.Shapes.TextAlignment.Left
            },
            "ellipse" => new EllipseElement(Id),
            _ => null
        };

        if (shape != null)
        {
            shape.Position = new Point(Position.X, Position.Y);
            shape.Width = Width;
            shape.Height = Height;
            shape.Fill = Fill;
            shape.Stroke = Stroke;
            shape.StrokeWidth = StrokeWidth;
            shape.Opacity = Opacity;
            shape.Rotation = Rotation;
            shape.Label = Label;
            shape.IsVisible = IsVisible;
            shape.ZIndex = ZIndex;
        }

        return shape;
    }
}
