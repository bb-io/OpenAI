using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Identifiers
{
    public class ImageChatModelIdentifier
    {
        [Display("Model ID")]
        [StaticDataSource(typeof(ImageChatModelDataSourceHandler))]
        public string ModelId { get; set; }
    }
}
