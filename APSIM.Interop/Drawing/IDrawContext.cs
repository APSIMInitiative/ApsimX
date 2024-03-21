using System.Drawing;

namespace APSIM.Interop.Drawing
{
    public enum States
    {
        Normal,
        Selected,
        Insensitive,
        Calculated
    }

    public interface IDrawContext
    {
        /// <summary>
        /// Sets the current line width within the Context. The line width value
        /// specifies the diameter of a pen that is circular in user space, (though
        /// device-space pen may be an ellipse in general due to
        /// scaling/shear/rotation of the CTM).
        /// 
        /// Note: When the description above refers to user space and CTM it refers
        /// to the user space and CTM in effect at the time of the stroking operation,
        /// not the user space and CTM in effect at the time of the call to SetLineWidth().
        /// The simplest usage makes both of these spaces identical. That is, if there is
        /// no change to the CTM between a call to set_line_width() and the stroking
        /// operation, then one can just pass user-space values to SetLineWidth() and
        /// ignore this note.
        /// 
        /// As with the other stroke parameters, the current line width is examined by
        /// Stroke(), stroke_extents(), and stroke_to_path(), but does not have any effect
        /// during path construction.
        /// 
        /// The default line width value is 2.0.
        /// </summary>
        /// <param name="lineWidth">A line width.</param>
        void SetLineWidth(double lineWidth);

        States State { get; set; }
        (int Left, int Top, int Width, int Height) GetPixelExtents(string text, bool bold, bool italics);

        /// <summary>
        /// Adds a closed sub-path rectangle of the given size to the current path at position
        /// (x, y) in user-space coordinates.
        /// </summary>
        /// <param name="rectangle">The rectangle dimensions.</param>
        void Rectangle(Rectangle rectangle);

        /// <summary>
        /// Establishes a new clip region by intersecting the current clip region with the
        /// current path as it would be filled by Fill() and according to the current
        /// fill rule (see SetFillRule()).
        /// 
        /// After Clip(), the current path will be cleared from the drawing context.
        /// 
        /// The current clip region affects all drawing operations by effectively masking
        /// out any changes to the surface that are outside the current clip region.
        /// 
        /// Calling clip() can only make the clip region smaller, never larger. But the
        /// current clip is part of the graphics state, so a temporary restriction of the
        /// clip region can be achieved by calling Clip() within a Save()/Restore() pair.
        /// The only other means of increasing the size of the clip region is ResetClip().
        /// </summary>
        void Clip();

        /// <summary>
        /// A drawing operator that strokes the current path according to the current line
        /// width, line join, line cap, and dash settings. After Stroke(), the current path
        /// will be cleared from the cairo context. See SetLineWidth(), SetLineJoin(),
        /// SetLineCap(), SetDash(), and StrokePreserve().
        /// 
        /// Note: Degenerate segments and sub-paths are treated specially and provide a useful
        /// result. These can result in two different situations:
        /// 
        /// 1. Zero-length "on" segments set in SetDash(). If the cap style is LineCap.Round
        /// or LineCap.Square then these segments will be drawn as circular dots or squares
        /// respectively. In the case of LineCap.Square, the orientation of the squares is
        /// determined by the direction of the underlying path.
        /// 
        /// 2. A sub-path created by MoveTo() followed by either a ClosePath() or one or more
        /// calls to LineTo() to the same coordinate as the MoveTo(). If the cap style is
        /// LineCap.Round then these sub-paths will be drawn as circular dots. Note that in the
        /// case of LineCap.Square a degenerate sub-path will not be drawn at all, (since the
        /// correct orientation is indeterminate).
        /// 
        /// In no case will a cap style of LineCap.Butt cause anything to be drawn in the case
        /// of either degenerate segments or sub-paths.
        /// </summary>
        void Stroke();

        /// <summary>
        /// Begin a new sub-path. After this call the current point will be (x, y).
        /// </summary>
        /// <param name="x">The X coordinate of the new position.</param>
        /// <param name="y">The Y coordinate of the new position.</param>
        void MoveTo(double x, double y);

        /// <summary>
        /// Reset the current clip region to its original, unrestricted state. That is, set the
        /// clip region to an infinitely large shape containing the target surface. Equivalently,
        /// if infinity is too hard to grasp, one can imagine the clip region being reset to the
        /// exact bounds of the target surface.
        /// 
        /// Note that code meant to be reusable should not call ResetClip() as it will cause results
        /// unexpected by higher-level code which calls Clip(). Consider using Save() and Restore()
        /// around Clip() as a more robust means of temporarily restricting the clip region.
        /// </summary>
        void ResetClip();

        void DrawFilledRectangle(int left, int top, int width, int height);
        void DrawFilledRectangle();

        /// <summary>
        /// Sets the source pattern within Context to a colour. This colour will then be used for
        /// any subsequent drawing operation until a new source pattern is set.
        /// 
        /// The default source pattern is opaque black.
        /// </summary>
        /// <param name="color">The source colour to use.</param>
        void SetColour(Color color);

        /// <summary>
        /// A drawing operator that generates the shape from a string of text, rendered according
        /// to the current font face, font size (font matrix), and font options.
        /// 
        /// This function first computes a set of glyphs for the string of text. The first glyph
        /// is placed so that its origin is at the current point. The origin of each subsequent glyph
        /// is offset from that of the previous glyph by the advance values of the previous glyph.
        /// 
        /// After this call the current point is moved to the origin of where the next glyph would
        /// be placed in this same progression. That is, the current point will be at the origin
        /// of the final glyph offset by its advance values. This allows for easy display of a
        /// single logical string with multiple calls to DrawText().
        /// 
        /// Note: The DrawText() function call is part of what the cairo designers call the "toy"
        /// text API. It is convenient for short demos and simple programs, but it is not expected
        /// to be adequate for serious text-using applications. See ShowGlyphs() for the "real" text
        /// display API in cairo.
        /// </summary>
        /// <param name="text">The text to be shown.</param>
        /// <param name="bold">Add the text in bold?</param>
        /// <param name="italics">Add the text in italics?</param>
        void DrawText(string text, bool bold, bool italics);

        /// <summary>
        /// A drawing operator that generates the shape from a string of text, rendered according
        /// to the current font face, font size (font matrix), and font options.
        /// 
        /// This function first computes a set of glyphs for the string of text. The first glyph
        /// is placed so that its origin is at the current point. The origin of each subsequent glyph
        /// is offset from that of the previous glyph by the advance values of the previous glyph.
        /// 
        /// After this call the current point is moved to the origin of where the next glyph would
        /// be placed in this same progression. That is, the current point will be at the origin
        /// of the final glyph offset by its advance values. This allows for easy display of a
        /// single logical string with multiple calls to DrawText().
        /// 
        /// Note: The DrawText() function call is part of what the cairo designers call the "toy"
        /// text API. It is convenient for short demos and simple programs, but it is not expected
        /// to be adequate for serious text-using applications. See ShowGlyphs() for the "real" text
        /// display API in cairo.
        /// </summary>
        /// <param name="text">Text to be rendered.</param>
        void ShowText(string text);

        /// <summary>
        /// Clear the current path. After this call, there will be no current
        /// path or point.
        /// </summary>
        void NewPath();

        /// <summary>
        /// Adds a cubic Bézier spline to the path from the current point to
        /// position (x2, y2) in user-space coordinates, using (x0, y0) and
        /// (x1, y1) as the control points. After this call the current point
        /// will be (x2, y2). If there is no current point before the call to
        /// CurveTo(), this function will behave as if preceded by a call to
        /// MoveTo(x0, y0).
        /// </summary>
        /// <param name="x0">The X coordinate of the first control point.</param>
        /// <param name="y0">The Y coordinate of the first control point.</param>
        /// <param name="x1">The X coordinate of the second control point.</param>
        /// <param name="y1">The Y coordinate of the second control point.</param>
        /// <param name="x2">The X coordinate of the end of the curve.</param>
        /// <param name="y2">The Y coordinate of the end of the curve.</param>
        void CurveTo(double x0, double y0, double x1, double y1, double x2, double y2);

        /// <summary>
        /// Adds a line to the path from the current point to position (x, y) in
        /// user-space coordinates. After this call the current point will be (x, y).
        /// If there is no current point before the call to line_to() this function
        /// will behave as ctx.move_to(x, y).
        /// </summary>
        /// <param name="x">The X coordinate of the end of the new line.</param>
        /// <param name="y">The Y coordinate of the end of the new line.</param>
        void LineTo(double x, double y);

        /// <summary>
        /// Adds a circular arc of the given radius to the current path. The arc is
        /// centered at (xc, yc), begins at angle1 and proceeds in the direction of
        /// increasing angles to end at angle2. If angle2 is less than angle1 it will
        /// be progressively increased by 2*PI until it is greater than angle1.
        ///
        /// If there is a current point, an initial line segment will be added to the
        /// path to connect the current point to the beginning of the arc. If this
        /// initial line is undesired, it can be avoided by calling NewSubPath() before
        /// calling Arc().
        /// 
        /// Angles are measured in radians. An angle of 0.0 is in the direction of the
        /// positive X axis (in user space). An angle of PI/2.0 radians (90 degrees) is
        /// in the direction of the positive Y axis (in user space). Angles increase in
        /// the direction from the positive X axis toward the positive Y axis. So with
        /// the default transformation matrix, angles increase in a clockwise direction.
        /// 
        /// This function gives the arc in the direction of increasing angles; see
        /// ArcNegative() to get the arc in the direction of decreasing angles.
        /// 
        /// The arc is circular in user space. To achieve an elliptical arc, you can scale
        /// the current transformation matrix by different amounts in the X and Y directions.
        /// </summary>
        /// <param name="xc">X position of the center of the arc.</param>
        /// <param name="yc">Y position of the center of the arc.</param>
        /// <param name="radius">The radius of the arc.</param>
        /// <param name="angle1">The start angle, in radians.</param>
        /// <param name="angle2">The end angle, in radians.</param>
        void Arc(double xc, double yc, double radius, double angle1, double angle2);

        /// <summary>
        /// A drawing operator that strokes the current path according to the current
        /// line width, line join, line cap, and dash settings. Unlike Stroke(),
        /// StrokePreserve() preserves the path within the cairo context. See
        /// <see cref="SetLineWidth(double)"/>, SetLineJoin(), SetLineCap(), SetDash(),
        /// StrokePreserve() (most of which aren't wrapped by this interface currently
        /// because we don't need them).
        /// </summary>
        void StrokePreserve();

        /// <summary>
        /// A drawing operator that fills the current path according to the current fill
        /// rule, (each sub-path is implicitly closed before being filled). After Fill(),
        /// the current path will be cleared from the Context. See SetFillRule() and
        /// FillPreserve().
        /// </summary>
        void Fill();

        /// <summary>
        /// Sets the current font matrix to a scale by a factor of size, replacing any
        /// font matrix previously set with SetFontSize() or SetFontMatrix(). This
        /// results in a font size of size user space units. (More precisely, this matrix
        /// will result in the font’s em-square being a size by size square in user space.)
        /// 
        /// If text is drawn without a call to SetFontSize(), (nor SetFontMatrix() nor
        /// SetScaledFont()), the default font size is 10.0.
        /// </summary>
        /// <param name="size">The new font size, in user space units.</param>
        void SetFontSize(double size);

        /// <summary>
        /// Gets the extents for a string of text. The extents describe a user-space
        /// rectangle that encloses the "inked" portion of the text, (as it would be
        /// drawn by DrawText()).
        /// 
        /// Note that whitespace characters do not directly contribute to the size of
        /// the rectangle (width and height). They do contribute indirectly by changing
        /// the position of non-whitespace characters. In particular, trailing whitespace
        /// characters are likely to not affect the size of the rectangle.
        /// </summary>
        /// <param name="text">The text to be measured.</param>
        Rectangle GetTextExtents(string text);
    }
}
