﻿using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.OpenAI.DataSourceHandlers
{
    public class ToneOfVoiceHandler : EnumDataHandler
    {
        // https://seowind.io/tone-of-voice-examples-ai/
        protected override Dictionary<string, string> EnumValues => new Dictionary<string, string>
        {
            { "The brand’s style is engaging and informative, aiming to educate and empower its audience. It utilizes a conversational voice, fostering a sense of community and approachability. Its tone is optimistic and encouraging, effectively mitigating potential intimidation and transforming it into enthusiasm. Embodying the essence of a nurturing guide, it supports aspirational undertakings with intricate knowledge. The brand communicates comprehensibly, ensuring clarity while pleasantly weaving colorful analogies and lighthearted humor.", "Knowledgeable Companion" },
            { "The brand’s style is engaging and informative, aiming to both educate and empower its audience. It utilizes a conversational voice, fostering a sense of community and approachability. Its tone is optimistic and encouraging, effectively mitigating any potential intimidation and transforming it into enthusiasm. Embodying the essence of a nurturing guide, it supports aspirational undertakings with intricate knowledge. The brand communicates in a comprehensible manner, ensuring clarity while pleasantly weaving colorful analogies and lighthearted humor throughout.", "Approachable Educator" },
            { "This tone employs rich detail, vivid imagery, and sensory language to bring stories to life and to help readers visualize the topic at hand. The tone is like painting a picture with words. It delves into the nuances, the textures, the colors, and the emotions of a scene or a concept. It’s not just about informing the reader but about transporting them into the narrative.", "Visual Narrator" },
            { "The brand voice is mimicking voice of fairies by being warm, friendly, whimsical and personalized. The tone is lightly magical and inviting, conveying love for nature and creativity. Language is informal, using first-person narrative to build a connection. It evokes uplifting emotions with undertones of fantasy and promises bespoke services with passion.", "Fairytale Friendliness" },
            { "The brand voice should be irreverent, cheeky, and clever. This isn’t a voice that looks to sugarcoat things but rather tells it as it is. It’s savvy, smart, and always has a retort up its sleeve. The communication should display intelligence and quick-wit while using sarcasm and cynicism as its main tools. While it’s predominantly tongue-in-cheek, it also carefully ensures it doesn’t delve into rudeness. \r\n\r\nThe brand’s style is edgy and bold. The brand voice should be cheeky, irreverent, and relatable. It should have an undercurrent of biting wit, sarcasm, and next-level humor, often challenging the status quo or conventional norms. It should convey messages in a way that disrupts usual corporate politeness and challenges audiences to think differently. The emotional tone should not alienate the audience, but rather make them feel part of an intellectual, somewhat rebellious discourse.", "Witty Cynic" },
            { "The voice of an empathetic brand would be both personal and relatable. Its communication would utilize the first person (“we”) to show that the brand is speaking directly. The language used would be simple, warm, and relatable, aiming to bridge the gap between the brand and the consumer and foster a relationship of trust. Brand’s tone would be supportive, understanding, and calming. It would be designed to reassure the consumer, instilling a sense of support and comfort. The tone would make users feel heard and cared for, bearing a sense of patience, warmth and making an effort to understand their perspective or problem.", "Empathetic Encourager" },
            { "The brand’s style is playful, spontaneous, and whimsical, resulting in charmingly clever content that elicits laughter and delight. The voice is audaciously bold, breezy and irreverent, ultimately transforming commonplace topics into entertaining discussions. The tone is upbeat, lighthearted, and engaging, masterfully using humor as a lens to provide refreshing insights or perspectives. The brand speaks with a vibrant, quick-witted spirit that appeals to its audience’s sense of fun and exploration.", "Playful Entertainer" },
            { "The brand adopts a commanding and authoritative voice, exuding confidence and stability. The tone is assertive yet charismatic, showcasing a sense of responsibility and an abundance of resources. The style is formal, leaning towards sophistication, synonymous with high status and leadership. The writing is clear and persuasive, demonstrating the brand’s power and control without losing its sense of compassion and trustworthiness.", "Confident Commander" },
            { "A voice and tone exudes wisdom, knowledge, and insight. The style is typically straightforward, factual, and thoughtful, with a focus on sharing valuable information rather than persuasion. The language used is articulate, respectful, and authoritative, often providing advice or guidance. The emotional tone is calm, collected, and supportive, fostering a sense of trust and credibility. The brand communicates as a mentor, with a patient and understanding demeanor.", "Thoughtful Advisor" },
            { "The brand’s style is mischievous, dynamic, and unconventional, embracing a ‘gremlin-esque’ persona. The voice is sharp, witty, and rebellious, showcasing brash confidence and quick-paced commentary. The tone is playful yet audacious, teetering between irreverence and edginess. The communication style is engaging, employing clever wordplay, resonant metaphors, and amusing anecdotes. The brand embodies unapologetic boldness, inviting audiences into an enticing world of quirky, disorderly charm.", "Provocative Prankster" },
        };       
    }
}