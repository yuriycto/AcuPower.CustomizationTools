using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AcuPower.CustomizationTools.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AcuPower.CustomizationTools.Client
{
    /// <summary>
    /// REST client for Acumatica's CustomizationApi endpoints.
    /// Uses cookie-based session management with HttpClient.
    /// </summary>
    public class AcumaticaCustomizationClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        /// <summary>
        /// Creates a new client for the given Acumatica instance URL.
        /// </summary>
        /// <param name="baseUrl">Base URL of the Acumatica instance (e.g. https://mysite.acumatica.com/MyInstance)</param>
        /// <param name="ignoreSslErrors">If true, all SSL certificate errors are ignored.</param>
        public AcumaticaCustomizationClient(string baseUrl, bool ignoreSslErrors = false)
        {
            _baseUrl = baseUrl.TrimEnd('/');

            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };

            if (ignoreSslErrors)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(30)
            };
        }

        #region Authentication

        /// <summary>
        /// Logs in to Acumatica using the provided credentials.
        /// </summary>
        /// <param name="username">Login username.</param>
        /// <param name="password">Login password.</param>
        /// <param name="company">Company/tenant name. Required for multi-tenant instances. Null for single-tenant.</param>
        public async Task LoginAsync(string username, string password, string company = null)
        {
            object payload = company != null
                ? (object)new { name = username, password = password, company = company }
                : (object)new { name = username, password = password };
            var response = await PostJsonAsync("/entity/auth/login", payload).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Logs in to Acumatica (synchronous).
        /// </summary>
        public void Login(string username, string password, string company = null)
        {
            LoginAsync(username, password, company).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Logs out from Acumatica.
        /// </summary>
        public async Task LogoutAsync()
        {
            var response = await PostJsonAsync("/entity/auth/logout", new { }).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Logs out from Acumatica (synchronous).
        /// </summary>
        public void Logout()
        {
            LogoutAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region Customization API

        /// <summary>
        /// Imports a customization project package.
        /// </summary>
        public async Task<ImportResult> ImportAsync(
            string projectName,
            string projectDescription,
            int projectLevel,
            byte[] packageZipContent,
            bool isReplaceIfExists = true)
        {
            var payload = new
            {
                ProjectName = projectName,
                ProjectDescription = projectDescription,
                ProjectLevel = projectLevel,
                ProjectContentBase64 = Convert.ToBase64String(packageZipContent),
                IsReplaceIfExists = isReplaceIfExists
            };

            var response = await PostJsonAsync("/CustomizationApi/Import", payload).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ImportResult>(json);
        }

        /// <summary>
        /// Imports a customization project package (synchronous).
        /// </summary>
        public ImportResult Import(
            string projectName,
            string projectDescription,
            int projectLevel,
            byte[] packageZipContent,
            bool isReplaceIfExists = true)
        {
            return ImportAsync(projectName, projectDescription, projectLevel, packageZipContent, isReplaceIfExists)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Begins publishing the specified customization projects.
        /// </summary>
        public async Task PublishBeginAsync(
            string[] projectNames,
            TenantMode tenantMode = TenantMode.Current,
            bool isOnlyValidation = false)
        {
            var payload = new
            {
                ProjectNames = projectNames,
                TenantMode = (int)tenantMode,
                IsOnlyValidation = isOnlyValidation
            };

            var response = await PostJsonAsync("/CustomizationApi/PublishBegin", payload).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Begins publishing (synchronous).
        /// </summary>
        public void PublishBegin(
            string[] projectNames,
            TenantMode tenantMode = TenantMode.Current,
            bool isOnlyValidation = false)
        {
            PublishBeginAsync(projectNames, tenantMode, isOnlyValidation).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Polls the publish status. Returns result with IsCompleted/IsFailed.
        /// </summary>
        public async Task<PublishEndResult> PublishEndAsync()
        {
            var response = await PostJsonAsync("/CustomizationApi/PublishEnd", new { }).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<PublishEndResult>(json);
        }

        /// <summary>
        /// Polls the publish status (synchronous).
        /// </summary>
        public PublishEndResult PublishEnd()
        {
            return PublishEndAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the project content as a base64-encoded ZIP.
        /// </summary>
        public async Task<byte[]> GetProjectAsync(string projectName)
        {
            var payload = new { ProjectName = projectName };
            var response = await PostJsonAsync("/CustomizationApi/GetProject", payload).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JObject.Parse(json);
            var base64 = result["ProjectContentBase64"]?.ToString();
            if (string.IsNullOrEmpty(base64))
                return new byte[0];
            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// Gets the project content (synchronous).
        /// </summary>
        public byte[] GetProject(string projectName)
        {
            return GetProjectAsync(projectName).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes a customization project.
        /// </summary>
        public async Task DeleteAsync(string projectName)
        {
            var payload = new { ProjectName = projectName };
            var response = await PostJsonAsync("/CustomizationApi/Delete", payload).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Deletes a customization project (synchronous).
        /// </summary>
        public void Delete(string projectName)
        {
            DeleteAsync(projectName).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Unpublishes all customization projects.
        /// </summary>
        public async Task UnpublishAllAsync(TenantMode tenantMode = TenantMode.Current)
        {
            var payload = new { TenantMode = (int)tenantMode };
            var response = await PostJsonAsync("/CustomizationApi/UnpublishAll", payload).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Unpublishes all customization projects (synchronous).
        /// </summary>
        public void UnpublishAll(TenantMode tenantMode = TenantMode.Current)
        {
            UnpublishAllAsync(tenantMode).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the list of currently published projects.
        /// </summary>
        public async Task<PublishedInfo> GetPublishedAsync()
        {
            var response = await PostJsonAsync("/CustomizationApi/GetPublished", new { }).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<PublishedInfo>(json);
        }

        /// <summary>
        /// Gets the list of currently published projects (synchronous).
        /// </summary>
        public PublishedInfo GetPublished()
        {
            return GetPublishedAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region High-Level Operations

        /// <summary>
        /// Imports a customization package, publishes it, and polls until publishing completes.
        /// </summary>
        /// <param name="projectName">Name of the customization project.</param>
        /// <param name="projectDescription">Description for the project.</param>
        /// <param name="projectLevel">Level of the project (ordering priority).</param>
        /// <param name="packageZipContent">ZIP package bytes.</param>
        /// <param name="tenantMode">Tenant mode for publishing.</param>
        /// <param name="pollIntervalSeconds">Seconds between publish status polls (default 15).</param>
        /// <param name="timeoutMinutes">Maximum minutes to wait for publish (default 30).</param>
        /// <param name="isReplaceIfExists">Replace existing project if it exists.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Final publish result.</returns>
        public async Task<PublishEndResult> ImportAndPublishAsync(
            string projectName,
            string projectDescription,
            int projectLevel,
            byte[] packageZipContent,
            TenantMode tenantMode = TenantMode.Current,
            int pollIntervalSeconds = 15,
            int timeoutMinutes = 30,
            bool isReplaceIfExists = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Import
            var importResult = await ImportAsync(projectName, projectDescription, projectLevel, packageZipContent, isReplaceIfExists)
                .ConfigureAwait(false);

            if (importResult != null && importResult.IsError)
            {
                return new PublishEndResult
                {
                    IsCompleted = false,
                    IsFailed = true,
                    Log = importResult.Log ?? new List<LogEntry>()
                };
            }

            // Publish
            await PublishBeginAsync(new[] { projectName }, tenantMode).ConfigureAwait(false);

            // Poll until done — with retry logic for app pool restarts during publish
            var deadline = DateTime.UtcNow.AddMinutes(timeoutMinutes);
            int consecutiveErrors = 0;
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken).ConfigureAwait(false);

                try
                {
                    var result = await PublishEndAsync().ConfigureAwait(false);
                    consecutiveErrors = 0;
                    if (result.IsCompleted || result.IsFailed)
                    {
                        return result;
                    }
                }
                catch (HttpRequestException)
                {
                    consecutiveErrors++;
                    if (consecutiveErrors > 10)
                        throw;
                    // App pool restarted during publish — wait longer and retry
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                }
            }

            // Timeout
            return new PublishEndResult
            {
                IsCompleted = false,
                IsFailed = true,
                Log = new List<LogEntry>
                {
                    new LogEntry
                    {
                        Timestamp = DateTime.UtcNow.ToString("o"),
                        LogType = "Error",
                        Message = $"Publishing timed out after {timeoutMinutes} minutes."
                    }
                }
            };
        }

        /// <summary>
        /// Imports and publishes (synchronous).
        /// </summary>
        public PublishEndResult ImportAndPublish(
            string projectName,
            string projectDescription,
            int projectLevel,
            byte[] packageZipContent,
            TenantMode tenantMode = TenantMode.Current,
            int pollIntervalSeconds = 15,
            int timeoutMinutes = 30,
            bool isReplaceIfExists = true)
        {
            return ImportAndPublishAsync(
                projectName, projectDescription, projectLevel, packageZipContent,
                tenantMode, pollIntervalSeconds, timeoutMinutes, isReplaceIfExists)
                .GetAwaiter().GetResult();
        }

        #endregion

        #region Private Helpers

        private async Task<HttpResponseMessage> PostJsonAsync(string relativeUrl, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = _baseUrl + relativeUrl;
            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string body = string.Empty;
                try { body = await response.Content.ReadAsStringAsync().ConfigureAwait(false); }
                catch { /* ignore */ }
                if (body != null && body.Length > 3000) body = body.Substring(0, 3000) + "...";
                throw new HttpRequestException(
                    $"POST {relativeUrl} -> {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }
            return response;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
