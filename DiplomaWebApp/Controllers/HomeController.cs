using DiplomaWebApp.Data.Interfaces;
using DiplomaWebApp.Models;
using DiplomaWebApp.Models.ViewModels;
using DiplomaWebApp.Python;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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

        [Route("Home/Error")]
        [Route("Home/Error/{errors}")]
        public IActionResult Error(string errors)
        {
            return View("Error", errors);
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
            string pathToMainPy = pathToWorkingDirectory + "predictor.py";

            RunCmdResult result = new RunCmd().Run(pathToMainPy, file.Path);
            result.Result = result.Result.Replace("[[", "");
            result.Result = result.Result.Replace("]]", "");
            result.Result = result.Result.Replace("\n", "");
            result.Result = result.Result.Replace("\r", "");

            await _filesService.DeleteAsync(file.Id);
            System.IO.File.Delete(file.Path);

            infoViewModel.Predictions = new Dictionary<string, List<double>>();
            infoViewModel.ListOfMaxIndexes = new List<int>();
            infoViewModel.Errors = result.Errors;

            string[] predictionsByModels = result.Result.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

            if(predictionsByModels.Length < 1)
                return View(infoViewModel);

            List<double> globalPredictions = new List<double>();
            double sumOfGlobalPredictions = 0;
            int maxPredictionIndex = 0;

            foreach (string predictionsByModel in predictionsByModels)
            {
                string[] nameAndPredictionsOfModel = predictionsByModel.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

                if (nameAndPredictionsOfModel.Length != 2)
                    continue;

                string[] predictions = nameAndPredictionsOfModel[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (predictions.Length != 10)
                    continue;

                double sumOfLocalPredictions = 0;

                List<double> localPredictions = new List<double>();

                foreach (string prediction in predictions)
                {
                    decimal predictionDecimal;
                    double predictionDouble;
                    if (!decimal.TryParse(prediction, NumberStyles.Any, CultureInfo.InvariantCulture, out predictionDecimal))
                        return View(infoViewModel);

                    predictionDouble = decimal.ToDouble(predictionDecimal);
                    sumOfLocalPredictions += predictionDouble;

                    localPredictions.Add(predictionDouble);
                }

                maxPredictionIndex = 0;
                for (int i = 0; i < localPredictions.Count; i++)
                {
                    if (globalPredictions.Count < (i + 1))
                        globalPredictions.Add(0);

                    globalPredictions[i] += localPredictions[i];
                    sumOfGlobalPredictions += localPredictions[i];

                    localPredictions[i] = (localPredictions[i] / sumOfLocalPredictions) * 100;

                    if (localPredictions[i] > localPredictions[maxPredictionIndex])
                        maxPredictionIndex = i;
                }

                infoViewModel.ListOfMaxIndexes.Add(maxPredictionIndex);
                infoViewModel.Predictions.Add(nameAndPredictionsOfModel[0], localPredictions);
            }

            maxPredictionIndex = 0;
            for (int i = 0; i < globalPredictions.Count; i++)
            {
                globalPredictions[i] = (globalPredictions[i] / sumOfGlobalPredictions) * 100;

                if (globalPredictions[i] > globalPredictions[maxPredictionIndex])
                    maxPredictionIndex = i;
            }

            infoViewModel.ListOfMaxIndexes.Add(maxPredictionIndex);
            infoViewModel.Predictions.Add("Учитывая все результаты", globalPredictions);

            return View(infoViewModel);
        }

        public IActionResult FileNotFound()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddFile(IFormFile uploadedFile)
        {
            if (uploadedFile != null && (uploadedFile.FileName.Contains(".wav") || uploadedFile.FileName.Contains(".mp3")))
            {
                //uploadedFile.FileName - реальное имя файла
                //randomNameInServerBeforeConverter - имя файла на сервере до конвертирования
                //randomNameInServerAfterConverter - имя файла на сервере после конвертирования
                string randomNameInServerBeforeConverter;

                if (uploadedFile.FileName.Contains(".wav"))
                    randomNameInServerBeforeConverter = String.Format(@"{0}.wav", Guid.NewGuid());
                else if (uploadedFile.FileName.Contains(".mp3"))
                    randomNameInServerBeforeConverter = String.Format(@"{0}.mp3", Guid.NewGuid());
                else
                    return RedirectToAction("Error", "Home", new { errors = "Неправильное расширение файла" });

                string pathToFileInServerBeforeConverter = _appEnvironment.WebRootPath + "/files/" + randomNameInServerBeforeConverter;
                string pathToFileInServerAfterConverter;

                // сохраняем файл в папку files в каталоге wwwroot
                using (var fileStream = new FileStream(pathToFileInServerBeforeConverter, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                // Конвертирование
                string pathToWorkingDirectory = _appEnvironment.WebRootPath + "/python/";
                string pathToMainPy = pathToWorkingDirectory + "converter.py";

                RunCmdResult result = new RunCmd().Run(pathToMainPy, pathToFileInServerBeforeConverter);
                string[] results = result.Result.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (results.Length != 2)
                    return RedirectToAction("Error", new {errors = "Ошибка в работе конвертера" });
                if (results[0] != "OK")
                    return RedirectToAction("Error", "Home", new { errors = results[1] });
                pathToFileInServerAfterConverter = results[1].Trim(new char[] { '\n', '\r'});

                // Создание моледи
                string randomNameInServer = pathToFileInServerAfterConverter.Substring(pathToFileInServerAfterConverter.IndexOf("/files/") + 7);
                FileModel file = new FileModel { Name = uploadedFile.FileName, Path = pathToFileInServerAfterConverter, RandomNameInServer = randomNameInServer };

                // Сохранение в БД
                await _filesService.AddAsync(file);
                file = await _filesService.GetByRandomNameAsync(randomNameInServer);

                return RedirectToAction("Info", "Home", new { id = file.Id });
            }

            return RedirectToAction("Index");
        }
    }
}
