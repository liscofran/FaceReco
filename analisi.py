from flask import Flask, request, jsonify
import cv2
import numpy as np
from deepface import DeepFace
import os

# dependency configuration
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "2"

app = Flask(__name__)

@app.route('/api/analisi', methods=['POST'])
def analisi():
    try:
        img_data = request.data
        img_np = np.frombuffer(img_data, dtype=np.uint8)
        img = cv2.imdecode(img_np, cv2.IMREAD_COLOR)
        
        if img is None:
            return "Errore nella decodifica dell'immagine"
        
        dfs = DeepFace.find(img_path=img, db_path="./wwwroot/imgs")

        print(dfs)

        for df in dfs:
            print(df)

        for df in dfs:
            if "identity" in df:
                print("Identity found:", df["identity"])

        if len(dfs) == 0:
            return "Non trovato"
        else: 
            first_dataframe = dfs[0]
            if "identity" in first_dataframe:
                identity = first_dataframe["identity"]
                return str(identity)  # Restituisce il valore dell'identità come stringa
            else:
                return "Attributo 'identity' non trovato nel primo DataFrame"
            
    except Exception as e:
        return str(e)

if __name__ == '__main__':
    app.run(debug=True)
    #analisi()