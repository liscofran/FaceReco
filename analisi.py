import threading
import cv2
from deepface import DeepFace
import asyncio
import websockets

server_closed = False
name = None
face_match = False

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
                name = identity[last_slash_index - 10 : last_slash_index]

                
    except ValueError:
        face_match = False
        name = None
   
def video_stream():
    global name
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

# Funzione per avviare il server WebSocket
async def start_websocket_server():
    global server_closed
    global name

    async def server(websocket, path):
        # Questa funzione verrà chiamata ogni volta che viene stabilita una connessione
        print(f"Connessione stabilita da {websocket.remote_address}")

        if name is not None:
            await websocket.send(name)

    # Avvia il server WebSocket sulla tua macchina
    server = await websockets.serve(server, "localhost", 8765)

    # Loop di eventi per gestire le connessioni WebSocket
    while not server_closed:
        await asyncio.sleep(1)

# Funzione per chiudere il server WebSocket in modo pulito
def close_websocket_server():
    global server_closed
    server_closed = True

# Creare un nuovo loop degli eventi per il thread principale
loop = asyncio.get_event_loop()

# Avvia il server WebSocket nel thread principale
if __name__ == "__main__":
    threading.Thread(target=video_stream).start()
    websocket_thread = threading.Thread(target=start_websocket_server)
    websocket_thread.start()

    # Puoi chiudere il server WebSocket in modo pulito quando lo desideri, ad esempio dopo un certo periodo di tempo
    # Qui sto impostando un timer per chiudere il server dopo 10 secondi, ma puoi gestirlo come desideri.
    timer_thread = threading.Timer(1000000000000, close_websocket_server)
    timer_thread.start()

    loop.run_until_complete(start_websocket_server())