using Blackbird.Applications.Sdk.Common.Exceptions;
using System;
using System.Threading.Tasks;

namespace Apps.OpenAI.Utils
{
    public static class ErrorHandler
    {
        public static async Task ExecuteWithErrorHandlingAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException(ex.Message);
            }
        }

        public static async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException(ex.Message);
            }
        }

        public static T ExecuteWithErrorHandling<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException(ex.Message);
            }
        }
    }
}
