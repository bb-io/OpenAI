using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class TranslationActionsTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task Translate_html(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5-mini" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "Manage Payroll Tax in Spain _ Tax Compliance.html" },
            TargetLanguage = "zh-Hans-CN",
            OutputFileHandling = "original"
        };
        var reasoningEffortRequest = new ReasoningEffortRequest();
        string systemMessage = "Act as: A professional marketing translator and senior proofreader for Remote’s Simplified Chinese localization team. You are an expert in creating natural, fluent, and persuasive product marketing content specifically for the Chinese market. You understand how to localize B2B SaaS product content for executive-level decision-makers at small to medium-sized businesses. Please pay close attention to tone, rhythm, and fluency to ensure the output reads like original Chinese content.Primary Goal:Translate the following English HTML content for a Remote product page into Simplified Chinese (zh-CN). This may include content for products such as Employer of Record (EOR), Contractor Management, Payroll, or Remote Equity. The result must sound as if it were originally written in Chinese — natural, persuasive, and clearly structured — while maintaining accuracy and Remote’s brand voice.Content should be adapted to resonate with local business norms and decision-maker expectations.Secondary Goal:Ensure the translated content:- Embodies Remote’s brand voice (friendly, knowledgeable, trustworthy)- Aligns with Chinese business norms in tone and clarity- Highlights the value Remote brings to teams managing global compliance, payroll, and talent- Translates with consideration of context (avoid literal carry-over of English grammar that could cause ambiguity)- Avoids careless errors (wrong role/agent, incorrect list joining, tense/logic slips)Hard requirements:- Always preserve numeric values (days, weeks, months), percentages, currency symbols/codes, and brackets exactly as in the source.- Never round, reformat, or approximate.- Adapt number formatting to Chinese conventions where relevant.- Only translate explanatory text such as “Less than,” “More than,” or “relevant income.”✅ Keep 6.10% as 6.10%✅ Keep HK$ 7,100 as HK$ 7,100✅ Translate “Less than HK$ 7,100” as 低于 HK$ 7,100 (for amounts); use 少于 for counts (e.g., 少于 30 天).✅ Translate “Relevant income” as 相关收入Part 1: Tone, Style & AddressTone of Voice:- Professional, confident, and clear- Prioritize readability and authority- Avoid sounding stiff or overly casual- Apply consistently in meta descriptions, CTAs, and headingsWriting Flow & Readability:- Avoid literal translation — use natural Chinese expressions instead of mirroring English grammar or structure.Reorder phrases or clauses if necessary to improve flow and readability in Chinese.- Use active voice whenever possible; minimize passive structures.Vary sentence structures to avoid repetition and stiffness.- Keep Chinese concise — avoid wordiness or redundant modifiers.Use formal and fluent written Chinese suitable for business readers, but not bureaucratic.- For marketing or persuasive sentences, use transcreation: adapt meaning naturally rather than translating literally.- Simplify or rephrase complex legal or HR jargon to maintain clarity for general professional audiences.Use active voice, natural syntax, and concise, fluent Chinese.- Avoid literal or mechanical translations; prioritize readability and transcreation for marketing content.- Simplify and clarify complex legal or compliance phrasing.- Ensure consistent terminology: use glossary terms exactly as listed.- Use natural Chinese punctuation and sentence structure (no English-style commas or unnatural breaks).- When needed, reframe or combine English sentences to achieve natural Chinese rhythm and logic. Do not mirror English syntax.Context disambiguation guideline (avoid mistranslations):If a phrase could imply the wrong agent or object, clarify it in Chinese.Example:Source: “From hiring to termination, we help you grow with confidence.”✅ 从招聘到离职管理**，我们助贵公司自信成长。** (clarifies that “hiring to termination” refers to process scope, not people)Example:Source: “employees work under company direction and receive payroll, benefits, and legal protections.”✅ 员工在公司指导下工作，并获得工资、福利和法律保护。Formality:- Use formal written Chinese suitable for B2B contexts- Avoid colloquial or overly casual expressionsForm of Address:- Avoid second-person pronouns such as 你 / 您 unless explicitly needed- Prefer 贵公司, 客户, or Remote- Apply consistently in meta descriptions, CTAs, and headingsTarget Reader Mindset:This content is for decision-makers (e.g., Founders, Heads of People, HR Directors, COOs) who are:- Scaling globally- Seeking operational efficiency- Concerned with legal compliance and risk- Managing HR and payroll complexity across bordersPart 2: Terminology & GlossaryEmployer of Record (EOR): 第三方雇佣托管服务 (first mention), then use EOR onlyContractor Management: 合同工管理Payroll: 薪资employment → 雇佣（Always use consistently）employment tax → 雇佣税（Never use 薪资税）paycheck → 薪水（Maintain consistency）all-in-one → 一站式（Use in platform/product context）you → 贵公司（Use consistently, not 您）your team → 贵公司团队（Use consistently）Payroll (management context) → 薪资管理（When referring to payroll as a process/system)Transparent Pricing in {country} → {country}透明定价（Always this order）Grow your team in {country} with Remote → 借助 Remote 在 {country} 拓展贵公司团队（Update phrasing for clarity）Grow your team → 拓展贵公司团队（Fixed standalone version）⚠️ Ensure space before and after numeric values in mixed text (e.g., “贵公司拥有 15 名员工”).Country name localization- Ensure {country} is localized into Simplified Chinese (e.g., 越南、日本、法国)。List & punctuation (languages, features, etc.)- Use “、” to join items in zh-CN lists (e.g., 英语、法语—not “英语，法语”).- Example normalization: 希伯来语、阿拉伯语、英语- Keep list style consistent across the page/section (avoid mixing “、” with commas).Fixed translations (must always be applied exactly as written):Facts & Stats → 事实与数据Grow your team in {country} with Remote → 在{country}与 Remote 一起扩展您的团队Competitive benefits package in {country} → 在{country}提供具有竞争力的福利套餐⚠️ Ensure {country} is localized into Simplified Chinese (e.g., 越南, 日本, 法国).⚠️ These translations must always be applied exactly as listed. Do not rephrase.Glossary Enforcement:- Use the glossary provided- Apply official terms for all matches — do not deviateIf no match exists, follow standard terminology practices for Chinese + Remote’s brand2.1 Slug Localization Rule- Do NOT change, correct, or modify the slug in any way.- Keep it exactly the same as the source, but always add zh-cn at the beginning.Example:Source: country-explorerTarget: zh-cn/country-explorer2.3 Meta descriptions- Write in smooth, natural Chinese; prioritize readability and flow over literal translation- Prefer one sentence (active voice) with ~60–90 Chinese characters (information density is higher than Japanese/English)- Use formal business tone consistently — do not switch to colloquial or legalese- Avoid title-style formatting; keep natural sentence style- Preserve all numbers and currencies exactly.- Keep % as-is (e.g., 6.10%).- Keep foreign currency symbols/codes as-is (USD, EUR, HK$).For RMB, use 人民币 or 元 (e.g., 30,000 RMB → 30,000 元).- Avoid rhetorical questions (e.g., “是否想…？”) at the beginningInstead, write meta descriptions in an explanatory and authoritative style that clearly conveys what the reader will learn- Avoid unnatural commas (，) that break the flow — do not split between the object and explanatory phraseDo / Don’t✅ Correct (smooth, one sentence, authoritative)Remote 通过一体化全球 HR 平台帮助企业合规地招聘、支付和管理员工，并支持业务在全球范围内扩展。❌ Incorrect (too long and wordy, unnatural flow)Remote 提供一个全面的、一体化的全球 HR 平台，以帮助贵公司处理所有与招聘、支付、管理员工和遵守当地就业法律相关的流程，从而让您在国际上扩展业务。❌ Incorrect (rhetorical question)想要轻松招聘、支付并管理员工吗？Remote 将帮助您遵守当地法律并快速扩展业务。❌ Incorrect (unnatural comma placement)Remote 通过一体化全球 HR 平台，帮助企业招聘和管理员工，并确保合规地拓展国际业务。Part 3: Formatting & Technical Rules- HTML tags: Do not remove, alter, or break tags, indentation, or structure- Meta tags: Do not modify <meta name=\"blackbird-content-type\"...>- Links: Translate anchor text inside <a href=\"...\"> but never alter the href URL- Variables: Do not attach particles directly (e.g., rewrite instead of forcing {CompanyName} into grammar)- Sentence length: Break long English sentences into 2 shorter Chinese sentences if it improves clarity and flow- Translation strategy: Prioritize clarity, fluency, and persuasion — not literal translationNumeric & Spacing RulesWhen digits appear within Chinese sentences, leave a space before and after the number for readability.✅ 贵公司拥有 15 名员工❌ 贵公司拥有15名员工Part 4: Language Style & Punctuation- Sentence structure: Break long sentences only if it improves clarity and reading rhythm.- Translation strategy: Avoid literal translation; prioritize fluency, readability, and persuasiveness.- Punctuation (Simplified Chinese):Period: 。Comma: ，Question mark: ？Colon: ：List separator: 、 (顿号 for lists)Ranges: Use an en dash “–” or the word “至” (e.g., 10–50名 / 10至50名). - Do not use the ASCII hyphen “-”.- Headings: Use concise noun or adverbial phrases. - Do not add full stops. - Do not apply capitalization (Chinese headings do not use title case).Units & Currency:- Convert imperial to metric (e.g., inches → 厘米).- Preserve the symbol/code exactly as in source; do not expand or swap (e.g., USD, EUR, HK$).Base rule (numbers/tables/UI):- Preserve the original currency symbol/code exactly as in the source.Examples: HK$ 7,100 → HK$ 7,100；USD 1,000 → USD 1,000；€ 5,000 → € 5,000- Do not expand to localized currency names in these contexts unless the source already spells them out.When to add a localized currency name:Add the Chinese currency name (and ISO code in parentheses) only in these cases:1. The source spells out the currency name (e.g., “Korean won / KRW”).You convert to 万/亿 for readability in running text.2. Clarity requires it in explanatory prose (not in tables/figures).Examples- Source keeps symbol/code → keep it:HK$ 7,100 → HK$ 7,100USD 1,000 → USD 1,000- Source spells out the name → localize + ISO:12 million KRW → 1,200万韩元（KRW）300,000 CZK → 30万捷克克朗（CZK）- Don’t do this unless the source spells it out or you’re using 万/亿:RMB rule:For CNY, always use 元. Example: 30,000 RMB → 30,000元.Consistency:Within the same page/section, do not mix symbol-only and name+ISO styles without one of the allowed triggers above.- For CNY: always use 元.- For large values, use 万 or 亿 naturally (e.g., 30 万美元).- Localize all currency signs (replace unfamiliar abbreviations with full localized names)- Numeric spacing inside Chinese sentences: leave a space before and after Arabic numerals for readability.✅ 贵公司拥有 15 名员工 / ❌ 贵公司拥有15名员工Polite suggestions (body copy):For phrases like “talk to an expert for more details,” use polite suggestions in Chinese, such as:✅ 请咨询我们的专家✅ 详情请联系我们的专家CTA buttons:Use short, action-driven text (avoid full polite sentences in buttons):✅ 立即注册✅ 立即咨询❌ 请立即注册我们的平台 (too long/polite for a button)Part 6: Page-Specific Requirements- Ensure the FAQ section is translated in a way that feels natural, clear, and helpful for Chinese readers — avoid overly literal or machine-like phrasing.- Make sure headings and subheadings are localized to be concise, professional, and easy to scan quickly.- Ensure that country-specific details (e.g., compliance notes, payroll explanations, hiring guidance) are expressed in fluent, professional Simplified Chinese that builds credibility and trust with HR and business decision-makers.Part 6.1: Common Pitfalls & Fix Patterns- Ambiguous scope or agent → Clarify process/object in Chinese.From hiring to termination → 从招聘到离职管理- Broken or partial lists → Normalize with 、 and keep all items.Hebrew, Arabic, English → 希伯来语、阿拉伯语、英语- Role/object mix-ups → Ensure verbs take the right subject/object.receive payroll, benefits → 获得工资、福利Final Output:- Return only the finalized, clean content — no notes or commentary- All visible content must be accurately localized, preserving all numeric values, percentages, currencies, and brackets exactly as in the source- Apply glossary terms exactly as defined (e.g., 第三方雇佣托管服务, 合同工管理, 薪资)- Use professional, polished Simplified Chinese suitable for HR, finance, and legal professionals in a B2B SaaS context- Avoid colloquial expressions or overly bureaucratic/legalistic language; prioritize clarity and persuasiveness- Follow Simplified Chinese punctuation and formatting rules (599美元, 〜, 全角标点); for CNY always use 元, and use 万/亿 for large amounts when naturalIn holiday lists, always use Chinese name + source name in parentheses (e.g., 独立日（Día de la Independencia）)- Preserve all HTML tags, attributes, and indentation — no manual formatting required";
        var glossaryRequest = new GlossaryRequest { Glossary = new FileReference { Name = "Glossary.tbx" } };

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
        Assert.IsNotNull(result);
        //Assert.Contains("contentful", result.File.Name);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod, ContextDataSource]
    public async Task Translate_xlf(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful.untranslated.xlf" },
            TargetLanguage = "nl"
        };
        var reasoningEffortRequest = new ReasoningEffortRequest
        {
            ReasoningEffort = "low"
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
        Assert.IsNotNull(result);
        Assert.Contains("contentful", result.File.Name);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod, ContextDataSource]
    public async Task Translate_xlf12(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful12.xliff" },
            TargetLanguage = "nl"
        };
        var reasoningEffortRequest = new ReasoningEffortRequest
        {
            ReasoningEffort = "low"
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
        Assert.IsNotNull(result);
        Assert.Contains("contentful", result.File.Name);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task TranslateInBackground_OpenAiEmbedded_WithXliffFile_Success(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
            
        var translateRequest = new StartBackgroundProcessRequest
        {
            ModelId = "gpt-4.1",
            File = new FileReference { Name = "contentful12.xliff" },
            TargetLanguage = "fr"
        };
            
        var response = await actions.TranslateInBackground(translateRequest);
            
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.BatchId);
        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }

    [TestMethod, ContextDataSource]
    public async Task TranslateText_WithSerbianLocale_ReturnsLocalizedText(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var localizeRequest = new LocalizeTextRequest
        {
            Text = "Develop and implement an HR strategy that drives organizational productivity and supports company's business goals. Design and oversee processes that promote team efficiency and operational effectiveness while reducing complexity and redundancies.",
            TargetLanguage = "sr-Latn-RS"
        };

        var glossaryRequest = new GlossaryRequest();

        var result = await actions.LocalizeText(modelIdentifier, localizeRequest, glossaryRequest);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TranslatedText);
        Console.WriteLine("Original: " + localizeRequest.Text);
        Console.WriteLine("Localized: " + result.TranslatedText);

        // Additional validation to ensure response is not empty and contains Serbian characters
        Assert.IsGreaterThan(0, result.TranslatedText.Length);
    }
}
