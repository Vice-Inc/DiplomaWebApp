using DiplomaWebApp.Data.Base;
using DiplomaWebApp.Data.Interfaces;
using DiplomaWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DiplomaWebApp.Data.Services
{
    public class FilesService : EntityBaseRepository<FileModel>, IFilesService
    {
        private AppDbContext _context;

        public FilesService(AppDbContext context) : base(context) 
        {
            _context = context;
        }

        public async Task<FileModel> GetByRandomNameAsync(string randomName)
        {
            return await _context.Set<FileModel>().FirstOrDefaultAsync(a => a.RandomNameInServer == randomName);
        }
    }
}
