using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Linq;
using System.Net;

namespace Apps.OpenAI.Utils;

public static class ErrorHelper
{
    public static Exception ConfigureErrorException(RestResponse response, JsonSerializerSettings jsonSettings)
    {
        if (response.Content == null)
            throw new PluginApplicationException(response.ErrorMessage);

        if (response.ContentType == "text/html")
        {
            string message = ExtractH1HtmlTag(response.Content);
            throw new PluginApplicationException(message);
        }
        var error = JsonConvert.DeserializeObject<ErrorDtoWrapper>(response.Content, jsonSettings);
        if (error?.Error != null)
        {
            if (response.StatusCode == HttpStatusCode.NotFound && error.Error.Type == "invalid_request_error")
                throw new PluginMisconfigurationException("Model chosen is not suitable for this task. Please choose a compatible model.");

            return new PluginApplicationException(error.Error.Message);
        }

        var firstError = ExtractFirstErrorFromMultipleErrors(response);
        if (!string.IsNullOrEmpty(firstError))
            return new PluginApplicationException(firstError);

        if (response.StatusCode == HttpStatusCode.NotFound && error.Error.Type == "invalid_request_error")
            throw new PluginMisconfigurationException("Model chosen is not suitable for this task. Please choose a compatible model.");

        return new PluginApplicationException(error?.Error?.Message ?? response.ErrorException.Message);
    }

    private static string ExtractFirstErrorFromMultipleErrors(RestResponse response)
    {
        var jObj = JObject.Parse(response.Content);
        var errors = jObj["errors"]?["data"]?.FirstOrDefault();
        string message = string.Empty;

        if (errors != null)
        {
            message = errors.Value<string>("message") ?? "Unknown error";
            string code = errors.Value<string>("code");

            if (code == "invalid_deployment_type")
                throw new PluginMisconfigurationException(message);
        }

        return message;
    }

    private static string ExtractH1HtmlTag(string html)
    {
        var startTag = "<h1>";
        var endTag = "</h1>";
        var startIndex = html.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        var endIndex = html.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

        if (startIndex >= 0 && endIndex > startIndex)
        {
            startIndex += startTag.Length;
            return html.Substring(startIndex, endIndex - startIndex).Trim();
        }
        else return "HTML error response received";
    }
}
