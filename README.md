# Blackbird.io OpenAI

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

This OpenAI app in Blackbird grants you access to all API endpoints and models that OpenAI has to offer from completion, chat, edit to DALL-E image generation and Whisper.

## Before setting up

Before you can connect you need to make sure that:

- You have an [OpenAI account](https://platform.openai.com/) and organization and have access to the API keys.

## Connecting

1. Navigate to apps and search for OpenAI. If you cannot find OpenAI then click _Add App_ in the top right corner, select OpenAI and add the app to your Blackbird environment.
2. Click _Add Connection_.
3. Name your connection for future reference e.g. 'My OpenAI connection'.
4. Fill in your Organization ID that you can find in your [OpenAI organization settings](https://platform.openai.com/account/org-settings). The organization ID has the shape `org-xxxxxxxxxxxxxxxxxxxxxxxx`.
5. Fill in your API key. You can create a new API key under [API keys](https://platform.openai.com/account/api-keys). The API key has the shape `sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`.
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

Be aware that model parameter's dynamic input returns all available models. You should select the model that is appropriate for the given task (e.g. gpt-4 model for **Chat** action). Action groups and models that should be used for them are shown in the table below.

| Action group |                                                                                                             Latest models                                                                                                              |                                                                                                                                                                                                                        Deprecated models                                                                                                                                                                                                                         |
| :----------: | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: |
|     Chat     | gpt-4 and dated model releases, gpt-4-1106-preview, gpt-4-vision-preview, gpt-4-32k and dated model releases, gpt-3.5-turbo and dated model releases, gpt-3.5-turbo-16k and dated model releases, fine-tuned versions of gpt-3.5-turbo |                                                                                                                                                                                    gpt-3.5-turbo-0613, gpt-3.5-turbo-16k-0613, gpt-3.5-turbo-0301, gpt-4-0314, gpt-4-32k-0314                                                                                                                                                                                    |
| Completions  |                                                                                            gpt-3.5-turbo-instruct, babbage-002, davinci-002                                                                                            |                                                                                                                                                                               text-ada-001, text-babbage-001, text-curie-001, text-davinci-001, text-davinci-002, text-davinci-003                                                                                                                                                                               |
| Audiovisual  |                                                         Only whisper-1 is supported for transcriptions and translations. tts-1 and tts-1-hd are supported for speech creation.                                                         |                                                                                                                                                                                                                                -                                                                                                                                                                                                                                 |
|    Images    |                                                                                                           dall-e-2, dall-e-3                                                                                                           |                                                                                                                                                                                                                                -                                                                                                                                                                                                                                 |
|  Embeddings  |                                                                                                         text-embedding-ada-002                                                                                                         | text-similarity-ada-001, text-similarity-babbage-001, text-similarity-curie-001, text-similarity-davinci-001, text-search-ada-doc-001, text-search-ada-query-001, text-search-babbage-doc-001, text-search-babbage-query-001, text-search-curie-doc-001, text-search-curie-query-001, text-search-davinci-doc-001, text-search-davinci-query-001, code-search-ada-code-001, code-search-ada-text-001, code-search-babbage-code-001, code-search-babbage-text-001 |

You can refer to the [Models](https://platform.openai.com/docs/models) documentation to find information about available models and the differences between them.

Some actions that are offered are pre-engineered on top of OpenAI. This means that they extend OpenAI's endpoints with additional prompt engineering for common language and content operations.

Do you have a cool use case that we can turn into an action? Let us know!

### Chat

- **Chat** given a chat message, returns a response.
- **Chat with system prompt** same as above but with an extra instructional system prompt parameter.
- **Generate edit** edits the provided text given an instruction.
- **Post-edit MT** (Pre-engineered) given a source segment and NMT translated target segment, responds with a post-edited version of the target segment taking into account typical NMT mistakes.
- **Get translation issues** (Pre-engineered) given a source segment and NMT translated target segment, highlights potential translation issues. Can be used to prepopulate TMS segment comments.
- **Perform LQA Analysis** performs an LQA Analysis of the translation and returns a text with issues if any.
- **Localize text** (Pre-engineered) given a text and a locale, tries to create a localized version of the text.

### Completions

- **Generate completion** generates a completion of the given text.
- **Create summary** (Pre-engineered) given a text, generates a summary.
- **Get translation issues** (Pre-engineered) given a source segment and NMT translated target segment, highlights potential translation issues. Can be used to prepopulate TMS segment comments.
- **Translate text** (Pre-engineered) given a text and a locale, tries to create a localized version of the text.
- **Post-edit MT** (Pre-engineered) given a source segment and NMT translated target segment, responds with a post-edited version of the target segment taking into account typical NMT mistakes.
- **Get MQM report** (Pre-engineered) performs an LQA Analysis of the translation. The result will be in the MQM framework form. The dimensions are: terminology, accuracy, linguistic conventions, style, locale conventions, audience appropriateness, design and markup. The input consists of the source and translated text. Optionally one can add languages and a description of the target audience.
- **Get MQM dimension values** (Pre-engineered) uses the same input and prompt as 'Get MQM report'. However, in this action the scores are returned as individual numbers so that they can be used in decisions. Also returns the proposed translation.

### Edits

- **Generate edit** Given a text and an instruction, edits the given text.

### Audiovisual

- **Create transcription** transcribes the supported audiovisual file formats into a textual response.
- **Create English translation** same as above but automatically translated into English.
- **Create speech** generates audio from the text input.

### Images

- **Generate image** use DALL-E to generate an image based on a prompt.

### Other

- **Create embedding** create a vectorized embedding of a text. Useful in combination with vector databases like Pinecone in order to store large sets of data.
- **Tokenize text** turn a text into tokens. Uses Tiktoken under the hood.

## Example

![1694620196801](image/README/1694620196801.png)
This simple example how OpenAI can be used to communicate with the Blackbird Slack app. Whenever the app is mentioned, the message will be send to Chat to generate an answer. We then use Amazon Polly to turn the textual response into a spoken-word resopnse and return it in the same channel.

## Missing features

In the future we will add actions for:

- Files
- Moderation
- Fine-tuning

Let us know if you're interested!

## Feedback

Feedback to our implementation of OpenAI is always very welcome. Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
