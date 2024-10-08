namespace Apps.OpenAI.Constants;

public static class ResponseFormats
{
    public static object GetProcessXliffResponseFormat()
    {
        return new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "TranslatedTexts",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        translations = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    translation_id = new
                                    {
                                        type = "string"
                                    },
                                    translated_text = new
                                    {
                                        type = "string"
                                    }
                                },
                                required = new[]
                                {
                                    "translation_id",
                                    "translated_text"
                                },
                                additionalProperties = false
                            }
                        }
                    },
                    required = new[]
                    {
                        "translations"
                    },
                    additionalProperties = false
                }
            }
        };
    }
    
    public static object GetQualityScoreXliffResponseFormat()
    {
        return new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "TranslatedTexts",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        translations = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    translation_id = new
                                    {
                                        type = "string"
                                    },
                                    quality_score = new
                                    {
                                        type = "number"
                                    }
                                },
                                required = new[]
                                {
                                    "translation_id",
                                    "quality_score"
                                },
                                additionalProperties = false
                            }
                        }
                    },
                    required = new[]
                    {
                        "translations"
                    },
                    additionalProperties = false
                }
            }
        };
    }
}