using DiplomaWebApp.Data.Base;
using DiplomaWebApp.Data.Interfaces;
using DiplomaWebApp.Models;

namespace DiplomaWebApp.Data.Services
{
    public class FilesService : EntityBaseRepository<FileModel>, IFilesService
    {
        public FilesService(AppDbContext context) : base(context) { }
    }
}
