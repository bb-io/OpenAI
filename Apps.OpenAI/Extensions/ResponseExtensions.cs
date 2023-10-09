using System;
using OpenAI.ObjectModels.ResponseModels;

namespace Apps.OpenAI.Extensions;

public static class ResponseExtensions
{
    public static void ThrowOnError(this BaseResponse response)
    {
        if (response.Error is null)
            return;
        
        throw new Exception($"{response.Error.Code}: {response.Error.Message}");
    }
}