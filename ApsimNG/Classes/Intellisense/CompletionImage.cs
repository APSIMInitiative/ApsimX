using System;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem;
using System.IO;
using System.Reflection;
using Gdk;

namespace UserInterface.Intellisense
{
    /// <summary>
	/// Provides icons for code completion.
	/// </summary>
	public class CompletionImage
    {
        /// <summary>
        /// Name of the image with file extension.
        /// </summary>
        private readonly string imageName;

        /// <summary>
        /// Path to the directory containing the completion images.
        /// </summary>
        private const string completionImagesPath = "ApsimNG.Resources.CompletionImages.";

        /// <summary>
        /// Whether or not the static overlay should be shown for static members.
        /// </summary>
        private readonly bool showStaticOverlay;

        /// <summary>
        /// Number of available overlays.
        /// </summary>
        private const int accessibilityOverlaysLength = 5;

        /// <summary>
        /// List of all possible overlay images.
        /// There is an overlay for each combination of accessibility and 'staticness'.
        /// e.g. private, private static, protected, protected static, etc.
        /// 0..N-1  = base image + accessibility overlay.
        /// N..2N-1 = base image + static overlay + accessibility overlay.
        /// </summary>
        private Pixbuf[] images = new Pixbuf[2 * accessibilityOverlaysLength];

        /// <summary>
        /// Accessibility overlays.
        /// These are images which are superimposed on the completion image. 
        /// e.g. a private method's image is a padlock on the corner of the normal method image.
        /// </summary>
        private static readonly Pixbuf[] AccessibilityOverlays = new Pixbuf[accessibilityOverlaysLength]
        {
            null,
            LoadPixbuf("OverlayPrivate"),
            LoadPixbuf("OverlayProtected"),
            LoadPixbuf("OverlayInternal"),
            LoadPixbuf("OverlayProtectedInternal")
        };

        /// <summary>
        /// Loads a pixbuf from the executing assembly.
        /// </summary>
        /// <param name="name">Name of the image with or without file extension.</param>
        /// <returns>Pixbuf containing the image.</returns>
        public static Pixbuf LoadPixbuf(string name)
        {
            if (!Path.HasExtension(name))
                name = Path.ChangeExtension(name, ".png");
            return new Pixbuf(Assembly.GetExecutingAssembly(), completionImagesPath + name);
        }

        #region Entity Images

        // These static properties take the place of a public constructor. They are a bit unsightly, but 
        // they don't rely on the developer passing the image name as a string into the constructor.

        /// <summary>Gets the image used for namespaces.</summary>
        public static CompletionImage Namespace { get { return new CompletionImage("NameSpace", false); } }

        /// <summary>Gets the image used for non-static classes.</summary>
        public static CompletionImage Class { get { return new CompletionImage("Class", false); } }

        /// <summary>Gets the image used for structs.</summary>
        public static CompletionImage Struct { get { return new CompletionImage("Struct", false); } }

        /// <summary>Gets the image used for interfaces.</summary>
        public static CompletionImage Interface { get { return new CompletionImage("Interface", false); } }

        /// <summary>Gets the image used for delegates.</summary>
        public static CompletionImage Delegate { get { return new CompletionImage("Delegate", false); } }

        /// <summary>Gets the image used for enums.</summary>
        public static CompletionImage Enum { get { return new CompletionImage("Enum", false); } }

        /// <summary>Gets the image used for modules/static classes.</summary>
        public static CompletionImage StaticClass { get { return new CompletionImage("StaticClass", false); } }

        /// <summary>Gets the image used for non-static classes.</summary>
        public static CompletionImage Field { get { return new CompletionImage("Field", true); } }

        /// <summary>Gets the image used for structs.</summary>
        public static CompletionImage ReadOnlyField { get { return new CompletionImage("FieldReadOnly", true); } }

        /// <summary>Gets the image used for constants.</summary>
        public static CompletionImage Literal { get { return new CompletionImage("Literal", false); } }

        /// <summary>Gets the image used for enum values.</summary>
        public static CompletionImage EnumValue { get { return new CompletionImage("EnumValue", false); } }

        /// <summary>Gets the image used for methods.</summary>
        public static CompletionImage Method { get { return new CompletionImage("Method", true); } }

        /// <summary>Gets the image used for constructos.</summary>
        public static CompletionImage Constructor { get { return new CompletionImage("Constructor", true); } }

        /// <summary>Gets the image used for virtual methods.</summary>
        public static CompletionImage VirtualMethod { get { return new CompletionImage("VirtualMethod", true); } }

        /// <summary>Gets the image used for operators.</summary>
        public static CompletionImage Operator { get { return new CompletionImage("Operator", false); } }

        /// <summary>Gets the image used for extension methods.</summary>
        public static CompletionImage ExtensionMethod { get { return new CompletionImage("ExtensionMethod", true); } }

        /// <summary>Gets the image used for P/Invoke methods.</summary>
        public static CompletionImage PInvokeMethod { get { return new CompletionImage("PInvokeMethod", true); } }

        /// <summary>Gets the image used for properties.</summary>
        public static CompletionImage Property { get { return new CompletionImage("Property", true); } }

        /// <summary>Gets the image used for indexers.</summary>
        public static CompletionImage Indexer { get { return new CompletionImage("Indexer", true); } }

        /// <summary>Gets the image used for events.</summary>
        public static CompletionImage Event { get { return new CompletionImage("Event", true); } }

        #endregion

        /// <summary>
        /// Gets the image for the specified entity.
        /// Returns null when no image is available for the entity type.
        /// </summary>
        public static Pixbuf GetImage(IEntity entity)
        {
            CompletionImage image = GetCompletionImage(entity);
            if (image != null)
                return image.GetImage(entity.Accessibility, entity.IsStatic);
            else
                return null;
        }

        /// <summary>
        /// Gets the image without any overlays.
        /// </summary>
        public Pixbuf BaseImage
        {
            get
            {
                Pixbuf image = images[0];
                if (image == null)
                {
                    image = LoadPixbuf(imageName);
                    Thread.MemoryBarrier();
                    images[0] = image;
                }
                return image;
            }
        }

        /// <summary>
        /// Gets the completion image for the specified type of type.
        /// Returns null if no image is available for the type.
        /// </summary>
        /// <param name="typeKind"></param>
        /// <param name="isStatic"></param>
        /// <returns></returns>
        private static CompletionImage GetCompletionImageForType(TypeKind typeKind, bool isStatic)
        {
            switch (typeKind)
            {
                case TypeKind.Interface:
                    return Interface;
                case TypeKind.Struct:
                case TypeKind.Void:
                    return Struct;
                case TypeKind.Delegate:
                    return Delegate;
                case TypeKind.Enum:
                    return Enum;
                case TypeKind.Class:
                    return isStatic ? StaticClass : Class;
                case TypeKind.Module:
                    return StaticClass;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the CompletionImage instance for the specified entity.
        /// Returns null when no image is available for the entity type.
        /// </summary>
        private static CompletionImage GetCompletionImage(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity cannot be null.");
            switch (entity.SymbolKind)
            {
                case SymbolKind.TypeDefinition:
                    return GetCompletionImageForType(((ITypeDefinition)entity).Kind, entity.IsStatic);
                case SymbolKind.Field:
                    IField field = (IField)entity;
                    if (field.IsConst)
                    {
                        if (field.DeclaringTypeDefinition != null && field.DeclaringTypeDefinition.Kind == TypeKind.Enum)
                            return EnumValue;
                        else
                            return Literal;
                    }
                    return field.IsReadOnly ? ReadOnlyField : Field;
                case SymbolKind.Method:
                    IMethod method = (IMethod)entity;
                    if (method.IsExtensionMethod)
                        return ExtensionMethod;
                    else
                        return method.IsOverridable ? VirtualMethod : Method;
                case SymbolKind.Property:
                    return Property;
                case SymbolKind.Indexer:
                    return Indexer;
                case SymbolKind.Event:
                    return Event;
                case SymbolKind.Operator:
                case SymbolKind.Destructor:
                    return Operator;
                case SymbolKind.Constructor:
                    return Constructor;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the index of the overlay image to use for a member with a given accessibility.
        /// </summary>
        /// <param name="accessibility">Accessibility of the member.</param>
        /// <returns>int in the range 0..<see cref="accessibilityOverlaysLength"/> - 1.</returns>
        private static int GetAccessibilityOverlayIndex(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Private:
                    return 1;
                case Accessibility.Protected:
                    return 2;
                case Accessibility.Internal:
                    return 3;
                case Accessibility.ProtectedOrInternal:
                case Accessibility.ProtectedAndInternal:
                    return 4;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets this image combined with the specified accessibility overlay.
        /// </summary>
        private Pixbuf GetImage(Accessibility accessibility, bool isStatic = false)
        {
            int accessibilityIndex = GetAccessibilityOverlayIndex(accessibility);
            int index = accessibilityIndex;
            if (isStatic && showStaticOverlay)
                index += accessibilityOverlaysLength;

            if (index == 0)
                return this.BaseImage;

            if (images[index] == null)
            {
                // This icon/overlay combination has not been used previously, so we need to generate it.
                Pixbuf icon = LoadPixbuf(imageName);
                Pixbuf overlay = AccessibilityOverlays[accessibilityIndex];
                if (isStatic && showStaticOverlay)
                    overlay = OverlayImages(LoadPixbuf("OverlayStatic"), overlay);

                // Create the composite image - the icon with the superimposed overlay
                Pixbuf composite = OverlayImages(icon, overlay);

                images[index] = composite;
            }

            return images[index];
        }

        /// <summary>
        /// Superimposes an image on top of another image.
        /// </summary>
        /// <param name="icon1">Image to go on the bottom layer.</param>
        /// <param name="icon2">Image to go on the top layer.</param>
        /// <returns>icon2 superimposed on top of icon1.</returns>
        private Pixbuf OverlayImages(Pixbuf icon1, Pixbuf icon2)
        {
            if (icon1 == null)
                return icon2;
            if (icon2 == null)
                return icon1;

            Pixbuf composite = new Pixbuf(icon1.Colorspace, icon1.HasAlpha, icon1.BitsPerSample, 16, 16);
            icon1.Composite(composite, 0, 0, 16, 16, 0, 0, 1, 1, InterpType.Hyper, 255);
            icon2.Composite(composite, 0, 0, 16, 16, 0, 0, 1, 1, InterpType.Hyper, 255);

            return composite;
        }

        /// <summary>
        /// Cosntructor.
        /// </summary>
        /// <param name="imageName">Image name to use.</param>
        /// <param name="showStaticOverlay">Whether or not the static overlay should be shown for static members.</param>
        private CompletionImage(string imageName, bool showStaticOverlay)
        {
            this.imageName = imageName + ".png";
            this.showStaticOverlay = showStaticOverlay;
        }
    }
}
