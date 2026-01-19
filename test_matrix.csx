using Avalonia.Media;
var m = new Matrix(2, 0, 0, 2, 100, 50);
Console.WriteLine($"Transform (10,10): ({10 * m.M11 + 10 * m.M21 + m.M31}, {10 * m.M12 + 10 * m.M22 + m.M32})");
var pt = m.Transform(new Avalonia.Point(10, 10));
Console.WriteLine($"Avalonia Transform: {pt}");
