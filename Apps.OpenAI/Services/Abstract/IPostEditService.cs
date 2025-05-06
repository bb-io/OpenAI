using System.Collections.Generic;
using System.Threading.Tasks;
using Apps.OpenAI.Models.PostEdit;

namespace Apps.OpenAI.Services.Abstract;

public interface IPostEditService
{
    Task<XliffResult> PostEditXliffAsync(OpenAiXliffInnerRequest request);
    int GetModifiedSegmentsCount(Dictionary<string, string> original, Dictionary<string, string> updated);
}