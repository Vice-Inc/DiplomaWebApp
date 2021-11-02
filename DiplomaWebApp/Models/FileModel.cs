using DiplomaWebApp.Data.Base;

namespace DiplomaWebApp.Models
{
    public class FileModel : IEntityBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RandomNameInServer { get; set; }
        public string Path { get; set; }
    }
}
