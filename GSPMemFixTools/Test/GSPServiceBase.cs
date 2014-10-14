using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Newtonsoft.Json;
using Securitas.GSP.RiaClient.Contracts;
using Securitas.GSP.RiaClient.Framework.Helpers;
using Securitas.GSP.RiaClient.Framework.Services;
using Securitas.GSP.RiaClient.Model;
using Securitas.GSP.RiaClient.Services.Serialization;

using Localization = Securitas.GSP.RiaClient.UIResources;
using Securitas.GSP.Core.BookingEngine.Rules;
using Securitas.GSP.Core.BookingEngine.OperationResponses;
using System.Threading;
using Securitas.GSP.Core.Common;
using System.Windows.Browser;

using Securitas.GSP.Core.Common.Extensions; // For HttpWebRespone extension methods

namespace Securitas.GSP.RiaClient.Services
{
    public enum WebMethod
    {
        Get = 0,
        Post = 1,
        Delete = 2,
        Put = 3,
        Head = 4,
        Options = 5,
    }

    public class GSPServiceBase
    {
        [Import]
        public IApplicationState AppState { get; set; }

        private readonly JsonDotNetSerializer _json;

        public GSPServiceBase()
        {
            JsonSerializerSettings _jsonSettings = new JsonSerializerSettings();
            _jsonSettings.Converters.Add(new JsonNETDateTimeConverter());
            _jsonSettings.Converters.Add(new JsonNetIServiceResultConverter());
            _jsonSettings.Converters.Add(new JsonBulkCreationConverter<Rule>());
            _jsonSettings.Converters.Add(new JsonBulkCreationConverter<RuleResult>());
            _jsonSettings.Converters.Add(new JsonBulkCreationConverter<JobDefinitionSchedule>());
            _jsonSettings.Converters.Add(new JsonBulkCreationConverter<BookingOperationResponse>());

            _jsonSettings.NullValueHandling = NullValueHandling.Ignore;

            _json = new JsonDotNetSerializer(_jsonSettings);
        }

        // TODO - Handle retries?

        private HttpWebRequest PrepareRequest(string path, WebMethod? method = WebMethod.Get)
        {
            var fullPath = GSPApplicationService.Current.ServerPath + path;
            var request = (HttpWebRequest)WebRequest.Create(new Uri(fullPath, UriKind.Absolute));
            request.Method = method.ToString();
            return request;
        }


        private static void ResolveEnumerableUrlSegments(IList<object> segments, int i)
        {
            var collection = (IEnumerable<object>)segments[i];
            var total = collection.Count();
            var sb = new StringBuilder();
            var count = 0;
            foreach (var item in collection)
            {
                sb.Append(item.ToString());
                if (count < total - 1)
                {
                    sb.Append(",");
                }
                count++;
            }
            segments[i] = sb.ToString();
        }

        private string ResolveUrlSegments(string path, List<object> segments)
        {
            if (segments == null) throw new ArgumentNullException("segments");


            for (var i = 0; i < segments.Count; i++)
            {
                if (typeof(IEnumerable).IsAssignableFrom(segments[i].GetType()) && !(segments[i].GetType() == typeof(string)))
                {
                    ResolveEnumerableUrlSegments(segments, i);
                }
            }

            path = PathHelpers.ReplaceUriTemplateTokens(segments, path);

            PathHelpers.EscapeDataContainingUrlSegments(segments);


            segments.Insert(0, path);

            return string.Concat(segments.ToArray()).ToString(CultureInfo.InvariantCulture);
        }

        protected IAsyncResult MakeRequestWithParams<T>(Action<T, IGSPServiceResponse> action, string path, bool lockClient, params object[] segments) where T : class
        {
            return MakeRequest(action, ResolveUrlSegments(path, segments.ToList()), lockClient);
        }

        protected IAsyncResult MakeRequestWithWebMethodAndParams<T>(WebMethod method, Action<T, IGSPServiceResponse> action, string path, bool lockClient, params object[] segments) where T : class
        {
            return MakeRequest(method, action, ResolveUrlSegments(path, segments.ToList()), lockClient);
        }

        protected IAsyncResult MakeRequest<T>(Action<T, IGSPServiceResponse> action, string path, bool lockClient) where T : class
        {
            var request = PrepareRequest(path);
            return MakeRequestImpl(request, action, lockClient);
        }


        protected IAsyncResult MakeRequest<T>(WebMethod method, Action<T, IGSPServiceResponse> action, string path, bool lockClient) where T : class
        {
            var request = PrepareRequest(path, method);            
            return MakeRequestImpl(request, action, lockClient);
        }

        protected IAsyncResult MakeRequest<T>(Object entity, WebMethod method, Action<T, IGSPServiceResponse> action, string path, bool lockClient) where T : class
        {
            // In DEBUG we want to make sure that the endpoints path does not end with a forward slash.
            // This is a reacurring typo from our part.
            Debug.Assert(!path.EndsWith("/"), string.Format("REST endpoint path \"{0}\"should not end with \"/\"", path));
            var request = PrepareRequest(path, method);              
            return MakeRequestImpl(request, action, lockClient, entity);
        }
        
        protected IAsyncResult MakeRequestImpl<T>(HttpWebRequest request, Action<T, IGSPServiceResponse> action, bool lockClient, Object entity = null) where T : class
        {
            IAsyncResult result = null;
            if (lockClient)
                GSPApplicationService.Current.AppState.EnqueueBusy();

            if (entity != null)
            {
                request.ContentType = "application/json";
                request.BeginGetRequestStream(ar =>
                {
                    try
                    {
                        var webRequest = ar.AsyncState as HttpWebRequest;
                        var stream = webRequest.EndGetRequestStream(ar);
                        var data = _json.Serialize(entity, typeof(T));
                        var writer = new StreamWriter(stream);
                        writer.Write(data);
                        writer.Flush();
                        writer.Close();
                        result = MakeServerCall(webRequest, action, lockClient);
                    }
                    catch (Exception ex) 
                    {
                        throw new Exception("Exception in anonymous callback: GSPServiceBase.MakeRequestImpl<T>.BeginGetRequestStream(X). See inner exception for details.", ex);
                    }
                }, request);
            }
            else
            {
                result = MakeServerCall(request, action, lockClient);
            }
            return result;
        }

        private IAsyncResult MakeServerCall<T>(HttpWebRequest request, Action<T, IGSPServiceResponse> action, bool lockClient, bool noErrorLog = false)
        {
            var requestUri = request.RequestUri.ToString();
            
            return request.BeginGetResponse(result =>
            {
                try
                {
                    var req = result.AsyncState as HttpWebRequest;
                    if (req == null)
                    {
                        return;
                    }

                    var response = req.EndGetResponseNoException(result) as HttpWebResponse;
                    
                    //Get the json payload from the response
                    string json = "";
                    Stream responseStream = response.GetResponseStream();
                    if (responseStream != null)
                    {
                        using (var sr = new StreamReader(responseStream))
                            json = sr.ReadToEnd();
                    }

                    //Create a GSPServiceResponse object
                    var serviceResponse = new GSPServiceResponse(response, json);

                    // Show error dialog if error occured and no parameter is supplied (parameter-based errors is handled in UI context)
                    if (noErrorLog && serviceResponse.HasErrors)
                    {
                        //Failed to log to server. This could mean that we have a network failure. 
                        //TODO: Try to log to isolatedstorage and then log back to server when network is available again
                        //TODO::Mathias
                        Debug.Assert(false, "Failed to log to server");
                    }
                    else if (serviceResponse.HasErrors && string.IsNullOrEmpty(serviceResponse.ApiErrorCode) && string.IsNullOrEmpty(serviceResponse.ApiErrorParamName))
                    {
                        result = LogToServer<T>(action, lockClient, result, serviceResponse.InnerException, requestUri);

                        if (lockClient)
                            ThreadHelper.ExecuteOnUI(() =>
                                {
                                    try
                                    {
                                        ShowDialog(Localization.Resources.ServiceErrorDialog_Title, serviceResponse.ErrorMessage, serviceResponse.StatusDescription, false, null);
                                        //Dialog.ShowDialog(Localization.Resources.ServiceErrorDialog_Title, serviceResponse.ErrorMessage, serviceResponse.StatusDescription, false, null);
                                    }
                                    catch (Exception innerEx)
                                    {
                                        throw new Exception("Exception in anonymous callback #1, requestUrl: " + requestUri, innerEx);
                                    }
                                    
                                });

                        return;
                    }
                    if (serviceResponse.HasErrors)
                        action.Invoke(default(T), serviceResponse);
                    else
                        action.Invoke(_json.DeserializeJson<T>(json), serviceResponse);
                }
                catch (Exception ex)
                {
                    ThreadHelper.ExecuteOnUI(() => 
                    {
                        try
                        {
                            var innerEx = ex.InnerException as WebException;
                            string localUri = string.Empty;
                            if (innerEx != null)
                            {
                                localUri = innerEx.Response.ResponseUri.LocalPath;
                            }
                            string finalErrorMessage = string.Empty;

                            if (!string.IsNullOrEmpty(localUri))
                                finalErrorMessage += localUri + "\n\n";

                            finalErrorMessage += ex.ToString();
                            ShowDialog(Localization.Resources.ServiceErrorDialog_Title, "Error when calling server API",finalErrorMessage, false, null);
                        }
                        catch (Exception innerEx)
                        {
                            throw new Exception("Exception in anonymous callback #2, requestUrl: " + requestUri, innerEx);
                        }
                        finally 
                        {
                            if (noErrorLog == false)
                                LogToServer<T>(action, lockClient, result, ex, requestUri);
                        }
                    });
                    //System.Diagnostics.Debug.Assert(false);
                }
                finally
                {
                    if (lockClient)
                        ThreadHelper.ExecuteOnUI(() => GSPApplicationService.Current.AppState.DequeueBusy());
                }
            }, request);
        }

        private void ShowDialog(string title, string message, string collapsedText, bool allowCancel, Action<bool> response)
        //private void ShowDialog(string title, string message, bool allowCancel, Action<bool> response)
        {
            var dialog = new ErrorDialog
                {
                    Title = title,
                    Message = message,
                    Details = collapsedText,
                    ShowMoreDetailsButton = !string.IsNullOrEmpty(collapsedText),
                    CloseAction = response ?? (r => { }),
                    ShowCancelButton = allowCancel,
                    OkButtonText = Localization.Resources.Global_OK,
                    CancelButtonText = Localization.Resources.Global_Cancel
                };

            EventHandler handleClose = null;            
            handleClose = (sender, args) =>
            {
                dialog.Closed -= handleClose;
                var dialogClosed = sender as ErrorDialog;
                if (dialogClosed == null) return;                
                dialogClosed.CloseAction(dialog.DialogResult == true);
            };
            dialog.Closed += handleClose;
            dialog.Show();
        }

        private IAsyncResult LogToServer<T>(Action<T, IGSPServiceResponse> action, bool lockClient, IAsyncResult result, Exception ex, string customMessage = null)
        {
            var noEx = "No exception (null) was passed in to LogToServer method.";
            var exMessage = ex != null ? ex.Message : noEx;
            var exToString = ex != null ? ex.ToString() : noEx;

            if (string.IsNullOrEmpty(customMessage) == false) 
            {
                exMessage = string.Format("{0} | Custom info: [{1}]", exMessage, customMessage);
            }

            var logRequest = PrepareRequest("/api/log", WebMethod.Post);
            var logObject = new Model.Log
            {
                Date = DateTime.Now,
                Message = exMessage,
                Exception = exToString,
                Level = "ERROR",
                Logger = "RiaClient",
                Thread = Thread.CurrentThread.ManagedThreadId.ToString()
            };
            
            logRequest.ContentType = "application/json";
            logRequest.BeginGetRequestStream(ar =>
            {
                var webRequest = ar.AsyncState as HttpWebRequest;
                var stream = webRequest.EndGetRequestStream(ar);
                var data = _json.Serialize(logObject, typeof(T));
                var writer = new StreamWriter(stream);
                writer.Write(data);
                writer.Flush();
                writer.Close();
                result = MakeServerCall(webRequest, action, lockClient, true);
            }, logRequest);

            return result;
        }
    }
}
