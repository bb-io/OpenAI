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

Some actions that are offered are pre-engineered on top of OpenAI. This means that they extend OpenAI's endpoints with additional prompt engineering for common language and content operations.

Do you have a cool use case that we can turn into an action? Let us know!

### Chat

- **Chat** given a chat message, returns a response.
- **Chat with system prompt** same as above but with an extra instructional system prompt parameter.

### Completions

- **Generate completion** generates a completion of the given text.
- **Create summary** (Pre-engineered) given a text, generates a summary.
- **Get translation issues** (Pre-engineered) given a source segment and NMT translated target segment, highlights potential translation issues. Can be used to prepopulate TMS segment comments.
- **Localize text** (Pre-engineered) given a text and a locale, tries to create a localized version of the text.
- **Post-edit MT** (Pre-engineered) given a source segment and NMT translated target segment, responds with a post-edited version of the target segment taking into account typical NMT mistakes.

### Edits

- **Generate edit** Given a text and an instruction, edits the given text.

### Audiovisual

- **Create transcription** transcribes the supported audiovisual file formats into a textual response.
- **Create English translation** same as above but automatically translated into English.

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
