
if __name__ == "__main__":

    import os
    os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

    import librosa
    import numpy as np
    import csv
    import pandas as pd
    from sklearn.preprocessing import StandardScaler
    import keras
    import shutil



    if os.path.exists('storage/music.mp3'):
        os.remove('dataset_input.csv')
    shutil.copy2('dataset.csv', 'dataset_input.csv')

    filename = 'classical.00000.wav'
    g = 'classical'
    songname = filename
    y, sr = librosa.load(songname, mono=True, duration=30)
    rmse = librosa.feature.rms(y=y)
    chroma_stft = librosa.feature.chroma_stft(y=y, sr=sr)
    spec_cent = librosa.feature.spectral_centroid(y=y, sr=sr)
    spec_bw = librosa.feature.spectral_bandwidth(y=y, sr=sr)
    rolloff = librosa.feature.spectral_rolloff(y=y, sr=sr)
    zcr = librosa.feature.zero_crossing_rate(y)
    mfcc = librosa.feature.mfcc(y=y, sr=sr)
    to_append = f'{filename} {np.mean(chroma_stft)} {np.mean(rmse)} {np.mean(spec_cent)} {np.mean(spec_bw)} {np.mean(rolloff)} {np.mean(zcr)}'
    for e in mfcc:
        to_append += f' {np.mean(e)}'
    to_append += f' {g}'

    file = open('dataset_input.csv', 'a', newline='')
    with file:
        writer = csv.writer(file)
        writer.writerow(to_append.split())

    data = pd.read_csv('dataset_input.csv')
    data.head()
    data = data.drop(['filename'], axis=1)
    scaler = StandardScaler()
    X = scaler.fit_transform(np.array(data.iloc[:, :-1], dtype=float))
    X_unknown = np.array([X[-1]])

    model = keras.models.load_model('my_model-69.h5')

    predictions = model.predict(X_unknown)
    print(predictions)
