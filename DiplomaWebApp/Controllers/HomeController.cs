using DiplomaWebApp.Data.Interfaces;
using DiplomaWebApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiplomaWebApp.Controllers
{
    public class HomeController : Controller
    {
        IWebHostEnvironment _appEnvironment;
        IFilesService _filesService;

        public HomeController(IWebHostEnvironment appEnvironment, IFilesService filesService)
        {
            _filesService = filesService;
            _appEnvironment = appEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<FileModel> files = await _filesService.GetAllAsync();
            return View(files);
        }        
    }
}
