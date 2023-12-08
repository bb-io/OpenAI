using System.Collections.Generic;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Responses;

namespace Apps.OpenAI.Utils;

public static class BlackbirdPromptParser
{
    public static (List<ChatMessageDto>, BlackbirdPromptAdditionalInfo? info) ParseBlackbirdPrompt(string inputPrompt)
    {
        var promptSegments = inputPrompt.Split(";;");

        if (promptSegments.Length is 1)
            return (new() { new(MessageRoles.User, promptSegments[0]) });

        if (promptSegments.Length is 2)
            return (new()
            {
                new(MessageRoles.System, promptSegments[0]),
                new(MessageRoles.User, promptSegments[1])
            });

        if (promptSegments.Length is 3)
            return (new()
            {
                new(MessageRoles.System, promptSegments[0]),
                new(MessageRoles.User, promptSegments[1])
            }, new BlackbirdPromptAdditionalInfo()
            {
                FileFormat = promptSegments[2]
            });

        throw new("Wrong blackbird prompt format");
    }

    public static string ParseFileFormat(string fileFormat)
    {
        return fileFormat switch
        {
            "Json" => "json_object",
            _ => throw new("Wrong response file format")
        };
    }
}