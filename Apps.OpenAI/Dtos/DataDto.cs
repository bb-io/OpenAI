using System.Collections.Generic;

namespace Apps.OpenAI.Dtos;

public record DataDto<T>(IEnumerable<T> Data);