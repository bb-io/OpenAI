using Blackbird.Xliff.Utils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Utils.Xliff
{
    public class ParsedXliff
    {
         public string SourceLanguage { get; set; }
         public string TargetLanguage { get; set; }

         public List<TransUnit> TranslationUnits { get; set; }       

    }
}
