using Cairo;

namespace UserInterface.Views
{
    class CairoTextExtents : ITextExtents
    {
        Context context;

        public CairoTextExtents(Context cr)
        {
            context = cr;
        }

        public TextExtents TextExtents(string text)
        {
            return context.TextExtents(text);
        }
    }
}
