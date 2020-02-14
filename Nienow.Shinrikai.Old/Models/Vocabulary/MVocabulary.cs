using Nienow.Shinrikai.Vocabulary.Model;
using System.Collections.Generic;

namespace Nienow.Shinrikai.Models.Vocabulary
{
    public class MVocabulary
    {
        public Dictionary<int, string> Subjects { get; set; }
        public TWord Word { get; set; }
    }
}