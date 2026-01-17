using FlowGraph.Core.Models;

namespace FlowGraph.Core.Serialization.Dtos;

/// <summary>
/// Data transfer object for LabelInfo serialization.
/// </summary>
public class LabelInfoDto
{
    public required string Text { get; set; }
    public LabelAnchor Anchor { get; set; } = LabelAnchor.Center;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double PerpendicularOffset { get; set; }

    public static LabelInfoDto FromLabelInfo(LabelInfo labelInfo)
    {
        return new LabelInfoDto
        {
            Text = labelInfo.Text,
            Anchor = labelInfo.Anchor,
            OffsetX = labelInfo.OffsetX,
            OffsetY = labelInfo.OffsetY,
            PerpendicularOffset = labelInfo.PerpendicularOffset
        };
    }

    public LabelInfo ToLabelInfo()
    {
        return new LabelInfo(Text, Anchor, OffsetX, OffsetY, PerpendicularOffset);
    }
}
