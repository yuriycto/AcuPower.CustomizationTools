using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AcuPower.CustomizationTools.Builder.Items;

namespace AcuPower.CustomizationTools.Package
{
    /// <summary>
    /// Builds the final customization package ZIP file containing project.xml and associated files.
    /// </summary>
    public static class CustomizationPackageBuilder
    {
        /// <summary>
        /// Builds a ZIP package in memory containing project.xml at the root and all associated files.
        /// </summary>
        /// <param name="projectXml">The project.xml content string.</param>
        /// <param name="files">Dictionary of relative file paths to their byte content.</param>
        /// <returns>ZIP file bytes.</returns>
        public static byte[] Build(string projectXml, Dictionary<string, byte[]> files)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    // Add project.xml at the root
                    var projectEntry = archive.CreateEntry("project.xml", CompressionLevel.Optimal);
                    using (var entryStream = projectEntry.Open())
                    {
                        var xmlBytes = Encoding.UTF8.GetBytes(projectXml);
                        entryStream.Write(xmlBytes, 0, xmlBytes.Length);
                    }

                    // Add all binary files
                    if (files != null)
                    {
                        foreach (var kvp in files)
                        {
                            if (kvp.Value == null)
                                continue;

                            // Normalize path separators to forward slashes for ZIP compatibility
                            var entryPath = kvp.Key.Replace('\\', '/');
                            var fileEntry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                            using (var entryStream = fileEntry.Open())
                            {
                                entryStream.Write(kvp.Value, 0, kvp.Value.Length);
                            }
                        }
                    }
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Builds a ZIP package from a project.xml file path and a directory of files.
        /// </summary>
        /// <param name="projectXmlPath">Path to the project.xml file on disk.</param>
        /// <param name="filesDirectory">Path to the directory containing files to include. Can be null.</param>
        /// <returns>ZIP file bytes.</returns>
        public static byte[] BuildFromDisk(string projectXmlPath, string filesDirectory)
        {
            var projectXml = File.ReadAllText(projectXmlPath, Encoding.UTF8);

            var files = new Dictionary<string, byte[]>();

            if (!string.IsNullOrEmpty(filesDirectory) && Directory.Exists(filesDirectory))
            {
                var dirInfo = new DirectoryInfo(filesDirectory);
                var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    // Compute relative path from the files directory
                    var relativePath = file.FullName.Substring(dirInfo.FullName.Length).TrimStart('\\', '/');
                    files[relativePath] = File.ReadAllBytes(file.FullName);
                }
            }

            return Build(projectXml, files);
        }

        /// <summary>
        /// Builds a ZIP package from an exported customization source directory that contains
        /// an <c>_project</c> folder plus associated files such as <c>Pages</c> and <c>SUContent</c>.
        /// </summary>
        /// <param name="customizationPackageDirectory">Path to the exported customization package folder.</param>
        /// <param name="productVersion">Acumatica product version to stamp into project.xml.</param>
        /// <param name="customizationDllPath">Optional path to the main customization assembly to place under Bin.</param>
        /// <param name="additionalBinFiles">Optional extra files to place under Bin using their file names.</param>
        /// <returns>ZIP file bytes.</returns>
        public static byte[] BuildFromCustomizationSource(
            string customizationPackageDirectory,
            string productVersion = "25.201",
            string customizationDllPath = null,
            IEnumerable<string> additionalBinFiles = null)
        {
            if (string.IsNullOrWhiteSpace(customizationPackageDirectory))
                throw new ArgumentException("Customization package directory is required.", nameof(customizationPackageDirectory));

            if (!Directory.Exists(customizationPackageDirectory))
                throw new DirectoryNotFoundException(customizationPackageDirectory);

            var projectDirectory = Path.Combine(customizationPackageDirectory, "_project");
            if (!Directory.Exists(projectDirectory))
                throw new DirectoryNotFoundException(projectDirectory);

            var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            var fileElements = new List<string>();
            var packageRoot = new DirectoryInfo(customizationPackageDirectory);
            var projectRoot = new DirectoryInfo(projectDirectory).FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            foreach (var file in packageRoot.GetFiles("*", SearchOption.AllDirectories))
            {
                if (IsUnderDirectory(file.FullName, projectRoot))
                    continue;

                var relativePath = file.FullName.Substring(packageRoot.FullName.Length).TrimStart('\\', '/');

                fileElements.Add(FileItem.ToXml(ToAppRelativePath(relativePath)));
                files[relativePath] = File.ReadAllBytes(file.FullName);
            }

            AddBinFile(files, fileElements, customizationDllPath);

            if (additionalBinFiles != null)
            {
                foreach (var path in additionalBinFiles.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    AddBinFile(files, fileElements, path);
                }
            }

            var projectXml = BuildProjectXmlFromCustomizationSource(projectDirectory, productVersion, fileElements);
            return Build(projectXml, files);
        }

        private static string BuildProjectXmlFromCustomizationSource(
            string projectDirectory,
            string productVersion,
            IEnumerable<string> fileElements)
        {
            var metadataPath = Path.Combine(projectDirectory, "ProjectMetadata.xml");
            var metadata = GetProjectMetadata(metadataPath);

            var projectItems = Directory.GetFiles(projectDirectory, "*.xml", SearchOption.TopDirectoryOnly)
                .Where(path => !string.Equals(Path.GetFileName(path), "ProjectMetadata.xml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(GetProjectItemPriority)
                .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append("<project");
            sb.Append($" description=\"{EscapeXml(metadata.Description)}\"");
            sb.Append($" productVersion=\"{EscapeXml(productVersion ?? "25.201")}\"");
            if (!string.IsNullOrWhiteSpace(metadata.Level))
            {
                sb.Append($" level=\"{EscapeXml(metadata.Level)}\"");
            }
            sb.AppendLine(">");

            foreach (var element in fileElements)
            {
                sb.Append("  ");
                sb.AppendLine(element);
            }

            foreach (var itemPath in projectItems)
            {
                var xml = File.ReadAllText(itemPath, Encoding.UTF8).Trim();
                if (string.IsNullOrWhiteSpace(xml))
                    continue;

                sb.Append("  ");
                sb.AppendLine(xml);
            }

            sb.AppendLine("</project>");
            return sb.ToString();
        }

        private static ProjectMetadata GetProjectMetadata(string metadataPath)
        {
            if (!File.Exists(metadataPath))
                return new ProjectMetadata("Customization", null);

            var document = XDocument.Load(metadataPath);
            var projectElement = document.Root;
            var description = projectElement?.Attribute("description")?.Value;
            if (string.IsNullOrWhiteSpace(description))
            {
                description = projectElement?.Attribute("name")?.Value;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                description = "Customization";
            }

            var level = projectElement?.Attribute("level")?.Value;
            return new ProjectMetadata(description, string.IsNullOrWhiteSpace(level) ? null : level);
        }

        private readonly struct ProjectMetadata
        {
            public ProjectMetadata(string description, string level)
            {
                Description = description;
                Level = level;
            }

            public string Description { get; }
            public string Level { get; }
        }

        private static int GetProjectItemPriority(string itemPath)
        {
            try
            {
                var rootName = XDocument.Load(itemPath).Root?.Name.LocalName ?? string.Empty;
                switch (rootName)
                {
                    case "File": return 0;
                    case "Page": return 1;
                    case "Screen": return 2;
                    case "PerTenantFile": return 3;
                    case "Sql": return 4;
                    case "SiteMapNode": return 5;
                    case "ScreenWithRights": return 6;
                    case "Webhook": return 7;
                    case "Report": return 8;
                    case "Dashboard": return 9;
                    case "Wiki": return 10;
                    case "GenericInquiry": return 11;
                    case "GenericInquiryScreen": return 11;
                    case "BpEvent": return 12;
                    case "MobileSiteMap": return 13;
                    case "XportScenario": return 14;
                    default: return 100;
                }
            }
            catch
            {
                return 100;
            }
        }

        private static bool IsUnderDirectory(string filePath, string directoryPath)
        {
            var normalizedFilePath = Path.GetFullPath(filePath);
            var normalizedDirectoryPath = Path.GetFullPath(directoryPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            return normalizedFilePath.StartsWith(normalizedDirectoryPath, System.StringComparison.OrdinalIgnoreCase);
        }

        private static void AddBinFile(Dictionary<string, byte[]> files, List<string> fileElements, string diskPath)
        {
            if (string.IsNullOrWhiteSpace(diskPath))
                return;

            if (!File.Exists(diskPath))
                throw new FileNotFoundException("Unable to locate customization package file.", diskPath);

            var relativePath = Path.Combine("Bin", Path.GetFileName(diskPath));
            fileElements.Add(FileItem.ToXml(relativePath));
            files[relativePath] = File.ReadAllBytes(diskPath);
        }

        private static string ToAppRelativePath(string relativePath)
        {
            return relativePath.Replace('/', '\\');
        }

        private static string EscapeXml(string value)
        {
            if (value == null) return string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
