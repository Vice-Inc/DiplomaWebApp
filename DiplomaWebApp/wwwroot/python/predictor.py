if __name__ == "__main__":

    import os
    from os import path

    os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

    import tensorflow as tf
    tf.get_logger().setLevel('ERROR')

    import librosa
    import numpy as np
    import csv
    import pandas as pd
    from sklearn.preprocessing import StandardScaler
    import keras
    import shutil
    import sys

    # Проверка передан ли путь к файлу
    if len(sys.argv) < 2:
        print('ERROR There is no path to file')
        exit(0)

    # Если параметров больше чем надо, то все плохо
    if len(sys.argv) > 2:
        print('ERROR There are too mach arguments')
        exit(0)

    pathToMusic = sys.argv[1]
    pathToPyDirectory = sys.argv[0].replace("predictor.py","")
    pathToCurrentDirectory = os.path.dirname(pathToMusic) + '/'

    if path.exists(pathToCurrentDirectory + 'dataset_input.csv'):
        os.remove(pathToCurrentDirectory + 'dataset_input.csv')
    shutil.copy2(pathToPyDirectory + 'dataset.csv', pathToCurrentDirectory + 'dataset_input.csv')

    g = 'none'
    songname = 'song'
    y, sr = librosa.load(pathToMusic, mono=True, duration=30)
    rmse = librosa.feature.rms(y=y)
    chroma_stft = librosa.feature.chroma_stft(y=y, sr=sr)
    spec_cent = librosa.feature.spectral_centroid(y=y, sr=sr)
    spec_bw = librosa.feature.spectral_bandwidth(y=y, sr=sr)
    rolloff = librosa.feature.spectral_rolloff(y=y, sr=sr)
    zcr = librosa.feature.zero_crossing_rate(y)
    mfcc = librosa.feature.mfcc(y=y, sr=sr)
    to_append = f'{songname} {np.mean(chroma_stft)} {np.mean(rmse)} {np.mean(spec_cent)} {np.mean(spec_bw)} {np.mean(rolloff)} {np.mean(zcr)}'
    for e in mfcc:
        to_append += f' {np.mean(e)}'
    to_append += f' {g}'

    file = open(pathToCurrentDirectory + 'dataset_input.csv', 'a', newline='')
    with file:
        writer = csv.writer(file)
        writer.writerow(to_append.split())

    data = pd.read_csv(pathToCurrentDirectory + 'dataset_input.csv')
    data.head()
    data = data.drop(['filename'], axis=1)
    scaler = StandardScaler()
    X = scaler.fit_transform(np.array(data.iloc[:, :-1], dtype=float))
    X_unknown = np.array([X[-1]])

    os.remove(pathToCurrentDirectory + 'dataset_input.csv')

    pathToModels = pathToPyDirectory + 'models/'

    for dirname in os.listdir(pathToModels):
        pathToDir = pathToModels + dirname + '/'
        if not ('.' in dirname):
            for modelName in os.listdir(pathToDir):
                pathToModel = pathToDir + modelName
                model = keras.models.load_model(pathToModel)
                print(f'{modelName}*')
                predictions = model.predict(X_unknown)
                print(predictions)
                print('#')
