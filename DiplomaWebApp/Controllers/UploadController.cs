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
                //randomDirName - рандомное уникальное имя директории для этой операции
                //randomDirPath - путь к уникальной директории для этой операции
                //randomNameInServerBeforeConverter - имя файла на сервере до конвертирования

                string randomDirName;
                string randomDirPath;
                string randomNameInServerBeforeConverter;

                //Определяем рандомное уникальное имя директории для этой операции
                do
                {
                    randomDirName = Guid.NewGuid().ToString();
                    randomDirPath = _appEnvironment.WebRootPath + "/files/" + randomDirName;
                } while (Directory.Exists(randomDirPath));
                Directory.CreateDirectory(randomDirPath);
                randomDirPath += "/";

                //Определяем имя файла на сервере до конвертирования
                if (uploadedFile.FileName.Contains(".wav"))
                    randomNameInServerBeforeConverter = String.Format(@"{0}.wav", Guid.NewGuid());
                else if (uploadedFile.FileName.Contains(".mp3"))
                    randomNameInServerBeforeConverter = String.Format(@"{0}.mp3", Guid.NewGuid());
                else
                    return RedirectToAction("Index", "Upload", new { errors = "Неправильное расширение файла" });

                //Определяем путь к файлу до конвертирования
                string pathToFileInServerBeforeConverter = randomDirPath + randomNameInServerBeforeConverter;

                // сохраняем файл в папку files в каталоге wwwroot
                using (var fileStream = new FileStream(pathToFileInServerBeforeConverter, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                // Конвертирование
                string pathToWorkingDirectory = _appEnvironment.WebRootPath + "/python/";
                string pathToMainPy = pathToWorkingDirectory + "converter.py";

                string lineArgs = randomDirPath + "*" + randomNameInServerBeforeConverter + "*" + mode;
                RunCmdResult result = new RunCmd().Run(pathToMainPy, lineArgs);
                string[] results = result.Result.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (results[0] is null)
                    return RedirectToAction("Index", "Upload", new { errors = "There is error in converter`s work" });
                if (results[0] != "OK")
                    return RedirectToAction("Index", "Upload", new { errors = results[1] });

                return RedirectToAction("Info", "Upload", new { dirName = randomDirName });
            }
            else
            {
                return RedirectToAction("Index", new { errors = "Недопустимый формат файла." });
            }
        }

        public IActionResult Info(string dirName)
        {
            //ViewModel
            InfoViewModel infoViewModel = new InfoViewModel();

            //dirName - рандомное уникальное имя директории для этой операции
            //randomDirPath - путь к уникальной директории для этой операции
            string randomDirPath = _appEnvironment.WebRootPath + "/files/" + dirName;

            if (!Directory.Exists(randomDirPath))
            {
                return RedirectToAction("FileNotFound");
            }

            string pathToWorkingDirectory = _appEnvironment.WebRootPath + "/python/";
            string pathToMainPy = pathToWorkingDirectory + "predictor.py";

            string[] filePaths = Directory.GetFiles(randomDirPath);
            List<RunCmdResult> results = new List<RunCmdResult>();
            string errors = null;

            foreach (string filePath in filePaths)
            {
                //Запуск анализатора
                RunCmdResult result = new RunCmd().Run(pathToMainPy, filePath);
                result.Result = result.Result.Replace("[[", "");
                result.Result = result.Result.Replace("]]", "");
                result.Result = result.Result.Replace("\n", "");
                result.Result = result.Result.Replace("\r", "");

                result.Result = result.Result.Replace("<", "");//TODO

                if (result.Errors is string || result.Errors.Length > 1)
                    errors = result.Errors;

                results.Add(result);
            }

            //Удаление папки
            Directory.Delete(randomDirPath, true);

            //Создаем все коллекции
            infoViewModel.Predictions = new Dictionary<string, List<double>>();
            infoViewModel.ListOfMaxIndexes = new List<int>();
            infoViewModel.Errors = errors;
            infoViewModel.ResultPredictions = new Dictionary<int, double>();

            foreach (RunCmdResult result in results)
            {
                //Парсим результат предсказания
                string[] predictionsByModels = result.Result.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                if (predictionsByModels.Length < 1)
                    return View(infoViewModel);

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

                    //Массив предсказаний в даблах
                    List<double> localPredictions = new List<double>();
                    //Сумма всех предсказаний для перевода в %
                    double sumOfLocalPredictions = 0;

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

                    //Переводим чсила в %
                    for (int i = 0; i < localPredictions.Count; i++)
                    {
                        localPredictions[i] = (localPredictions[i] / sumOfLocalPredictions) * 100;
                    }

                    //Добаляем все это в итог
                    if (infoViewModel.Predictions.ContainsKey(nameAndPredictionsOfModel[0]))
                    {
                        for(int i = 0; i < localPredictions.Count; i++)
                        {
                            infoViewModel.Predictions[nameAndPredictionsOfModel[0]][i] += localPredictions[i];
                        }
                    }
                    else
                    {
                        infoViewModel.Predictions.Add(nameAndPredictionsOfModel[0], localPredictions);
                    }
                }
            }

            //Усредененные глобальные предсказания
            List<double> globalPredictions = new List<double>();
            double sumOfGlobalPredictions = 0;
            int maxPredictionIndex = 0;

            foreach (List<double> predictions in infoViewModel.Predictions.Values)
            {
                //Делем % на количесво частей файла и ищем максимум
                maxPredictionIndex = 0;
                for (int i = 0; i < predictions.Count; i++)
                {
                    if (globalPredictions.Count < (i + 1))
                        globalPredictions.Add(0);

                    predictions[i] = predictions[i] / results.Count;
                    if (predictions[i] > predictions[maxPredictionIndex])
                        maxPredictionIndex = i;

                    globalPredictions[i] += predictions[i];
                    sumOfGlobalPredictions += predictions[i];
                }
                //Добаляем все это в итог
                infoViewModel.ListOfMaxIndexes.Add(maxPredictionIndex);
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
