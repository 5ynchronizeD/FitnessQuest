using Microsoft.Maui.Graphics;

namespace FitnessQuest.Drawing;

/// <summary>
/// Lightweight bar/line chart drawn with Microsoft.Maui.Graphics — no external
/// charting dependency. Feed it values + labels and assign to a GraphicsView.
/// </summary>
public class ChartDrawable : IDrawable
{
    public double[] Values { get; set; } = Array.Empty<double>();
    public string[] Labels { get; set; } = Array.Empty<string>();
    public Color BarColor { get; set; } = Color.FromArgb("#7C4DFF");
    public Color TextColor { get; set; } = Color.FromArgb("#9AA0C7");
    public bool IsLine { get; set; }
    public double? GoalValue { get; set; }
    public string ValueFormat { get; set; } = "0";
    public bool ShowValues { get; set; } = true;
    /// <summary>When false (line charts), scale between data min and max instead of 0..max.</summary>
    public bool ZeroBased { get; set; } = true;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Values.Length == 0) return;

        float leftPad = 8, rightPad = 8, topPad = 18, bottomPad = 22;
        float w = dirtyRect.Width - leftPad - rightPad;
        float h = dirtyRect.Height - topPad - bottomPad;
        if (w <= 0 || h <= 0) return;

        double dataMax = Math.Max(Values.Max(), GoalValue ?? 0);
        double floor = 0;
        if (!ZeroBased && Values.Length > 0)
        {
            double dMin = Values.Min();
            double dMax = Values.Max();
            double pad = Math.Max(1, (dMax - dMin) * 0.2);
            floor = dMin - pad;
            dataMax = dMax + pad;
        }
        double range = dataMax - floor;
        if (range <= 0) range = 1;
        if (ZeroBased) range *= 1.15; // headroom above tallest bar

        float baseY = topPad + h;
        int n = Values.Length;
        float slot = w / n;

        canvas.FontSize = 9;

        float Y(double v) => baseY - (float)((v - floor) / range) * h;

        // Goal line
        if (GoalValue is double goal && goal > 0)
        {
            float gy = Y(goal);
            canvas.StrokeColor = Color.FromArgb("#00E5A0");
            canvas.StrokeDashPattern = new float[] { 4, 4 };
            canvas.StrokeSize = 1.5f;
            canvas.DrawLine(leftPad, gy, leftPad + w, gy);
            canvas.StrokeDashPattern = null;
        }

        if (IsLine)
        {
            var points = new List<PointF>();
            for (int i = 0; i < n; i++)
            {
                float cx = leftPad + slot * i + slot / 2;
                float cy = Y(Values[i]);
                points.Add(new PointF(cx, cy));
            }
            canvas.StrokeColor = BarColor;
            canvas.StrokeSize = 3;
            for (int i = 0; i < points.Count - 1; i++)
                canvas.DrawLine(points[i], points[i + 1]);
            canvas.FillColor = BarColor;
            foreach (var p in points)
                canvas.FillCircle(p, 4);
        }
        else
        {
            float barW = slot * 0.6f;
            canvas.FillColor = BarColor;
            for (int i = 0; i < n; i++)
            {
                float y = Y(Values[i]);
                float bh = baseY - y;
                float x = leftPad + slot * i + (slot - barW) / 2;
                if (bh > 0)
                    canvas.FillRoundedRectangle(x, y, barW, bh, 4);

                if (ShowValues && Values[i] > 0)
                {
                    canvas.FontColor = TextColor;
                    canvas.DrawString(Values[i].ToString(ValueFormat),
                        leftPad + slot * i, y - 14, slot, 12,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
        }

        // X labels
        canvas.FontColor = TextColor;
        for (int i = 0; i < n && i < Labels.Length; i++)
        {
            canvas.DrawString(Labels[i], leftPad + slot * i, baseY + 4, slot, 16,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
