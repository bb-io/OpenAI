using Apps.OpenAI.Models.PostEdit;

namespace Apps.OpenAI.Services.Abstract;

public interface IResponseDeserializationService
{
    DeserializeTranslationEntitiesResult DeserializeResponse(string content);
}
