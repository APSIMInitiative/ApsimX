using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using SkiaSharp;

namespace APSIM.Interop.Drawing.Skia
{
    /// <summary>
    /// A drawing context for a 
    /// </summary>
    public class SkiaContext : IDrawContext, IDisposable
    {
        private SKCanvas canvas;
        private SKBitmap bitmap;

        /// <summary>
        /// This is the current paint settings - colour, blend, stroke, etc.
        /// </summary>
        private SKPaint paint = new SKPaint();

        /// <summary>
        /// This is the current font settings.
        /// </summary>
        /// <returns></returns>
        private SKFont font = new SKFont();

        /// <summary>
        /// The current path (a rectangle).
        /// </summary>
        private SKRect currentRectangle = SKRect.Empty;

        /// <summary>
        /// The current drawing location (as set by <see cref="MoveTo()"/>).
        /// </summary>
        private SKPoint currentPoint = SKPoint.Empty;

        /// <summary>
        /// The current drawing path.
        /// </summary>
        private SKPath currentPath = null;

        /// <summary>
        /// Create a new <see cref="SkiaContext"/> instance of the given width and height.
        /// </summary>
        /// <param name="width">Width of the canvas.</param>
        /// <param name="height">Height of the canvas.</param>
        public SkiaContext(int width, int height)
        {
            bitmap = new SKBitmap(width, height);
            canvas = new SKCanvas(bitmap);
        }

        /// <inheritdoc />
        public States State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc />
        public void Arc(double xc, double yc, double radius, double angle1, double angle2)
        {
            float left = (float)(xc - radius);
            float right = (float)(xc + radius);
            float top = (float)(yc - radius);
            float bottom = (float)(yc + radius);
            SKRect oval = new SKRect(left, top, right, bottom);

            // The second angle we pass in is the "sweep" angle (ie a delta).
            // It's not the actual end angle - it's how much of the ellipse we want to draw.
            float startAngle = (float)angle1;
            float sweepAngle = (float)(angle2 - angle1);

            // The useCenter argument will draw straight lines to the center point,
            // resulting in a wedge being drawn. This should always be false here.
            canvas.DrawArc(oval, startAngle, sweepAngle, false, paint);
        }

        /// <inheritdoc />
        public void Clip()
        {
            canvas.Save();
            if (currentPath != null)
                canvas.ClipPath(currentPath);
            else if (!currentRectangle.Equals(SKRect.Empty))
                canvas.ClipRect(currentRectangle);
            else
                throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void CurveTo(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            if (currentPoint == SKPoint.Empty)
                // This may be a little harsh. We could instead use (0, 0) as the start point.
                throw new InvalidOperationException("Cannot draw bezier curve - start point has not been set (e.g. via MoveTo())");
            SKPath path = new SKPath();
            path.MoveTo(currentPoint);
            path.CubicTo((float)x0, (float)y0, (float)x1, (float)y1, (float)x2, (float)y2);

            DrawPath(path, false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            canvas.Dispose();
            bitmap.Dispose();
        }

        /// <inheritdoc />
        public void DrawFilledRectangle(int left, int top, int width, int height)
        {
            SKRect rect = new SKRect(left, top, left + width, top + height);
            DrawRectangle(rect, true);
        }

        /// <inheritdoc />
        public void DrawFilledRectangle()
        {
            if (currentRectangle.Equals(SKRect.Empty))
                throw new InvalidOperationException("Unable to draw filled rectangle - programmer is missing a call to Rectangle()");

            float left = (float)currentRectangle.Left;
            float top = (float)currentRectangle.Top;
            float right = (float)(currentRectangle.Left + currentRectangle.Width);
            float bottom = (float)(currentRectangle.Top + currentRectangle.Height);
            SKRect rect = new SKRect(left, top, right, bottom);

            DrawRectangle(rect, true);
        }

        /// <inheritdoc />
        public void DrawText(string text, bool bold, bool italics)
        {
            if (currentPoint == SKPoint.Empty)
                throw new InvalidOperationException("Unable to draw text - programmer is missing a call to MoveTo()");
                
            if (bold || italics)
                Debug.WriteLine("todo: bold/italic text in skia drawing context");
            ShowText(text);
        }

        /// <inheritdoc />
        public void Fill()
        {
            FillPreserve();
            NewPath();
        }

        /// <inheritdoc />
        public void FillPreserve()
        {
            if (currentPath == null)
                throw new InvalidOperationException("Unable to fill current path: current path is null");
            DrawPath(currentPath, true);
        }

        /// <inheritdoc />
        public (int Left, int Right, int Width, int Height) GetPixelExtents(string text, bool bold, bool italics)
        {
            // tbi: account for text formatting.
            var rect = GetTextExtents(text);
            return (rect.Left, rect.Right, rect.Width, rect.Height);
        }

        /// <inheritdoc />
        public Rectangle GetTextExtents(string text)
        {
            // testme
            ReadOnlySpan<ushort> glyphs = Encoding.UTF8.GetBytes(text).Select(b => (ushort)b).ToArray();
            SKFont font = new SKFont();
            float numGlyphs = font.MeasureText(glyphs, out SKRect bounds, paint);
            Rectangle rect = new Rectangle((int)bounds.Left, (int)bounds.Right, (int)bounds.Width, (int)bounds.Height);
            return rect;
        }

        /// <inheritdoc />
        public void LineTo(double x, double y)
        {
            if (currentPoint.Equals(SKPoint.Empty))
                MoveTo(x, y);
            else
            {
                if (currentPath == null)
                    currentPath = new SKPath();
                currentPoint = new SKPoint((float)x, (float)y);
                currentPath.LineTo(currentPoint);
            }
        }

        /// <inheritdoc />
        public void MoveTo(double x, double y)
        {
            currentPoint = new SKPoint((float)x, (float)y);
        }

        /// <inheritdoc />
        public void NewPath()
        {
            if (currentPath != null)
            {
                currentPath.Dispose();
                currentPath = null;
            }
        }

        /// <inheritdoc />
        public void Rectangle(Rectangle rectangle)
        {
            currentRectangle = new SKRect(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        /// <inheritdoc />
        public void ResetClip()
        {
            canvas.Restore();
        }

        /// <inheritdoc />
        public void SetColour(Color color)
        {
            paint.Color = new SKColor(color.R, color.G, color.B);
        }

        /// <inheritdoc />
        public void SetFontSize(double size)
        {
            font.Size = (float)size;
        }

        /// <inheritdoc />
        public void SetLineWidth(double lineWidth)
        {
            paint.StrokeWidth = (float)lineWidth;
        }

        /// <inheritdoc />
        public void ShowText(string text)
        {
            canvas.DrawText(text, currentPoint, paint);
        }

        /// <inheritdoc />
        public void Stroke()
        {
            StrokePreserve();
            NewPath();
        }

        /// <inheritdoc />
        public void StrokePreserve()
        {
            if (currentPath == null)
                throw new InvalidOperationException("Unable to draw stroke - current path is null");
            DrawPath(currentPath, false);
        }

        /// <summary>
        /// Draw the given path with current paint settings.
        /// </summary>
        /// <param name="path">Path to be drawn.</param>
        /// <param name="fill">Should we fill the path (ie with colour)?</param>
        private void DrawPath(SKPath path, bool fill)
        {
            SKPaintStyle style = paint.Style;
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawPath(currentPath, paint);
            paint.Style = style;
        }

        /// <summary>
        /// Draw the given rectangle with current paint settings.
        /// </summary>
        /// <param name="rect">Rectangle to be drawn.</param>
        /// <param name="fill">Should we fill the rectangle (ie with colour)?</param>
        private void DrawRectangle(SKRect rect, bool fill)
        {
            SKPaintStyle style = paint.Style;
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawRect(rect, paint);
            paint.Style = style;
        }
    }
}