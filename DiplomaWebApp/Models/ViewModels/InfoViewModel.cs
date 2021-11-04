using System.Collections.Generic;

namespace DiplomaWebApp.Models.ViewModels
{
    public class InfoViewModel
    {
        public Dictionary<string, List<double>> Predictions { get; set; }
        public List<int> ListOfMaxIndexes { get; set; }
        public string Errors { get; set; }
        public FileModel FileModel { get; set; }
    }
}
