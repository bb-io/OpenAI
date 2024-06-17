# Blackbird.io OpenAI

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

This OpenAI app in Blackbird grants you access to all API endpoints and models that OpenAI has to offer from completion, chat, edit to DALL-E image generation and Whisper.

## Before setting up

Before you can connect you need to make sure that:

- You have an [OpenAI account](https://platform.openai.com/signup).
- You have an organization and have obtained its Organization ID that you can find in your [OpenAI organization settings](https://platform.openai.com/account/org-settings). The organization ID has the shape `org-xxxxxxxxxxxxxxxxxxxxxxxx`.
- You have generated a new API key in the [API keys](https://platform.openai.com/account/api-keys) section, granting programmatic access to OpenAI models on a 'pay-as-you-go' basis. With this, you only pay for your actual usage, which [starts at $0,002 per 1,000 tokens](https://openai.com/pricing) for the fastest chat model. Note that the ChatGPT Plus subscription plan is not applicable for this; it only provides access to the limited web interface at chat.openai.com and doesn't include OpenAI API access. Ensure you copy the entire API key, displayed once upon creation, rather than an abbreviated version. The API key has the shape `sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`.
- Your API account has a payment method and a positive balance, with a minimum of $5. You can set this up in the [Billing settings](https://platform.openai.com/account/billing/overview) section.

**Note**: Blackbird by default uses the latest models in its actions. If your subscription does not support these models then you have to add the models you can use in every Blackbird action.

## Connecting

1. Navigate to apps and search for OpenAI. If you cannot find OpenAI then click _Add App_ in the top right corner, select OpenAI and add the app to your Blackbird environment.
2. Click _Add Connection_.
3. Name your connection for future reference e.g. 'My OpenAI connection'.
4. Fill in your Organization ID obtained earlier.
5. Fill in your API key obtained earlier.
6. Click _Connect_.

![1694611695232](image/README/1694611695232.png)

## Actions

All textual actions have the following optional input values in order to modify the generated response:

- Model (defaults to the latest)
- Maximum tokens
- Temperature
- top_p
- Presence penalty
- Frequency penalty

For more in-depth information about most actions consult the [OpenAI API reference](https://platform.openai.com/docs/api-reference).

Different actions support various models that are appropriate for the given task (e.g. gpt-4 model for **Chat** action). Action groups and the corresponding models recommended for them are shown in the table below.

| Action group |                                                                                                                          Latest models                                                                                                                           |      Default model (when _Model ID_ input parameter is unspecified)      |                                                                                                                                                                                                                        Deprecated models                                                                                                                                                                                                                         |
| :----------: | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: | :----------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: |
|     Chat     | gpt-4-turbo-preview and dated model releases, gpt-4 and dated model releases, gpt-4-vision-preview, gpt-4-32k and dated model releases, gpt-3.5-turbo and dated model releases, gpt-3.5-turbo-16k and dated model releases, fine-tuned versions of gpt-3.5-turbo | gpt-4-turbo-preview; gpt-4-vision-preview for **Chat with image** action |                                                                                                                                                                                    gpt-3.5-turbo-0613, gpt-3.5-turbo-16k-0613, gpt-3.5-turbo-0301, gpt-4-0314, gpt-4-32k-0314                                                                                                                                                                                    |
| Audiovisual  |                                                                      Only whisper-1 is supported for transcriptions and translations. tts-1 and tts-1-hd are supported for speech creation.                                                                      |                  tts-1-hd for **Create speech** action                   |                                                                                                                                                                                                                                -                                                                                                                                                                                                                                 |
|    Images    |                                                                                                                        dall-e-2, dall-e-3                                                                                                                        |                                 dall-e-3                                 |                                                                                                                                                                                                                                -                                                                                                                                                                                                                                 |
|  Embeddings  |                                                                                                                      text-embedding-ada-002                                                                                                                      |                          text-embedding-ada-002                          | text-similarity-ada-001, text-similarity-babbage-001, text-similarity-curie-001, text-similarity-davinci-001, text-search-ada-doc-001, text-search-ada-query-001, text-search-babbage-doc-001, text-search-babbage-query-001, text-search-curie-doc-001, text-search-curie-query-001, text-search-davinci-doc-001, text-search-davinci-query-001, code-search-ada-code-001, code-search-ada-text-001, code-search-babbage-code-001, code-search-babbage-text-001 |

You can refer to the [Models](https://platform.openai.com/docs/models) documentation to find information about available models and the differences between them.

Some actions that are offered are pre-engineered on top of OpenAI. This means that they extend OpenAI's endpoints with additional prompt engineering for common language and content operations.

Do you have a cool use case that we can turn into an action? Let us know!

### Chat

- **Chat** given a chat message, returns a response. You can optionally add a system prompt and/or an image. Also you can add collection of texts and it will be added to the prompt along with the message. Also you can optionally add Glossary as well. Useful if you want to add collection of messages to the prompt.
- **Chat with system prompt** the same as above except that the system prompt is mandatory.

### Assistant

- **Message assistant** sends a message to a pre-configured assistant. It will execute the assistant and return its message. The assistant action can accept up to 10 files as input. **Note**: If you want to read the attached files make sure your assistant has _Retrieval_ enabled.

### Localization (pre-engineered)

- **Post-edit MT** given a source segment and NMT translated target segment, responds with a post-edited version of the target segment taking into account typical NMT mistakes.
- **Get translation issues** given a source segment and NMT translated target segment, highlights potential translation issues. Can be used to prepopulate TMS segment comments.
- **Get MQM report** performs an LQA Analysis of the translation. The result will be in the MQM framework form. The dimensions are: terminology, accuracy, linguistic conventions, style, locale conventions, audience appropriateness, design and markup. The input consists of the source and translated text. Optionally one can add languages and a description of the target audience.
- **Get MQM dimension values** uses the same input and prompt as 'Get MQM report'. However, in this action the scores are returned as individual numbers so that they can be used in decisions. Also returns the proposed translation.
- **Translate text** given a text and a locale, tries to create a localized version of the text.

- **Get localizable content from image** retrieves localizable content from given image.

### Content repurposing

All actions can take: Target audience, locale, glossary, tone of voice and any additional prompts

- **Repurpose content** repurposes content to a specific target audience.
- **Summarize content** creates a summary of content.

### Glossary extraction

- **Extract glossary** extracts a glossary (.tbx) from any (multilingual) content. You can use this in well in conjunction with other apps that take glossaries.

### Audiovisual

- **Create transcription** transcribes the supported audiovisual file formats into a textual response.
- **Create English translation** same as above but automatically translated into English.
- **Create speech** generates audio from the text input.

### Images

- **Generate image** use DALL-E to generate an image based on a prompt.

### Other

- **Create embedding** create a vectorized embedding of a text. Useful in combination with vector databases like Pinecone in order to store large sets of data.
- **Tokenize text** turn a text into tokens. Uses Tiktoken under the hood.

### XLIFF operations

- **Get Quality Scores for XLIFF file** Gets segment and file level quality scores for XLIFF files. Supports only version 1.2 of XLIFF currently. Optionally, you can add Threshold, New Target State and Condition input parameters to the Blackbird action to change the target state value of segments meeting the desired criteria (all three must be filled).

    Optional inputs:
	- Prompt: Add your criteria for scoring each source-target pair. If none are provided, this is replaced by _"accuracy, fluency, consistency, style, grammar and spelling"_.
	- Bucket size: Amount of translation units to process in the same request.
	- Source and Target languages: By defualt, we get these values from the XLIFF header. You can provide different values, no specific format required. 
	- Threshold: value between 0-10.
	- Condition: Criteria to filter segments whose target state will be modified.
	- New Target State: value to update target state to for filtered translation units.

    Output:
	- Average Score: aggregated score of all segment level scores.
	- Updated XLIFF file: segment level score added to extradata attribute & updated target state when instructed.

As an example, the settings in the image below will result in all semegments with a score below or equal to 6 to have their target state updated to "needs-review-translation". 

![OpenAIOptionalInput](image/README/OpenAIOptionalInput.png)

- **Post-edit XLIFF file** Updates the targets of XLIFF 1.2 files

Optional inputs:
	- Prompt: Add your linguistic criteria for postediting targets.
	- Bucket size: Amount of translation units to process in the same request.
	- Source and Target languages: By defualt, we get these values from the XLIFF header. You can provide different values, no specific format required.
	- Glossary

- **Process XLIFF file** given an XLIFF file, processes each translation unit according to provided instructions (default is to translate source tags) and updates the target text for each unit. Supports only version 1.2 of XLIFF currently.

Here is an example bird for processing XLIFF files:

![XLIFF1](image/README/XLIFF1.png)

![XLIFF2](image/README/XLIFF2.png)

![XLIFF3](image/README/XLIFF3.png)

![XLIFF4](image/README/XLIFF4.png)

## Example

![1694620196801](image/README/1694620196801.png)
This simple example how OpenAI can be used to communicate with the Blackbird Slack app. Whenever the app is mentioned, the message will be send to Chat to generate an answer. We then use Amazon Polly to turn the textual response into a spoken-word resopnse and return it in the same channel.

## Missing features

In the future we can add actions for:

- Files
- Moderation
- Fine-tuning

Let us know if you're interested!

## Actions limitations

- For every action maximum allowed timeout are 300 seconds (5 minutes). If the action takes longer than 300 seconds, it will be terminated. Based ob our experience, even complex actions should take less than 5 minutes. But if you have a use case that requires more time, let us know.

## Feedback

Feedback to our implementation of OpenAI is always very welcome. Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
