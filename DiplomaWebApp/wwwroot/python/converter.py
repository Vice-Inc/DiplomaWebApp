# Программа принимает на вход файл в формате .mp3 или .wav
# На выходе ожидается ошибка "ERROR Текст ошибки"
# Или OK с последующим списком треков для дальнейшего анализирования
# с помощью predictor.py (OK 0.wav 1.wav 2.wav)

if __name__ == "__main__":

	import os
	import librosa
	import subprocess
	from subprocess import STDOUT
	from pydub import AudioSegment
	import sys

	# Проверка аргументов
	if len(sys.argv) != 2:
		print('ERROR There must be 2 args')
		exit(0)

	args = sys.argv[1].split("*")

	# Проверка есть ли уникальная папка пользователя
	if len(args) < 1:
		print('ERROR There is no unique directory name')
		exit(0)

	# Проверка есть ли файл для конвертирования
	if len(args) < 2:
		print('ERROR There is no file to convert')
		exit(0)

	# Проверка указан ли тип детали
	if len(args) < 3:
		print('ERROR There is no type')
		exit(0)

	# Если параметров больше чем надо, то все плохо
	if len(args) > 3:
		print('ERROR There are too mach arguments')
		exit(0)

	# Путь к рабочей директории
	pathToWorkingDirectory = args[0] + '/'

	# Получаем путь к музыке
	pathToMusic = pathToWorkingDirectory + args[1]

	# Раздельно имя и расширение файла
	filename, file_extension = os.path.splitext(pathToMusic)

	# Реальное имя файла
	filename = args[1].replace(file_extension, '')

	# Получаем тип детали
	partType = args[2]



	# Проверка на необходимые расширения файла
	if file_extension != '.mp3' and file_extension != '.wav':
		print('ERROR Bad type of file')
		exit(0)

	# Если нужно, то конвертируем входной файл
	pathToWavMusic = 'none'
	if file_extension == '.mp3':
		pathToWavMusic = pathToWorkingDirectory + filename + '-wav' + '.wav'
		sound = AudioSegment.from_mp3(pathToMusic)
		sound.export(pathToWavMusic, format="wav")
		file_extension = '.wav'
	if pathToWavMusic != 'none':
		os.remove(pathToMusic)
		pathToMusic = pathToWorkingDirectory + filename + file_extension
		os.rename(pathToWavMusic, pathToMusic)

	# Длительность трека
	duration = librosa.get_duration(filename=pathToMusic)

	# Если трек короче 30 сек
	if duration < 30:
		print('ERROR Too short file')
		exit(0)

	# Собираем список точек, начиная с которых будем брать по 30 сек трека
	startsOfParts = []
	shortParts = []
	countOfParts = int(duration // 30)
	for i in range(countOfParts):
		startsOfParts.append(i * 30)

	# Если список точек не пуст, то режем
	if len(startsOfParts) > 0:
		DEVNULL = open(os.devnull, 'wb')

		# Для кажой точки нарезаем и добавляем в список
		for time in startsOfParts:
			pathTo30SecMusic = pathToWorkingDirectory + str(time) + file_extension
			shortParts.append(pathTo30SecMusic)

			# ffmpeg -ss 70 -i Ch.mp3 -t 30 result.mp3 - 30 сек начиная с 70й секунды
			process = subprocess.Popen(f'ffmpeg -ss {time} -i {pathToMusic} -t 30 {pathTo30SecMusic}', stdout=DEVNULL, stderr=STDOUT)

			process.wait()

		DEVNULL.close()
		os.remove(pathToMusic)
	else:  # Если список пуст, то сам файл переименовывается и добавляется в список
		pathTo30SecMusic = pathToWorkingDirectory + "0" + file_extension
		shortParts.append(pathTo30SecMusic)
		os.rename(pathToMusic, pathTo30SecMusic)

	result = ''
	for part in shortParts:
		result += ' '
		result += part

	print('OK' + result)