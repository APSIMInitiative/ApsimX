using System;
using Markdig;
using Markdig.Renderers;

namespace UserInterface.Classes
{
    /// <summary>
    /// A markdown extension which allows &lt;sub&gt; and &lt;sup&gt; tags
    /// inside a body of text to be parsed as subscript/superscript content.
    /// </summary>
    public class SubSuperScriptExtensions : IMarkdownExtension
    {
        /// <summary>
        /// Configure a pipeline builder to use the sub/superscript parser.
        /// </summary>
        /// <param name="pipeline">Pipeline to be configured.</param>
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<SubSuperScriptParser>())
                pipeline.InlineParsers.Insert(0, new SubSuperScriptParser());
        }

        /// <summary>
        /// Configure a pipeline to use the sub/superscript parser.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="renderer">The renderer.</param>
        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            throw new NotImplementedException();
        }
    }
}
