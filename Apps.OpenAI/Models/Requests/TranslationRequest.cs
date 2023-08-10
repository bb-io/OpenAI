using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests
{
    public class TranslationRequest
    {
        [Display("File name")]
        public string FileName { get; set; }
        
        public byte[] File { get; set; }

        [Display("Temperature")]
        [DataSource(typeof(TemperatureDataSourceHandler))]
        public float? Temperature { get; set; }
    }
}
