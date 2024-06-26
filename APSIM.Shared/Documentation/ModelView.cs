namespace APSIM.Shared.Documentation
{
    /// <summary>Describes a model view for the tags system.</summary>
    public class ModelView : ITag
    {
        /// <summary>Model</summary>
        public object model;

        /// <summary>Constructor</summary>
        /// <param name="modelToDocument">The model to document</param>
        public ModelView(object modelToDocument)
        {
            model = modelToDocument;
        }
    }
}
