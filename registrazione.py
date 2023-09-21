import os
from deepface import DeepFace
from flask import Flask, request

# dependency configuration
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "2"

app = Flask(__name__)

@app.route('/api/verifica', methods=['POST'])
def verifica():
    try: 
        data = request.get_json()  # Ottiene l'oggetto JSON dalla richiesta
        path = data.get("imagePath")  # Estrae il percorso dal campo "imagePath" dell'oggetto JSON

        dfs = DeepFace.find(img_path=path, db_path="./wwwroot/imgs")

        if len(dfs) == 0:
            return "Non trovato"
        else: 
            first_dataframe = dfs[0]
            if "identity" in first_dataframe:
                identity = str(first_dataframe["identity"][0])
                last_slash_index = identity.rfind("/")
                last_dot_index = identity.rfind(".")

                # Estrai la parte della stringa tra l'ultimo slash e l'ultimo punto
                return identity[last_slash_index + 1 : last_dot_index]
                    
            else:
                return "Attributo 'identity' non trovato nel primo DataFrame"
            
    except Exception as e:
        return str(e)

if __name__ == "__main__":
     app.run(debug=True)