namespace Apps.OpenAI.Constants;

public static class ResponseFormats
{
    public static object GetXliffResponseFormat()
    {
        return new
        {
            type = "json_schema",
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
                            }
                            ,
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
        };
    }
    
    public static object GetQualityScoreXliffResponseFormat()
    {
        return new
        {
            type = "json_schema",
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
                            }
                            ,
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
        };
    }
    
    public static object GetMqmReportResponseFormat()
    {
        return new
        {
            type = "json_schema",
            name = "MqmReports",
            strict = true,
            schema = new
            {
                type = "object",
                properties = new
                {
                    reports = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                segment_id = new
                                {
                                    type = "string"
                                },
                                mqm_report = new
                                {
                                    type = "string"
                                }
                            }
                            ,
                            required = new[]
                            {
                                "segment_id",
                                "mqm_report"
                            },
                            additionalProperties = false
                        }
                    }
                },
                required = new[]
                {
                    "reports"
                },
                additionalProperties = false
            }
        };
    }

    public static object GetCodeReviewResponseFormat()
    {
        return new
        {
            type = "json_schema",
            name = "CodeReviewFindings",
            strict = true,
            schema = new
            {
                type = "object",
                properties = new
                {
                    summary = new
                    {
                        type = "string"
                    },
                    findings = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                path = new
                                {
                                    type = "string"
                                },
                                line = new
                                {
                                    type = "integer"
                                },
                                side = new
                                {
                                    type = "string",
                                    @enum = new[] { "RIGHT" }
                                },
                                severity = new
                                {
                                    type = "string",
                                    @enum = new[] { "critical", "high", "medium", "low" }
                                },
                                category = new
                                {
                                    type = "string"
                                },
                                title = new
                                {
                                    type = "string"
                                },
                                body = new
                                {
                                    type = "string"
                                },
                                suggestion = new
                                {
                                    type = "string"
                                }
                            },
                            required = new[]
                            {
                                "path",
                                "line",
                                "side",
                                "severity",
                                "category",
                                "title",
                                "body",
                                "suggestion"
                            },
                            additionalProperties = false
                        }
                    }
                },
                required = new[]
                {
                    "summary",
                    "findings"
                },
                additionalProperties = false
            }
        };
    }
}
