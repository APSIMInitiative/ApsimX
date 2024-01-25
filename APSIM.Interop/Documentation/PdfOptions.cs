using System;
using System.IO;
using APSIM.Interop.Documentation.Helpers;
using APSIM.Shared.Utilities;

namespace APSIM.Interop.Documentation
{
    public class PdfOptions
    {
        /// <summary>
        /// Path at which to search for images to be included in the PDF.
        /// </summary>
        /// <value></value>
        public string ImagePath { get; private set; }

        /// <summary>
        /// Custom citation resolver. This will be used to resolve references.
        /// </summary>
        public ICitationHelper CitationResolver { get; private set; }

        /// <summary>
        /// The default citation resolver, which uses APSIM.bib.
        /// </summary>
        private static ICitationHelper defaultCitationResolver;

        /// <summary>
        /// Create a <see cref="PdfOptions"/> instance.
        /// </summary>
        /// <param name="imagePath">Path at which to search for images to be included in the PDF.</param>
        /// <param name="citationHelper">Custom citation resolver.</param>
        public PdfOptions(string imagePath, ICitationHelper citationHelper)
        {
            if (citationHelper == null)
                citationHelper = GetDefaultCitationHelper();

            ImagePath = imagePath;
            CitationResolver = citationHelper;
        }

        public static PdfOptions Default
        {
            get
            {
                return new PdfOptions(null, GetDefaultCitationHelper());
            }
        }

        /// <summary>
        /// Create an instance of the default citation resolver. Note: this will
        /// read in APSIM.bib each time it's called, so this should really be
        /// called once, to initialise <see cref="defaultCitationResolver"/>.
        /// </summary>
        /// <returns></returns>
        private static ICitationHelper GetDefaultCitationHelper()
        {
            if (defaultCitationResolver == null)
                defaultCitationResolver = CreateDefaultCitationHelper();
            return defaultCitationResolver;
        }

        /// <summary>
        /// Create an instance of the default citation resolver. Note: this will
        /// read in APSIM.bib each time it's called, so this should really be
        /// called once, to initialise <see cref="defaultCitationResolver"/>.
        /// </summary>
        /// <returns></returns>
        private static ICitationHelper CreateDefaultCitationHelper()
        {
            string bibFile = PathUtilities.GetAbsolutePath(Path.Combine("%root%", "APSIM.bib"), null);
            return new BibTeX(bibFile);
        }
    }
}
