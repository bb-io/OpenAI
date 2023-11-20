using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.OpenAI.Models.Responses.Audio;

public record CreateSpeechResponse(File Audio);