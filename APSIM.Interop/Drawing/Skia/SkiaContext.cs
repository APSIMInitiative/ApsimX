using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        private SKRect? currentRectangle = null;

        /// <summary>
        /// The current drawing location (as set by <see cref="MoveTo()"/>).
        /// </summary>
        private SKPoint? currentPoint = null;

        /// <summary>
        /// The current drawing path.
        /// </summary>
        private SKPath currentPath = null;

        /// <summary>
        /// This is a stand-in for the current path when an action
        /// can't necessarily be represented by an SKPath, but we
        /// also don't want to immediately perform the action because
        /// we don't necessarily know if we should be stroking or
        /// filling the path.
        /// </summary>
        private Action<SKPaint> currentAction = null;

        /// <summary>
        /// Create a new <see cref="SkiaContext"/> instance of the given width and height.
        /// </summary>
        /// <param name="width">Width of the canvas.</param>
        /// <param name="height">Height of the canvas.</param>
        public SkiaContext(int width, int height)
        {
            bitmap = new SKBitmap(width, height);
            canvas = new SKCanvas(bitmap);
            paint.IsAntialias = true;
        }

        /// <inheritdoc />
        public States State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc />
        public void Arc(double xc, double yc, double radius, double angle1, double angle2)
        {
            // The angles coming in are in radians, but skia expects angles in degrees.
            const double rad2Deg = 180.0 / Math.PI;
            angle1 *= rad2Deg;
            angle2 *= rad2Deg;

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
            currentAction = p => canvas.DrawArc(oval, startAngle, sweepAngle, false, p);
        }

        /// <inheritdoc />
        public void Clip()
        {
            canvas.Save();
            if (currentPath != null)
                canvas.ClipPath(currentPath);
            else if (currentRectangle != null)
                canvas.ClipRect((SKRect)currentRectangle);
            else
                throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void CurveTo(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            if (currentPoint == null)
                // This may be a little harsh. We could instead use (0, 0) as the start point.
                throw new InvalidOperationException("Cannot draw bezier curve - start point has not been set (e.g. via MoveTo())");
            SKPath path = new SKPath();
            path.MoveTo((SKPoint)currentPoint);
            path.CubicTo((float)x0, (float)y0, (float)x1, (float)y1, (float)x2, (float)y2);

            currentPoint = new SKPoint((float)x2, (float)y2);
            currentPath = path;
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
            if (currentRectangle == null)
                throw new InvalidOperationException("Unable to draw filled rectangle - programmer is missing a call to Rectangle()");

            SKRect rect = (SKRect)currentRectangle;

            float left = (float)rect.Left;
            float top = (float)rect.Top;
            float right = (float)(rect.Left + rect.Width);
            float bottom = (float)(rect.Top + rect.Height);
            rect = new SKRect(left, top, right, bottom);

            DrawRectangle(rect, true);
        }

        /// <inheritdoc />
        public void DrawText(string text, bool bold, bool italics)
        {
            if (currentPoint == null)
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
            if (currentAction != null)
                Draw(currentAction, true);
            else
            {
                if (currentPath == null)
                    throw new InvalidOperationException("Unable to fill current path: current path is null");
                DrawPath(currentPath, true);
            }
        }

        /// <inheritdoc />
        public (int Left, int Top, int Width, int Height) GetPixelExtents(string text, bool bold, bool italics)
        {
            // tbi: account for text formatting.
            var rect = GetTextExtents(text);
            return (rect.Left, rect.Top, rect.Width, rect.Height);
        }

        /// <inheritdoc />
        public Rectangle GetTextExtents(string text)
        {
            // testme
            ReadOnlySpan<ushort> glyphs = Encoding.UTF8.GetBytes(text).Select(b => (ushort)b).ToArray();
            SKFont font = new SKFont();
            float numGlyphs = font.MeasureText(glyphs, out SKRect bounds, paint);
            Rectangle rect = new Rectangle((int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height);
            return rect;
        }

        /// <inheritdoc />
        public void LineTo(double x, double y)
        {
            if (currentPoint == null)
                MoveTo(x, y);
            else
            {
                if (currentPath == null)
                    currentPath = new SKPath();
                if (!currentPath.LastPoint.Equals(currentPoint))
                    currentPath.MoveTo((SKPoint)currentPoint);
                currentPoint = new SKPoint((float)x, (float)y);
                currentPath.LineTo((SKPoint)currentPoint);
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
            currentAction = null;
            currentPoint = null;
            currentRectangle = null;
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

        /// <summary>
        /// Save the current state of the drawing context to an image.
        /// </summary>
        public SKImage Save()
        {
            return SKImage.FromBitmap(bitmap);
        }

        /// <inheritdoc />
        public void SetColour(Color color)
        {
            // todo: improve handling of alpha channel here
            paint.Color = new SKColor(color.R, color.G, color.B, color.A);
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
            canvas.DrawText(text, (SKPoint)currentPoint, paint);
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
            if (currentAction != null)
                Draw(currentAction, false);
            else
            {
                if (currentPath == null)
                    throw new InvalidOperationException("Unable to draw stroke - current path is null");
                DrawPath(currentPath, false);
            }
        }

        /// <summary>
        /// Draw the given path with current paint settings.
        /// </summary>
        /// <param name="path">Path to be drawn.</param>
        /// <param name="fill">Should we fill the path (ie with colour)?</param>
        private void DrawPath(SKPath path, bool fill)
        {
            Draw(p => canvas.DrawPath(path, p), fill);
        }

        /// <summary>
        /// Draw the given rectangle with current paint settings.
        /// </summary>
        /// <param name="rect">Rectangle to be drawn.</param>
        /// <param name="fill">Should we fill the rectangle (ie with colour)?</param>
        private void DrawRectangle(SKRect rect, bool fill)
        {
            Draw(p => canvas.DrawRect(rect, p), fill);
        }

        /// <summary>
        /// Perform the given drawing action with or without fill,
        /// using the current paint settings.
        /// </summary>
        /// <param name="action">The drawing action to be performed.</param>
        /// <param name="fill">Should we fill the area (ie with colour)?</param>
        private void Draw(Action<SKPaint> action, bool fill)
        {
            SKPaintStyle style = paint.Style;
            paint.Style = fill ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
            action(paint);
            paint.Style = style;
        }
    }
}
