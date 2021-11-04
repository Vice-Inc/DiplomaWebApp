if __name__ == "__main__":

	import os
	import librosa
	import subprocess
	from subprocess import STDOUT
	from pydub import AudioSegment
	import sys

	if (len(sys.argv) < 2):
		print('ERROR There is no file to convert')
		exit(0)

	pathToMusic = sys.argv[1]

	filename, file_extension = os.path.splitext(pathToMusic)

	if(file_extension != '.mp3' and file_extension != '.wav'):
		print('ERROR Bad type of file')
		exit(0)

	duration = librosa.get_duration(filename=pathToMusic)

	pathTo30SecMusic = 'none'
	if(duration > 30):
		pathTo30SecMusic = filename + '-30' + file_extension
		DEVNULL = open(os.devnull, 'wb')
		process = subprocess.Popen(f'ffmpeg -t 30 -i {pathToMusic} -acodec copy {pathTo30SecMusic}', stdout=DEVNULL, stderr=STDOUT)# shell=True
		process.wait()
		DEVNULL.close()

	if(pathTo30SecMusic != 'none'):
		os.remove(pathToMusic)
		os.rename(pathTo30SecMusic, pathToMusic)

	pathToWavMusic = 'none'
	if(file_extension == '.mp3'):
		pathToWavMusic = filename + '-wav' + '.wav'
		sound = AudioSegment.from_mp3(pathToMusic)
		sound.export(pathToWavMusic, format="wav")

	if(pathToWavMusic != 'none'):
		os.remove(pathToMusic)
		pathToMusic = filename + '.wav'
		os.rename(pathToWavMusic, pathToMusic)

	print('OK ' + pathToMusic)