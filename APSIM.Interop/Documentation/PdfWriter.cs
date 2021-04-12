using APSIM.Services.Documentation;
using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Documentation.Helpers;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.Rendering;
using PdfSharpCore.Fonts;
#else
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
#endif

namespace APSIM.Interop.Documentation
{
    /// <summary>
    /// This class will generate a PDF file from a collection of tags.
    /// </summary>
    public static class PdfWriter
    {
        public static void Write(string fileName, IEnumerable<ITag> tags)
        {
            // This is a bit tricky on non-Windows platforms. 
            // Normally PdfSharp tries to get a Windows DC for associated font information
            // See https://alex-maz.info/pdfsharp_150 for the work-around we can apply here.
            // See also http://stackoverflow.com/questions/32726223/pdfsharp-migradoc-font-resolver-for-embedded-fonts-system-argumentexception
            // The work-around is to register our own fontresolver. We don't need to do this on Windows.
            if (!ProcessUtilities.CurrentOS.IsWindows && !(GlobalFontSettings.FontResolver is FontResolver))
                GlobalFontSettings.FontResolver = new FontResolver();
            
            Document pdf = new Document();

            foreach (ITag tag in tags)
            {
                Section section = pdf.AddSection();
                section.Add(tag);
            }

            PdfDocumentRenderer renderer = new PdfDocumentRenderer(false);
            renderer.Document = pdf;
            renderer.RenderDocument();
            renderer.Save(fileName);
        }
    }
}