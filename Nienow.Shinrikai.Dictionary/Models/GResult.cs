using Nienow.Shinrikai.Vocabulary.Model;
using System.Collections.Generic;

namespace Nienow.Shinrikai.Dictionary.Models
{
    public class GResult
    {
        public string Term { get; set; }
        public List<GSearchWord> Results { get; set; }
    }

    public class GSearchWord
    {
        public string FoundTerm { get; set; }
        public TWord Word { get; set; }
    }
}
