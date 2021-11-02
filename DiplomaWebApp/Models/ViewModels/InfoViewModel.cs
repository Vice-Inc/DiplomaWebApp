using System.Collections.Generic;

namespace DiplomaWebApp.Models.ViewModels
{
    public class InfoViewModel
    {
        public List<double> Predictions { get; set; }
        public string Errors { get; set; }
        public FileModel FileModel { get; set; }
    }
}
