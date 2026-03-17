# Blackbird.io OpenAI

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

This OpenAI app in Blackbird gives you access to OpenAI APIs for chat, audio, images, and localization workflows. You can use it with both OpenAI's official API and Azure OpenAI endpoints, using the models available in your account.

The app now uses OpenAI Responses API for text-generation workflows (including chat, translation, editing, review, reporting, and repurposing flows), which enables support for newer model families and built-in tools like web search.

## Before setting up

Before you can connect you need to make sure that:

### For OpenAI actions

- You have an [OpenAI account](https://platform.openai.com/signup).
- You have generated a new `API key` in the [API keys](https://platform.openai.com/account/api-keys) section, granting programmatic access to OpenAI models on a 'pay-as-you-go' basis. With this, you only pay for your actual usage, which [starts at $0,002 per 1,000 tokens](https://openai.com/pricing) for the fastest chat model. Note that the ChatGPT Plus subscription plan is not applicable for this; it only provides access to the limited web interface at chat.openai.com and doesn't include OpenAI API access. Ensure you copy the entire API key, displayed once upon creation, rather than an abbreviated version. The API key has the shape `sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`.
- Your API account has a payment method and a positive balance, with a minimum of $5. You can set this up in the [Billing settings](https://platform.openai.com/account/billing/overview) section.

**Note**: Blackbird by default uses the latest models in its actions. If your subscription does not support these models then you have to add the models you can use in every Blackbird action.

### For Azure OpenAI actions

- You have the `Resource URL` for your Azure OpenAI account.
- You know `Deployment name` and `API key` for your Azure OpenAI account.

You can find how to create and deploy an Azure OpenAI Service resource [here](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).

## Connecting

Navigate to apps and search for OpenAI and click _Add Connection_. This application has three connection types: OpenAI, Azure OpenAI, and OpenAI with an embedded model in the connection. You can select the connection you need from the dropdown menu. Please give your connection a name for future reference, e.g. 'My OpenAI Connection'.

![connection](image/README/common.png)

### OpenAI

1. Select `OpenAI` connection type.
2. Fill in your `API key` obtained earlier.
3. Click _Connect_.
4. Verify that connection was added successfully.

![connection](image/README/openai.png)

If you choose `OpenAI (embedded model)` connection type, please specify the model you want to use.

![connection](image/README/openai-embed.png)

**Note**: The selected model can be overridden in the action using the `Model` input.

### Azure OpenAI

1. Select `Azure OpenAI` connection type.
2. Enter the `Resource URL`, `Deployment name`, and `API key` for your Azure OpenAI account.
3. Click _Connect_.
4. Verify that connection was added successfully.

**Note**: Pay attention to your `Resource URL` connection parameter. Sometimes the correct URL could have some path after a domain name. For example: https://example.openai.azure.com/**openai**

![connection](image/README/azure.png)

## Actions

Actions are grouped below. Optional inputs are listed as Advanced settings.

### Chat

- **Chat** Outputs a response to a chat message. Advanced settings: **Model**: Select the model to use for OpenAI connections (ignored for Azure OpenAI). **System prompt**: Optional system instruction to guide the response. **Texts**: Texts that will be added to the user prompt along with the message. **File**: Optional file to include (text, image, or audio). **Maximum tokens**: Maximum number of tokens to generate. **Temperature**: Controls randomness in the output. **top_p**: Use nucleus sampling. **Presence penalty**: Penalize new tokens based on whether they appear in the text so far. **Frequency penalty**: Penalize new tokens based on frequency in the text so far. **Reasoning effort**: Controls how much reasoning effort the model applies. **Glossary**: Glossary file to enforce term usage.
- **Chat with system prompt** Outputs a response to a chat message with a required system prompt. Advanced settings: **Image**: Optional image to include with the message.

### Translation

- **Translate** Translates file content from a CMS or file storage and outputs localized content for compatible actions. Advanced settings: **Source language**: Override the source language if it is not already set in the file. **Output file handling**: Determine the format of the output file. The default Blackbird behavior is to convert to XLIFF for future steps. **Additional instructions**: Additional instructions to guide the translation. **Bucket size**: Number of source texts to translate at once (default 1500).
- **Translate in background** Starts background translation for a file and outputs a batch ID to download results later.
- **Translate text** Outputs localized text for the provided input text.

### Editing

- **Edit** Edits translated file content and outputs reviewed content. Advanced settings: **Target language**: Override the target language if it is not already set in the file. **Filter glossary terms**: Filter glossary terms to those present in the content. **Modified by**: Add modified-by metadata to updated units. **Process only segments with this state**: Only segments with this state will be processed. **Max tokens**: The maximum number of tokens to generate in the completion (default 1000).
- **Edit in background** Starts background editing for translated content and outputs a batch ID to download results later.
- **Edit text** Reviews translated text and outputs an edited version. Advanced settings: **Target audience**: Specify the target audience. **Additional prompt**: Additional prompt to guide editing.
- **Apply prompt to bilingual content (experimental)** Applies a prompt to each translation unit and outputs updated target text, with optional batching.

### Reporting

- **Create MQM report** Performs MQM analysis for translated text and outputs a report.
- **Create MQM report in background** Starts background MQM analysis for translated file content and outputs a batch ID to download results later.
- **Create MQM report from file** Performs MQM analysis for translated file content and outputs a report.

### Review

- **Get translation issues** Reviews translated text and outputs issue descriptions.
- **Get MQM dimension values** Performs MQM analysis for translated text and outputs per-dimension scores with a proposed translation.
- **Review text** Reviews translated text quality and outputs a quality score. Advanced settings: **top_k**: Only sample from the top K options for each subsequent token.
- **Review** Reviews translated file content and outputs segment quality scores. Advanced settings: **Score threshold**: All segments above this score will automatically be finalized (0..1). **Stop sequences**: Sequences that will cause the model to stop generating completion text.

### Background

- **Download background file** Downloads content processed in the background after the job is complete.
- **Get background result** Gets MQM report results from a background batch process.

### Repurposing

- **Summarize text** Summarizes text for different target audiences, languages, tones of voice and platforms. Summaries are shorter than the original text. Advanced settings: **Tone of voice**: Select the tone of voice for the output. **Locale**: Locale for the output language.
- **Summarize** Summarizes content for different target audiences, languages, tones of voice and platforms. Summaries are shorter than the original text.
- **Repurpose text** Repurposes text for different target audiences, languages, tones of voice and platforms. Repurposing does not significantly change the length of the content.
- **Repurpose** Repurposes content for different target audiences, languages, tones of voice and platforms. Repurposing does not significantly change the length of the content.

### Glossaries

- **Extract glossary** Extracts glossary terms from text and outputs a glossary file. Advanced settings: **Name**: Name of the output glossary file.

### Audio

- **Create English translation** Translates speech from an audio or video file into English text.
- **Create transcription** Transcribes speech from an audio or video file and outputs text. Advanced settings: **Language (ISO 639-1)**: Language of the audio. **Timestamp granularities**: Return timestamps at the specified granularity (only supported with whisper-1). **Prompt**: Text to guide the model's style or continue a previous audio segment.
- **Create speech** Generates speech audio from input text. Advanced settings: **Output audio name**: The name of the output audio file without the extension. **Response format**: Output audio format. **Speed**: Speech speed multiplier.

### Images

- **Generate image** Generates an image from a prompt. Advanced settings: **Output image name**: The name of the output image without the extension. **Size**: Image size. **Quality (only for dall-e-3)**: Image quality. **Style (only for dall-e-3)**: Image style.
- **Get localizable content from image** Extracts localizable text content from an image.

### Text analysis

- **Create embedding** Generates an embedding vector for input text. Advanced settings: **Model ID**: Select the embedding model to use.
- **Tokenize text** Tokenizes input text and outputs token IDs. Advanced settings: **Encoding**: Tokenizer encoding to use.

### Bucket size, performance and cost

Files can contain a lot of segments. Each action takes your segments and sends them to OpenAI for processing. It's possible that the amount of segments is so high that the prompt exceeds to model's context window or that the model takes longer than Blackbird actions are allowed to take. This is why we have introduced the bucket size parameter. You can tweak the bucket size parameter to determine how many segments to send to OpenAI at once. This will allow you to split the workload into different OpenAI calls. The trade-off is that the same context prompt needs to be send along with each request (which increases the tokens used). From experiments we have found that a bucket size of 1500 is sufficient for gpt-4o. That's why 1500 is the default bucket size, however other models may require different bucket sizes.

### Temperatures

OpenAI allows you to set the temperature value for and Action you perform in Blackbird. You can customize this value (between 0.0 and 2.0) but we've also added handy labels that are available in the Blackbird UI:

- 0.2 Governed: Maximum predictability.
- 0.6 Balanced: A controlled blend of accuracy and flexibility.
- 1.0 Expressive: Accuracy is preserved, structure is looser (default).
- 1.2 Exploratory: Encourages alternative phrasings and ideas while remaining context-aware.
- 1.6 Experimental: Prioritizes creativity over predictability.

### Models

For more in-depth information about most actions consult the [OpenAI API reference](https://platform.openai.com/docs/api-reference) or [Azure OpenAI API reference](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/latest).

Different actions support various models that are appropriate for the given task (e.g. gpt-4 model for **Chat** action). Action groups and the corresponding models recommended for them are shown in the table below.

| Action group |                                                                                                                          Latest models                                                                                                                           |      Default model (when _Model ID_ input parameter is unspecified)      |                                                                                                                                                                                                                        Deprecated models                                                                                                                                                                                                                         |
| :----------: | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: | :----------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: |
|     Chat     | gpt-4o, gpt-o1, gpt-o1 mini,  gpt-4-turbo-preview and dated model releases, gpt-4 and dated model releases, gpt-4-vision-preview, gpt-4-32k and dated model releases, gpt-3.5-turbo and dated model releases, gpt-3.5-turbo-16k and dated model releases, fine-tuned versions of gpt-3.5-turbo | gpt-4-turbo-preview; gpt-4-vision-preview for **Chat with image** action |                                                                                                                                                                                    gpt-3.5-turbo-0613, gpt-3.5-turbo-16k-0613, gpt-3.5-turbo-0301, gpt-4-0314, gpt-4-32k-0314                                                                                                                                                                                    |
| Audiovisual  |                                                                      Only whisper-1 is supported for transcriptions and translations. tts-1 and tts-1-hd are supported for speech creation.                                                                      |                  tts-1-hd for **Create speech** action                   |                                                                                                                                                                                                                                -                                                                                                                                                                                                                                 |
|    Images    |                                                                                                                        dall-e-2, dall-e-3                                                                                                                        |                                 dall-e-3                                 |                                                                                                                                                                                                                                -                                                                                                                                                                                                                                 |
|  Embeddings  |                                                                                                                      text-embedding-ada-002                                                                                                                      |                          text-embedding-ada-002                          | text-similarity-ada-001, text-similarity-babbage-001, text-similarity-curie-001, text-similarity-davinci-001, text-search-ada-doc-001, text-search-ada-query-001, text-search-babbage-doc-001, text-search-babbage-query-001, text-search-curie-doc-001, text-search-curie-query-001, text-search-davinci-doc-001, text-search-davinci-query-001, code-search-ada-code-001, code-search-ada-text-001, code-search-babbage-code-001, code-search-babbage-text-001 |

You can refer to the [Models](https://platform.openai.com/docs/models) documentation to find information about available models and the differences between them.

Some actions that are offered are pre-engineered on top of OpenAI. This means that they extend OpenAI's endpoints with additional prompt engineering for common language and content operations.

Do you have a cool use case that we can turn into an action? Let us know!

## Eggs

Check downloadable workflow prototypes featuring this app that you can import to your Nests [here](https://docs.blackbird.io/eggs/tms-to-llm/). 

### Limitations

- The maximum number of translation units in the file is `50 000` because a single batch may include up to 50,000 requests

### Reasoning effort

GPT 5.1 introduced the configurable input of "Reasoning effort". All models prior to 5.1 ignore this input and use "medium" reasoning effort. In Blackbird, if you don't provide any input to reasoning effort then by default it passes "medium" to OpenAI.

With GPT-5, you can now directly control how much the model says using the verbosity parameter. Set it to low for concise answers, medium for balanced detail, or high for in-depth explanations.

### Web search (Chat actions)

Web search is available as an optional feature in both chat actions:

- **Chat**
- **Chat with system prompt**

When enabled, the action can return:

- **Citations** (URLs cited in the generated answer)
- **Sources** (all URLs consulted during web search)

Optional web search inputs:

- **Web search context size**: low, medium, high
- **Allow live web access**: if disabled, uses cached/indexed web content only
- **Allowed domains**: optional domain allow-list (up to 100)
- **User location**: city, country (ISO-2), region, timezone (IANA)

Web search limitations:

- Web search is not supported for `gpt-4.1-nano`.
- Web search is not supported for `gpt-5` when reasoning effort is `minimal`.
- For Azure OpenAI connections, web search in chat actions is not supported by this app.

### How to know if the batch process is completed? 

You have 3 options here:

1. You can use the `On background job finished` event trigger to get notified when the batch process is completed. But note, that this is a polling trigger and it will check the status of the batch process based on the interval you set.
2. Use the `Delay` operator to wait for a certain amount of time before checking the status of the batch process. This is a more straightforward way to check the status of the batch process.
3. Since October 2024, users can rely on [Checkpoints](https://docs.blackbird.io/concepts/checkpoints/#large-language-models-llms--batch-processing) to achieve a fully streamlined process. A Checkpoint can pause the workflow until the LLM returns a result or a batch process completes. 

We recommend using the `On background job finished` event trigger as it is more efficient for checking the status of the batch process.

## Events

### Background

- **On background job finished** Triggered when a batch job reaches a terminal state (completed, failed, or cancelled).

## Example

![1694620196801](image/README/1694620196801.png)
This simple example how OpenAI can be used to communicate with the Blackbird Slack app. Whenever the app is mentioned, the message will be send to Chat to generate an answer. We then use Amazon Polly to turn the textual response into a spoken-word resopnse and return it in the same channel.

## Actions limitations

- For every action maximum allowed timeout are 600 seconds (10 minutes). If the action takes longer than 600 seconds, it will be terminated. Based on our experience, even complex actions should take less than 10 minutes. But if you have a use case that requires more time, let us know.

> OpenAI is sometimes prone to errors. If your Flight fails at an OpenAI step, please check https://status.openai.com/history first to see if there is a known incident or error communicated by OpenAI. If there are no known errors or incidents, please feel free to report it to Blackbird Support.

## Feedback

Feedback to our implementation of OpenAI is always very welcome. Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
