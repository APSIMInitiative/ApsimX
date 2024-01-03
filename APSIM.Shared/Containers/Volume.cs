using System.IO;

namespace APSIM.Shared.Containers
{
    /// <summary>
    /// Represents a volume which may be mounted into a docker container.
    /// </summary>
    public class Volume
    {
        /// <summary>
        /// Source path of the volume. Should be a directory on local disk.
        /// </summary>
        public string SourcePath { get; private set; }

        /// <summary>
        /// Path at which the volume should be mounted in a container.
        /// </summary>
        public string DestinationPath { get; private set; }

        /// <summary>
        /// Create a new <see cref="Volume"/> instance.
        /// </summary>
        /// <param name="source">Source path of the volume. Should be a directory.</param>
        /// <param name="dest">Path at which the volume should be mounted.</param>
        public Volume(string source, string dest)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Directory '{source}' does not exist.");
            SourcePath = source;
            DestinationPath = dest;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{SourcePath}:{DestinationPath}";
        }
    }
}
