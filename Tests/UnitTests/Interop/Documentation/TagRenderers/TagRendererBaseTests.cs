using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;
using Document = MigraDocCore.DocumentObjectModel.Document;
using NUnit.Framework;
using System;

namespace UnitTests.Interop.Documentation.TagRenderers
{
    /// <summary>
    /// Tests for the <see cref="TagRendererBase{T}"/> class.
    /// </summary>
    /// <remarks>
    /// fixme: should really extract an interface from PdfBuilder. The problem is
    /// that the callees (specifically, markdown tag renderers) need to access some
    /// methods which are inherited from an abstract base class (in the MarkDig library).
    /// </remarks>
    [TestFixture]
    public class TagRendererBaseTests
    {
        private class TagBase : ITag { }
        private class SubTag : TagBase { }
        private class Renderer : TagRendererBase<TagBase>
        {
            protected override void Render(TagBase tag, PdfBuilder renderer)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Ensure that TagRendererBase can render a tag of the same type as its type parameter.
        /// </summary>
        [Test]
        public void TestCanRender()
        {
            ITagRenderer renderer = new Renderer();
            Assert.That(renderer.CanRender(new TagBase()), Is.True);
        }

        /// <summary>
        /// Ensure that TagRendererBase can render a type derived from its type parameter.
        /// </summary>
        [Test]
        public void TestCanRenderSubclass()
        {
            ITagRenderer renderer = new Renderer();
            Assert.That(renderer.CanRender(new SubTag()), Is.True);
        }

        /// <summary>
        /// Ensure that TagRendererBase cannot render a type which is not
        /// derived from its type parameter.
        /// </summary>
        [Test]
        public void TestCannotRenderOtherClass()
        {
            ITagRenderer renderer = new Renderer();
            Assert.That(renderer.CanRender(new MockTag(p => { })), Is.False);
        }

        /// <summary>
        /// Ensure that the abstract render method is called.
        /// </summary>
        [Test]
        public void TestRender()
        {
            PdfBuilder builder = new PdfBuilder(new Document(), PdfOptions.Default);
            ITag tag = new SubTag();
            ITagRenderer renderer = new Renderer();
            Assert.Throws<NotImplementedException>(() => renderer.Render(tag, builder));
        }

        /// <summary>
        /// Ensure that attempts to render an unsupported type result in an exception.
        /// </summary>
        [Test]
        public void TestRenderInvalidType()
        {
            PdfBuilder builder = new PdfBuilder(new Document(), PdfOptions.Default);
            ITag tag = new MockTag(p => { });
            ITagRenderer renderer = new Renderer();
            Assert.Throws<InvalidCastException>(() => renderer.Render(tag, builder));
        }
    }
}