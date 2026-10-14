using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Image;

public class GenerateLocalizedImageVariantRequest
{
    [Display("Localized XLIFF file", Description = "A translated XLIFF file containing the original image and source and target segments.")]
    public FileReference LocalizedFile { get; set; }

    [Display("Additional instructions")]
    public string? AdditionalInstructions { get; set; }

    [Display("Output image name", Description = "The name of the output image without the extension.")]
    public string? OutputImageName { get; set; }
}
