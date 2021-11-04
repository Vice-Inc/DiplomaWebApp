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

    if (len(sys.argv) < 2):
        print('There is no file to predict')
        exit(0)

    pathToWorkingDirectory = sys.argv[0].replace("predictor.py","")
    pathToMusic = sys.argv[1]

    if path.exists(pathToWorkingDirectory + 'dataset_input.csv'):
        os.remove(pathToWorkingDirectory + 'dataset_input.csv')
    shutil.copy2(pathToWorkingDirectory + 'dataset.csv', pathToWorkingDirectory + 'dataset_input.csv')

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

    file = open(pathToWorkingDirectory + 'dataset_input.csv', 'a', newline='')
    with file:
        writer = csv.writer(file)
        writer.writerow(to_append.split())

    data = pd.read_csv(pathToWorkingDirectory + 'dataset_input.csv')
    data.head()
    data = data.drop(['filename'], axis=1)
    scaler = StandardScaler()
    X = scaler.fit_transform(np.array(data.iloc[:, :-1], dtype=float))
    X_unknown = np.array([X[-1]])

    os.remove(pathToWorkingDirectory + 'dataset_input.csv')

    model = keras.models.load_model(pathToWorkingDirectory + 'my_model-66.h5')
    print('my_model-66*', end="")
    predictions = model.predict(X_unknown)
    print(predictions)
    print('#')

    model = keras.models.load_model(pathToWorkingDirectory + 'my_model-68.h5')
    print('my_model-68*', end="")
    predictions = model.predict(X_unknown)
    print(predictions)
    print('#')

    model = keras.models.load_model(pathToWorkingDirectory + 'my_model-69.h5')
    print('my_model-69*', end="")
    predictions = model.predict(X_unknown)
    print(predictions)
    print('#')

    model = keras.models.load_model(pathToWorkingDirectory + 'my_model-69-1.h5')
    print('my_model-69-1*')
    predictions = model.predict(X_unknown)
    print(predictions)
    print('#')

    model = keras.models.load_model(pathToWorkingDirectory + 'my_model-70.h5')
    print('my_model-70*')
    predictions = model.predict(X_unknown)
    print(predictions)
    print('#')

    model = keras.models.load_model(pathToWorkingDirectory + 'my_model-71.h5')
    print('my_model-71*')
    predictions = model.predict(X_unknown)
    print(predictions)
    print('#')

    model = keras.models.load_model(pathToWorkingDirectory + 'my_model-74.h5')
    print('my_model-74*')
    predictions = model.predict(X_unknown)
    print(predictions)
