import threading
import os
import cv2
import json
from deepface import DeepFace
import asyncio
import websockets
import datetime
import time
import mysql.connector


server_closed = False
name = None
face_match = False
timestamp = []
db_connection = None

def check_face(frame):
    global face_match
    global name

    try:
        dfs = DeepFace.find(frame, db_path="./wwwroot/imgs")
        if len(dfs) == 0:
            face_match = False
            name = None
            
        else:
            face_match = True
            first_dataframe = dfs[0]
            if "identity" in first_dataframe:
                identity = str(first_dataframe["identity"][0])
                last_slash_index = identity.rfind("/")
                user_id = identity[last_slash_index - 10 : last_slash_index]
                name = get_nome_by_id(db_connection, user_id)
                
    except ValueError:
        face_match = False
        name = None

# Funzione per eseguire la query
def get_nome_by_id(db_connection, user_id):
    query = "SELECT nome FROM FaceUser WHERE Id = %s"
    cursor = db_connection.cursor()

    try:
        cursor.execute(query, (user_id,))
        result = cursor.fetchone()
        if result:
            return result[0]  # Ritorna il nome se trovato
        else:
            return None  # Ritorna None se l'ID non esiste
    except mysql.connector.Error as err:
        print(f"Errore nella query: {err}")
        return None
    finally:
        cursor.close()

# Connessione al database
def connect_to_db():
    try:
        db_connection = mysql.connector.connect(
            host="127.0.0.1",  # Sostituisci con l'indirizzo del tuo database
            user="FaceUser",   # Sostituisci con il tuo nome utente
            password="Useruser!",  # Sostituisci con la tua password
            database="facedb"  # Sostituisci con il nome del tuo database
        )
        return db_connection
    except mysql.connector.Error as err:
        print(f"Errore nella connessione al database: {err}")
        return None

   
def video_stream():
    global name
    global timestamp
    cap = cv2.VideoCapture(0)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

    counter = 0

    while True:
        ret, frame = cap.read()

        if ret:
            if counter % 150 == 0:
                try:
                    threading.Thread(target=check_face, args=(frame.copy(),)).start()
                except ValueError:
                    pass

            counter += 1

            if face_match:
                cv2.putText(frame, "Match", (20, 450), cv2.FONT_HERSHEY_SIMPLEX, 2, (0, 255, 0))
            else:
                cv2.putText(frame, "No Match", (20, 450), cv2.FONT_HERSHEY_SIMPLEX, 2, (0, 0, 255))
                name = None

            cv2.imshow("video", frame)

        key = cv2.waitKey(1)
        if key == ord("q"):
            break

    cap.release()
    cv2.destroyAllWindows()
    write_timestamp()
    close_websocket_server()
    db_connection.close()

# Funzione per avviare il server WebSocket
async def start_websocket_server():
    global server_closed
    global name

    async def server(websocket, path):
        # Questa funzione verrà chiamata ogni volta che viene stabilita una connessione
        print(f"Connessione stabilita da {websocket.remote_address}")

        if name is not None:
            # Inviare la stringa JSON tramite WebSocket
            # Creare un dizionario con il tipo di dato e il valore
            message_data = {
                "name": name,
                "timestamp": timestamp_now("orario")
            }

            timestamp.append(timestamp_now("orario") + " " + name)

            # Convertire il dizionario in una stringa JSON
            message_json = json.dumps(message_data)
            await websocket.send(message_json)
        else:
            message_data = {
                "name": "nessun volto riconosciuto",
                "timestamp": timestamp_now("orario")
            }

            timestamp.append(timestamp_now("orario") + " nessun volto riconosciuto")
            
            # Convertire il dizionario in una stringa JSON
            message_json = json.dumps(message_data)
            await websocket.send(message_json)

        message = await websocket.recv()
        message_data = json.loads(message)

        if "action" in message_data and message_data["action"] == "closeConnection":
                print(f"Ricevuto messaggio di chiusura da {websocket.remote_address}")
                await websocket.close()
            
        time.sleep(5)


    # Avvia il server WebSocket sulla tua macchina
    server = await websockets.serve(server, "localhost", 8765)

    # Loop di eventi per gestire le connessioni WebSocket
    while not server_closed:
        await asyncio.sleep(1)

# Funzione per chiudere il server WebSocket in modo pulito
def close_websocket_server():
    global server_closed
    server_closed = True

def timestamp_now(formato):

    now = datetime.datetime.now()

    if(formato == "orario"):
        # Estrai ora, minuti e secondi
        ora = now.hour
        minuti = now.minute
        secondi = now.second

        # Formatta il timestamp come "ora:minuti:secondi"
        return f"{ora:02d}:{minuti:02d}:{secondi:02d}"
    
    else:
        # Estrai anno, mese e giorno      
        giorno = now.day
        mese = now.month
        anno = now.year

        # Formatta il timestamp come "ora:minuti:secondi"
        return f"{anno:02d}_{mese:02d}_{giorno:02d}"
    
def write_timestamp():
    global timestamp
    # Specifica il nome del file di output
    nome_file = timestamp_now("giorno") + '.txt'
    path = './wwwroot/timestamps/'

    # Verifica se il file esiste nella directory
    file_esiste = os.path.isfile(os.path.join(path, nome_file))

    # Apre il file in modalità scrittura o append a seconda della sua esistenza
    mode = 'a' if file_esiste else 'w'
    
    with open(os.path.join(path, nome_file), mode) as file:
        # Itera attraverso la lista degli elementi
        for elemento in timestamp:
            # Scrive l'elemento nel file e aggiunge un carattere di nuova linea
            file.write(elemento + '\n')

# Creare un nuovo loop degli eventi per il thread principale
loop = asyncio.get_event_loop()

# Avvia il server WebSocket nel thread principale
if __name__ == "__main__":
    threading.Thread(target=video_stream).start()
    db_connection = connect_to_db()
    websocket_thread = threading.Thread(target=start_websocket_server)
    websocket_thread.start()

    loop.run_until_complete(start_websocket_server())