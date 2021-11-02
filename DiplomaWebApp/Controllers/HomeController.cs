using DiplomaWebApp.Data.Interfaces;
using DiplomaWebApp.Models;
using DiplomaWebApp.Models.ViewModels;
using DiplomaWebApp.Python;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

        public async Task<IActionResult> Info(int id)
        {
            InfoViewModel infoViewModel = new InfoViewModel();

            FileModel file = await _filesService.GetByIdAsync(id);

            if (file is null)
            {
                return RedirectToAction("FileNotFound");
            }

            infoViewModel.FileModel = file;

            string pathToWorkingDirectory = _appEnvironment.WebRootPath + "/python/";
            string pathToMusic = _appEnvironment.WebRootPath + file.Path;
            string pathToMainPy = pathToWorkingDirectory + "main.py";


            RunCmdResult result = new RunCmd().Run(pathToMainPy, pathToMusic);
            result.Result = result.Result.Replace("[[", "");
            result.Result = result.Result.Replace("]]", "");

            infoViewModel.Predictions = new List<double>();
            infoViewModel.Errors = result.Errors;

            string[] predictions = result.Result.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (predictions.Length != 10)
                return View(infoViewModel);

            double sumOfPrediction = 0;

            foreach (string prediction in predictions)
            {
                decimal predictionDecimal;
                double predictionDouble;
                if (!decimal.TryParse(prediction, NumberStyles.Any, CultureInfo.InvariantCulture, out predictionDecimal))
                    return View(infoViewModel);

                predictionDouble = decimal.ToDouble(predictionDecimal);
                sumOfPrediction += predictionDouble;

                infoViewModel.Predictions.Add(predictionDouble);
            }

            for(int i = 0; i < infoViewModel.Predictions.Count; i++)
            {
                infoViewModel.Predictions[i] = (infoViewModel.Predictions[i] / sumOfPrediction) * 100;
            }

            await _filesService.DeleteAsync(file.Id);
            System.IO.File.Delete(pathToMusic);

            return View(infoViewModel);
        }

        public IActionResult FileNotFound()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddFile(IFormFile uploadedFile)
        {
            if (uploadedFile != null && uploadedFile.FileName.Contains(".wav"))
            {
                // путь к папке Files
                string randomNameInServer = String.Format(@"{0}.wav", Guid.NewGuid());
                string path = "/files/" + randomNameInServer;

                //uploadedFile.FileName - реальное имя файла
                //randomNameInServer - имя файла на сервере

                // сохраняем файл в папку Files в каталоге wwwroot
                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }
                FileModel file = new FileModel { Name = uploadedFile.FileName, Path = path, RandomNameInServer = randomNameInServer };

                await _filesService.AddAsync(file);
                file = await _filesService.GetByRandomNameAsync(randomNameInServer);

                return RedirectToAction("Info", "Home", new { id = file.Id });
            }

            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
