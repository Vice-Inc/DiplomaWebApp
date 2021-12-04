using DiplomaWebApp.Data.Interfaces;
using DiplomaWebApp.Data.MusicData;
using DiplomaWebApp.Models;
using DiplomaWebApp.Models.ViewModels;
using DiplomaWebApp.Python;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace DiplomaWebApp.Controllers
{
    public class UploadController : Controller
    {
        IWebHostEnvironment _appEnvironment;
        IFilesService _filesService;

        public UploadController(IWebHostEnvironment appEnvironment, IFilesService filesService)
        {
            _filesService = filesService;
            _appEnvironment = appEnvironment;
        }

        public IActionResult Index(string errors)
        {
            return View("Index", errors);
        }

        [HttpPost]
        public async Task<IActionResult> AddFile(IFormFile uploadedFile, string mode)
        {
            //Проверка на допустимый формат файла
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
                    return RedirectToAction("Index", "Upload", new { errors = "Неправильное расширение файла" });

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
                    return RedirectToAction("Index", new { errors = "Ошибка в работе конвертера" });
                if (results[0] != "OK")
                    return RedirectToAction("Index", "Upload", new { errors = results[1] });
                pathToFileInServerAfterConverter = results[1].Trim(new char[] { '\n', '\r' });

                // Создание моледи
                string randomNameInServer = pathToFileInServerAfterConverter.Substring(pathToFileInServerAfterConverter.IndexOf("/files/") + 7);
                FileModel file = new FileModel { Name = uploadedFile.FileName, Path = pathToFileInServerAfterConverter, RandomNameInServer = randomNameInServer };

                // Сохранение в БД
                await _filesService.AddAsync(file);
                file = await _filesService.GetByRandomNameAsync(randomNameInServer);

                return RedirectToAction("Info", "Upload", new { id = file.Id });
            }
            else
            {
                return RedirectToAction("Index", new { errors = "Недопустимый формат файла." });
            }
        }

        public async Task<IActionResult> Info(int id)
        {
            //ViewModel
            InfoViewModel infoViewModel = new InfoViewModel();

            //Получаем модель файла из бд
            FileModel file = await _filesService.GetByIdAsync(id);

            if (file is null)
            {
                return RedirectToAction("FileNotFound");
            }

            //Передаем в результат имя файла
            infoViewModel.FileModel = file;

            string pathToWorkingDirectory = _appEnvironment.WebRootPath + "/python/";
            string pathToMainPy = pathToWorkingDirectory + "predictor.py";

            //Запуск анализатора
            RunCmdResult result = new RunCmd().Run(pathToMainPy, file.Path);
            result.Result = result.Result.Replace("[[", "");
            result.Result = result.Result.Replace("]]", "");
            result.Result = result.Result.Replace("\n", "");
            result.Result = result.Result.Replace("\r", "");

            //Удаление модели файла из БД
            await _filesService.DeleteAsync(file.Id);
            System.IO.File.Delete(file.Path);

            //Создаем все коллекции
            infoViewModel.Predictions = new Dictionary<string, List<double>>();
            infoViewModel.ListOfMaxIndexes = new List<int>();
            infoViewModel.Errors = result.Errors;
            infoViewModel.ResultPredictions = new Dictionary<int, double>();

            //Парсим результат предсказания
            string[] predictionsByModels = result.Result.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

            if (predictionsByModels.Length < 1)
                return View(infoViewModel);

            //Усредененные предсказания
            List<double> globalPredictions = new List<double>();
            double sumOfGlobalPredictions = 0;
            int maxPredictionIndex = 0;

            //Для каждого предсказания
            foreach (string predictionsByModel in predictionsByModels)
            {
                //Разделяем строку на название и предсказания
                string[] nameAndPredictionsOfModel = predictionsByModel.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                if (nameAndPredictionsOfModel.Length != 2)
                    continue;

                //Парсим предсказания
                string[] predictions = nameAndPredictionsOfModel[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (predictions.Length != 10)
                    continue;

                //Сумма предсказаний для перевода в %
                double sumOfLocalPredictions = 0;
                //Массив предсказаний в даблах
                List<double> localPredictions = new List<double>();

                //Парсим каждую строку предсказания
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

                //Переводим чсила в % и ищем максимум
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

                //Добаляем все это в итог
                infoViewModel.ListOfMaxIndexes.Add(maxPredictionIndex);
                infoViewModel.Predictions.Add(nameAndPredictionsOfModel[0], localPredictions);
            }

            //Находим максимум для усредненных предсказаний
            maxPredictionIndex = 0;
            for (int i = 0; i < globalPredictions.Count; i++)
            {
                globalPredictions[i] = (globalPredictions[i] / sumOfGlobalPredictions) * 100;

                if (globalPredictions[i] > globalPredictions[maxPredictionIndex])
                    maxPredictionIndex = i;

                //Усредненные предсказания заносим в специальную коллекцию
                infoViewModel.ResultPredictions.Add(i, globalPredictions[i]);
            }

            //Добаляем в итог лучший жанр усредненных предсказаний
            infoViewModel.ResultGenre = (Genre)maxPredictionIndex;

            return View(infoViewModel);
        }

        public IActionResult FileNotFound()
        {
            return View();
        }
    }
}
