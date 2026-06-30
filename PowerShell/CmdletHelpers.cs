using System;
using System.IO;
using AcuPower.CustomizationTools.Builder;
using AcuPower.CustomizationTools.Builder.Schema;
using AcuPower.CustomizationTools.Client;
using AcuPower.CustomizationTools.Models;
using AcuPower.CustomizationTools.Package;

namespace AcuPower.CustomizationTools.PowerShell
{
    /// <summary>
    /// Static helper methods designed for PowerShell usage via Add-Type -Path.
    /// Provides synchronous wrappers around the core library functionality.
    /// </summary>
    public static class AcumaticaCst
    {
        /// <summary>
        /// Imports a customization package ZIP and publishes it, polling until completion.
        /// </summary>
        /// <param name="baseUrl">Acumatica instance base URL.</param>
        /// <param name="user">Login username.</param>
        /// <param name="password">Login password.</param>
        /// <param name="projectName">Customization project name.</param>
        /// <param name="packageZipPath">Path to the .zip package file on disk.</param>
        /// <param name="ignoreSsl">Ignore SSL certificate errors (default true).</param>
        /// <returns>Publish result with completion status and log.</returns>
        public static PublishEndResult ImportAndPublish(
            string baseUrl,
            string user,
            string password,
            string projectName,
            string packageZipPath,
            bool ignoreSsl = true,
            string company = null)
        {
            var packageBytes = File.ReadAllBytes(packageZipPath);

            using (var client = new AcumaticaCustomizationClient(baseUrl, ignoreSsl))
            {
                try
                {
                    client.Login(user, password, company);

                    var result = client.ImportAndPublish(
                        projectName: projectName,
                        projectDescription: projectName,
                        projectLevel: 0,
                        packageZipContent: packageBytes,
                        tenantMode: TenantMode.Current,
                        pollIntervalSeconds: 15,
                        timeoutMinutes: 30,
                        isReplaceIfExists: true);

                    return result;
                }
                finally
                {
                    try { client.Logout(); }
                    catch { /* best effort logout */ }
                }
            }
        }

        /// <summary>
        /// Builds a customization package ZIP from a project.xml file and a files directory.
        /// </summary>
        /// <param name="projectXmlPath">Path to the project.xml file.</param>
        /// <param name="filesDirectory">Path to the directory containing associated files. Can be null.</param>
        /// <returns>ZIP package bytes.</returns>
        public static byte[] BuildPackage(string projectXmlPath, string filesDirectory)
        {
            return CustomizationPackageBuilder.BuildFromDisk(projectXmlPath, filesDirectory);
        }

        /// <summary>
        /// Builds a customization package ZIP from an exported customization source folder.
        /// </summary>
        /// <param name="customizationPackageDirectory">Path to the exported customization package directory.</param>
        /// <param name="productVersion">Acumatica product version to stamp into project.xml.</param>
        /// <param name="customizationDllPath">Optional path to the main customization assembly to place under Bin.</param>
        /// <param name="additionalBinFiles">Optional extra files to place under Bin using their file names.</param>
        /// <returns>ZIP package bytes.</returns>
        public static byte[] BuildPackageFromCustomizationSource(
            string customizationPackageDirectory,
            string productVersion = "25.201",
            string customizationDllPath = null,
            string[] additionalBinFiles = null)
        {
            return CustomizationPackageBuilder.BuildFromCustomizationSource(
                customizationPackageDirectory,
                productVersion,
                customizationDllPath,
                additionalBinFiles);
        }

        /// <summary>
        /// Builds a package from an exported customization source folder, imports it, and publishes it.
        /// </summary>
        /// <param name="baseUrl">Acumatica instance base URL.</param>
        /// <param name="user">Login username.</param>
        /// <param name="password">Login password.</param>
        /// <param name="projectName">Customization project name.</param>
        /// <param name="customizationPackageDirectory">Path to the exported customization package directory.</param>
        /// <param name="productVersion">Acumatica product version to stamp into project.xml.</param>
        /// <param name="customizationDllPath">Optional path to the main customization assembly to place under Bin.</param>
        /// <param name="additionalBinFiles">Optional extra files to place under Bin using their file names.</param>
        /// <param name="ignoreSsl">Ignore SSL certificate errors (default true).</param>
        /// <param name="company">Optional tenant name.</param>
        /// <returns>Publish result with completion status and log.</returns>
        public static PublishEndResult ImportAndPublishCustomizationSource(
            string baseUrl,
            string user,
            string password,
            string projectName,
            string customizationPackageDirectory,
            string productVersion = "25.201",
            string customizationDllPath = null,
            string[] additionalBinFiles = null,
            bool ignoreSsl = true,
            string company = null)
        {
            var packageBytes = BuildPackageFromCustomizationSource(
                customizationPackageDirectory,
                productVersion,
                customizationDllPath,
                additionalBinFiles);

            using (var client = new AcumaticaCustomizationClient(baseUrl, ignoreSsl))
            {
                try
                {
                    client.Login(user, password, company);

                    return client.ImportAndPublish(
                        projectName: projectName,
                        projectDescription: projectName,
                        projectLevel: 0,
                        packageZipContent: packageBytes,
                        tenantMode: TenantMode.Current,
                        pollIntervalSeconds: 15,
                        timeoutMinutes: 30,
                        isReplaceIfExists: true);
                }
                finally
                {
                    try { client.Logout(); }
                    catch { /* best effort logout */ }
                }
            }
        }

        /// <summary>
        /// Converts a SQL CREATE TABLE statement to Acumatica table schema XML.
        /// </summary>
        /// <param name="createTableSql">SQL CREATE TABLE DDL statement.</param>
        /// <returns>Acumatica table schema XML string.</returns>
        public static string ConvertSqlToSchema(string createTableSql)
        {
            return TableSchemaBuilder.FromSqlDdl(createTableSql);
        }

        /// <summary>
        /// Creates a simple customization package containing only a DLL file.
        /// </summary>
        /// <param name="description">Project description.</param>
        /// <param name="dllPath">Path to the DLL file on disk.</param>
        /// <param name="productVersion">Acumatica product version (default "25.201").</param>
        /// <returns>ZIP package bytes.</returns>
        public static byte[] CreateDllPackage(string description, string dllPath, string productVersion = "25.201")
        {
            var dllFileName = Path.GetFileName(dllPath);
            var appRelativePath = "Bin\\" + dllFileName;

            var builder = new CustomizationProjectBuilder(description, productVersion);
            builder.AddFile(appRelativePath, dllPath);

            return builder.BuildPackageZip();
        }
    }
}
