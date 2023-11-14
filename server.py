import asyncio
import websockets
import os
import json
from deepface import DeepFace
import asyncio
import websockets
import datetime
from PIL import Image
import io
import mysql.connector
import string
import random

db_connection = None

address = "192.168.1.89"
port_camera = 8765
port_log = 8766
port_reg = 8767

models = ['VGG-Face', 'Facenet', 'OpenFace', 'DeepFace', 'DeepID', 'Dlib', 'ArcFace']
metrics = ['cosine', 'euclidean', 'euclidean_l2']

tenant_ids= ["1579a3f2gB","7GJ8i9t5e3","38dyJue7oP","U8d5Y43Kin"]
connection_id = [0,0,0,1]
client_modes = [False,False,False,False]
client_streaming = [False,False,False,False]
messages = [None,None,None,None]
timestamps = [[],[],[],[]]
latest_frames = [None,None,None,None]

dictionary = {
  "tenants": tenant_ids,
  "connection_ids": connection_id,
  "multiple_faces": client_modes,
  "last_frame_available": client_streaming,
  "mess": messages,
  "timestamps": timestamps,
  "latest_frame": latest_frames
}

# Connessione al database
def connect_to_db():
    global db_connection
    try:
        db_connection = mysql.connector.connect(
            host="127.0.0.1", 
            user="FaceUser",   
            password="Useruser!",  
            database="facedb"  
        )
        return db_connection
    except mysql.connector.Error as err:
        print(f"Errore nella connessione al database: {err}")
        return None

#Ottieni il nome dell'utente, conoscendo il suo id (necessita che prima sia stata effettuata la connessione al db)
def get_nome_by_id(user_id):

    global db_connection

    query = "SELECT nome FROM FaceUser WHERE Id = %s"
    cursor = db_connection.cursor()

    try:
        cursor.execute(query, (user_id,))
        result = cursor.fetchone()
        if result:
            return result[0]  
        else:
            return None  
    except mysql.connector.Error as err:
        print(f"Errore nella query: {err}")
        return None
    finally:
        cursor.close()

def register_user(name,img):
    global db_connection

    try:
        if db_connection is None:
            print("La connessione al database non è stata stabilita.")
            return

        user_id = ''.join(random.choices(string.ascii_letters + string.digits, k=10))

        cursor = db_connection.cursor()

        insert_query = "INSERT INTO faceuser (id, nome) VALUES (%s, %s)"
        data = (user_id, name)
        cursor.execute(insert_query, data)
        db_connection.commit()

        cursor.close()
        print(f"Utente registrato con successo con ID: {user_id}")
        
        directory = "./wwwroot/imgs/" + user_id + "/"
        os.makedirs(directory, exist_ok=True)
        file_path = os.path.join(directory, user_id + ".jpeg")
        img.save(file_path, format="JPEG")

        os.remove("./wwwroot/imgs/representations_vgg_face.pkl")
        DeepFace.find("./wwwroot/imgs/1234567890/1234567890.jpg", db_path="./wwwroot/imgs",  model_name = models[0], distance_metric = metrics[0])

    except mysql.connector.Error as err:
        print(f"Errore durante la registrazione dell'utente: {err}")

#Funzione che si occupa di riconoscere un solo volto per frame (Deepface)
def check_face(frame):

    global db_connection
    global models
    global metrics

    try:
        dfs = DeepFace.find(frame, db_path="./wwwroot/imgs",  model_name = models[0], distance_metric = metrics[0])
        if len(dfs) == 0:
            return None           
        else:
            first_dataframe = dfs[0]
            if first_dataframe.empty:
                return None
            if "identity" in first_dataframe:
                identity = str(first_dataframe["identity"][0])
                last_slash_index = identity.rfind("/")
                user_id = identity[last_slash_index - 10 : last_slash_index]
                return get_nome_by_id(user_id)          
    except ValueError:
        return "Errore"
    
#Funzione che si occupa di riconoscere un solo volto per frame (Deepface)
def check_multiple_faces(frame):

    global db_connection
    global models
    global metrics

    faces = []
  
    try:
        dfs = DeepFace.find(frame, db_path="./wwwroot/imgs",  model_name = models[0], distance_metric = metrics[0])
        if len(dfs) == 0:
            return None           
        else:
            for face in dfs:
                if face.empty:
                    faces.append("Volto non riconosciuto")               
                if "identity" in face:
                    identity = str(face["identity"][0])
                    last_slash_index = identity.rfind("/")
                    user_id = identity[last_slash_index - 10 : last_slash_index]
                    faces.append(get_nome_by_id(user_id))   
            return faces         
        
    except ValueError:
        return "Errore"
    
#Funzione che si occupa di generare il timestamp dell'orario attuale
def generate_timestamp(formato):

    now = datetime.datetime.now()

    if(formato == "orario"):
        ora = now.hour
        minuti = now.minute
        secondi = now.second

        return f"{ora:02d}:{minuti:02d}:{secondi:02d}"
    
    else:
        giorno = now.day
        mese = now.month
        anno = now.year

        return f"{anno:02d}_{mese:02d}_{giorno:02d}"

#Funzione che si occupa di scrivere i log delle persone che hanno effettuato l'accesso
def write_timestamp(j):

    global dictionary

    nome_file = generate_timestamp("giorno")+ "_" + dictionary["tenants"][j] + '.txt' 
    path = './wwwroot/timestamps/'

    file_esiste = os.path.isfile(os.path.join(path, nome_file))

    mode = 'a' if file_esiste else 'w'
    
    with open(os.path.join(path, nome_file), mode) as file:
        for elemento in dictionary["timestamps"][j]:
            file.write(elemento + '\n')

# Funzione per gestire i frame ricevuti dalla componente di acquisizione fotogrammi
async def handle_frame_camera(websocket, path):

    global dictionary
    global db_connection
    client_id = None
  
    try:
        while True:          
                frame_data = await websocket.recv()
                msg_json = json.loads(frame_data)
                client_id = msg_json["id"]
                img = msg_json["image"]

                if(client_id in dictionary["tenants"]):     
                    i = dictionary["tenants"].index(client_id)
                    await process_latest_frame(img, i)          
                else:
                    websocket.send("Connessione non autorizzata")

    except websockets.exceptions.ConnectionClosedError as e: 
        print(f"Connessione chiusa: {e}")
    except Exception as e:
        print(e)

# Funzione per processare il frame più recente ogni 3 secondi
async def process_latest_frame(frame, j):

    global dictionary
    directory = "./wwwroot/tmps/vrf.jpg"
    name = None
    if frame is not None:
        try:
            image_data = bytes(frame)
            image = Image.open(io.BytesIO(image_data))
            image = image.transpose(Image.FLIP_LEFT_RIGHT)

            #image.show()

            image.save(directory, format="JPEG")

            if dictionary["multiple_faces"][j]:
                names = check_multiple_faces(directory)
                if names != None and names != "Errore":
                    name = ';'.join(names)
            else:
                name = check_face(directory)

            # Elimina l'immagine dalla cartella
            os.remove(directory)

            if name == "Errore" or name == None:
                message_data = {
                    "name": "nessun volto riconosciuto",
                    "timestamp": generate_timestamp("orario"),
                    "tenant":dictionary["tenants"][j]
                }
                dictionary["timestamps"][j].append(generate_timestamp("orario") + " nessun volto riconosciuto")                    
                dictionary["mess"][j] = json.dumps(message_data)
                dictionary["last_frame_available"][j] = True
            else:
                message_data = {
                    "name": name,
                    "timestamp": generate_timestamp("orario"),
                    "tenant": dictionary["tenants"][j]
                }
                dictionary["timestamps"][j].append(generate_timestamp("orario") + " " + name)
                dictionary["mess"][j] = json.dumps(message_data)
                dictionary["last_frame_available"][j] = True      

        except Exception as e:
            print(e)

async def handle_connection_log(websocket, path):

    global dictionary

    while True:
        message = await websocket.recv()
        message_data = json.loads(message)
        client_id = message_data["id"]
        mode = message_data["mode"]          
        action = message_data["action"]

        if client_id in dictionary["tenants"]:
            
            if action == "closeConnection":
                print(f"Ricevuto messaggio di chiusura da {websocket.remote_address}")
                write_timestamp(k)
                await websocket.close() 
            
            if action == "Connection":

                k = dictionary["tenants"].index(client_id)
                conn_id = dictionary["connection_ids"][k]
                i = 0
                for ids in dictionary["connection_ids"]:
                    if conn_id == ids and dictionary["last_frame_available"][i]:
                        if mode == "multiple":
                            dictionary["multiple_faces"][k] = True
                            dictionary["multiple_faces"][i] = True
                        else:
                            dictionary["multiple_faces"][k] = False
                            dictionary["multiple_faces"][i] = False
                        await websocket.send(dictionary["mess"][i])
                        dictionary["last_frame_available"][i] = False                 
                    i += 1         
                                
async def handle_registration(websocket, path):

    global db_connection
    directory = "./wwwroot/tmps/img.jpg"

    try:
        print(f"Connessione stabilita da {websocket.remote_address}")
        
        msg = await websocket.recv()
        msg_json = json.loads(msg)
        client_id = msg_json["id"]
        image = msg_json["image"]
        field_value = msg_json["fieldValue"]

        if client_id in dictionary["tenants"]:

            #Gestisci l'immagine
            image = bytes(image)
            img = Image.open(io.BytesIO(image))
            img = img.rotate(90)

            img.show()

            img.save(directory, format="JPEG")
            name = check_face(directory)

            # Elimina l'immagine dalla cartella
            os.remove(directory)
            
            if name == None:    
                register_user(field_value,img)
                await websocket.send("2")
            elif name == "Errore":
                await websocket.send("3")
            else:
                await websocket.send(name)
        else:
            await websocket.send("1")

    except websockets.exceptions.ConnectionClosedError as e:
        print(f"Connessione chiusa: {e}")
    except Exception as e:
        print(e)

def start_websocket_server(function, address, port):

    if function == "camera":
        print("websocket riconoscimento avviata")
        return websockets.serve(handle_frame_camera, address, port, ping_timeout=None)
    elif function == "log":
        print("websocket log avviata")
        return websockets.serve(handle_connection_log, address, port, ping_timeout=None)
    else:
        print("websocket registrazione avviata")
        return websockets.serve(handle_registration, address, port, ping_timeout=None)

# Si occupa di avviare le Websocket e la connessione al Db
async def main():

    global db_connection
    if (db_connection == None):
        db_connection = connect_to_db() 

    await asyncio.gather(start_websocket_server("camera", address, port_camera),
    start_websocket_server("log", address, port_log),
    start_websocket_server("registration", address, port_reg))

if __name__ == "__main__":
    
    loop = asyncio.get_event_loop()
    loop.run_until_complete(main())
    try:
        loop.run_forever()
    except KeyboardInterrupt:
    
        print("Esecuzione interrotta manualmente. Salvataggio in corso...")
        db_connection.close()
        for timestamp in dictionary["timestamps"]:
            write_timestamp(dictionary["timestamps"].index(timestamp))