using DiplomaWebApp.Data.Base;
using DiplomaWebApp.Models;
using System.Threading.Tasks;

namespace DiplomaWebApp.Data.Interfaces
{
    public interface IFilesService : IEntityBaseRepository<FileModel>
    {
        public Task<FileModel> GetByRandomNameAsync(string randomName);
    }
}
