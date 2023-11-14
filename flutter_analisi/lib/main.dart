import 'package:flutter/material.dart';
import 'package:camera/camera.dart';
import 'package:web_socket_channel/web_socket_channel.dart';
import 'package:web_socket_channel/io.dart';
import 'package:image/image.dart' as imglib;
import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';
import 'dart:io';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:provider/provider.dart';

void main() async{
  WidgetsFlutterBinding.ensureInitialized(); 
  final prefs = await SharedPreferences.getInstance(); 
  runApp(
    ChangeNotifierProvider(
      create: (context) => CodeModel(prefs),
      child: MyApp(),
    ),
  );
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      home: HomePage(),
    );
  }
}

class CodeModel with ChangeNotifier {
  String _code = '';
  SharedPreferences? _prefs;

  CodeModel(SharedPreferences prefs) {
    _prefs = prefs;
    _loadCode();
  }

  Future<void> _loadCode() async {
    _code = _prefs!.getString('code') ?? '';
    notifyListeners();
  }

  set code(String newCode) {
    _code = newCode;
    notifyListeners();
    _saveCode(newCode);
  }

  Future<void> _saveCode(String newCode) async {
    await _prefs!.setString('code', newCode);
  }
}

class HomePage extends StatefulWidget {
  @override
  _HomePageState createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {

  TextEditingController _codeController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _codeController = TextEditingController();
  }

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final codeModel = Provider.of<CodeModel>(context, listen: false);

    _codeController.text = codeModel._code;

    return Scaffold(
      appBar: AppBar(
        title: Text('Home Page'),
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text('Client'),
              Padding(
              padding: const EdgeInsets.all(16.0),
              child: TextField(
                controller: _codeController,
                decoration: InputDecoration(
                  labelText: 'Inserisci il codice (max 10 caratteri)',
                ),
                maxLength: 10,
                onChanged: (value) {
                  if (value.length <= 10) {
                    codeModel.code = value; // Salva il valore nel modello condiviso
                  }
                },
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: Theme(
        data: Theme.of(context).copyWith(
          splashColor: Colors.transparent,
          highlightColor: Colors.transparent,
        ),
        child: BottomNavigationBar(
            selectedLabelStyle: TextStyle(fontSize: 14, color: Colors.blue),
            unselectedLabelStyle: TextStyle(fontSize: 14, color: Colors.blue), 
            items: [
              BottomNavigationBarItem(
                icon: Icon(Icons.pageview, size: 30, color: Colors.blue),
                label: 'Riconoscimento',
              ),
              BottomNavigationBarItem(
                icon: Icon(Icons.create, size: 30, color: Colors.blue), 
                label: 'Registrazione',
              ),
            ],
          onTap: (index) {
            if (index == 0) {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => Page1()),
              );
            } else if (index == 1) {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => Page2()),
              );
            }
          },
        ),
      ),
    );
  }
}

class Page1 extends StatefulWidget {
  @override
  _Page1State createState() => _Page1State();
}

class _Page1State extends State<Page1> {
  CameraController? controller;
  late WebSocketChannel channel; 
  bool isWebSocketConnected = false; // Aggiungi questa variabile di stato
  int currentCameraIndex = 0;
  Timer? periodicTimer;

  @override
  void initState() {
    super.initState();
    initializeCamera();
  }

  Future<void> initializeCamera() async {
  final cameras = await availableCameras();

  if (controller == null) {
    controller = CameraController(cameras[0], ResolutionPreset.medium);
    await controller!.initialize();
  }

  if (mounted) {
    setState(() {});
  }
}

  @override
  void dispose() {
    controller!.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Riconoscimento'),
      ),
      body: Stack(
        children: [
           if (controller != null && controller!.value.isInitialized) CameraPreview(controller!),
          Align(
            alignment: Alignment.bottomCenter,
            child: Container(
              margin: EdgeInsets.only(bottom: 20.0),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  ElevatedButton(
                    onPressed: switchCamera,
                    child: Icon(Icons.switch_camera, size: 48.0),
                  ),
                  SizedBox(width: 20.0),
                  ElevatedButton(
                    onPressed: () {
                      if (!isWebSocketConnected) {
                        startSendingFramesPeriodically();
                      } else {
                        stopSendingFramesPeriodically();
                      }
                      setState(() {
                        isWebSocketConnected = !isWebSocketConnected;
                      });
                    },
                    child: isWebSocketConnected
                        ? Icon(Icons.stop, size: 48.0)
                        : Icon(Icons.play_arrow, size: 48.0),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  getAvailableCameras() async
  {
    return await availableCameras();
  }

  List<int> convertYUV420ToJPEG(CameraImage cameraImage) 
  {
    final width = cameraImage.width;
    final height = cameraImage.height;

    final yRowStride = cameraImage.planes[0].bytesPerRow;
    final uvRowStride = cameraImage.planes[1].bytesPerRow;
    final uvPixelStride = cameraImage.planes[1].bytesPerPixel!;

    final image = imglib.Image(width, height);

    for (var w = 0; w < width; w++) {
      for (var h = 0; h < height; h++) {
        final uvIndex =
            uvPixelStride * (w / 2).floor() + uvRowStride * (h / 2).floor();
        final index = h * width + w;
        final yIndex = h * yRowStride + w;

        final y = cameraImage.planes[0].bytes[yIndex];
        final u = cameraImage.planes[1].bytes[uvIndex];
        final v = cameraImage.planes[2].bytes[uvIndex];

        image.data[index] = yuv2rgb(y, u, v);
      }
    }

    final rotatedImage = imglib.copyRotate(image, 270);

    // Salva l'immagine in formato JPEG
    final jpegData = Uint8List.fromList(imglib.encodeJpg(rotatedImage));

    // Converte l'immagine JPEG in una lista di interi
    final jpegList = jpegData.toList();

    return jpegList;
  }

  static int yuv2rgb(int y, int u, int v) {
    // Convert yuv pixel to rgb
    var r = (y + v * 1436 / 1024 - 179).round();
    var g = (y - u * 46549 / 131072 + 44 - v * 93604 / 131072 + 91).round();
    var b = (y + u * 1814 / 1024 - 227).round();

    // Clipping RGB values to be inside boundaries [ 0 , 255 ]
    r = r.clamp(0, 255);
    g = g.clamp(0, 255);
    b = b.clamp(0, 255);

    return 0xff000000 |
        ((b << 16) & 0xff0000) |
        ((g << 8) & 0xff00) |
        (r & 0xff);
  }

  void startSendingFramesPeriodically () async
  {
    if (controller!.value.isInitialized) 
    {
      channel = IOWebSocketChannel.connect(Uri.parse('ws://192.168.1.89:8765'));

      var id = Provider.of<CodeModel>(context, listen: false)._code;     

      var sending = true;

      void sendFrame(CameraImage image) async {
        if (sending) {
          List<int> jpegData = convertYUV420ToJPEG(image);
          Uint8List imageBytes = Uint8List.fromList(jpegData);
          final mess = 
          {
            'id': id,
            'image': imageBytes
          };

          channel.sink.add(await jsonEncode(mess));
          sending = false; // Imposta a false solo dopo l'invio del frame
        }
      }

      // Imposta un timer per riattivare l'invio dei frame ogni 3 secondi
      periodicTimer = Timer.periodic(Duration(seconds: 2), (Timer timer) {
        sending = true;
      });

      // Chiama la funzione sendFrame all'interno di startImageStream
      controller!.startImageStream((CameraImage image) async {
        sendFrame(image);
      });
      
    }
  }

  void stopSendingFramesPeriodically() async
  {
    controller!.stopImageStream();
    periodicTimer!.cancel();  
    await channel.sink.close(1000);
  }

  void switchCamera() async {
    final List<CameraDescription> cameras = await getAvailableCameras();

    if (!isWebSocketConnected) {
      // Rilascia la fotocamera corrente
      await controller!.dispose();

      // Calcola l'indice della fotocamera successiva
      currentCameraIndex = (currentCameraIndex + 1) % cameras.length;

      // Inizializza la fotocamera successiva
      controller = CameraController(cameras[currentCameraIndex], ResolutionPreset.low);
      await controller!.initialize();

      // Aggiorna lo stato
      setState(() {});
    }
  }
}

class Page2 extends StatefulWidget {
  @override
  _Page2State createState() => _Page2State();
}

class _Page2State extends State<Page2> 
{
  CameraController? controller;
  TextEditingController _textFieldController = TextEditingController();
  bool isFieldValid = false;
  XFile? capturedImage;

  @override
  void initState() {
    super.initState();
    initializeCamera();
  }

  Future<void> initializeCamera() async {
    final cameras = await availableCameras();

    // Trova la fotocamera anteriore (interna)
    final frontCamera = cameras.firstWhere(
        (camera) => camera.lensDirection == CameraLensDirection.front,
        orElse: () => cameras.first);

    controller = CameraController(frontCamera, ResolutionPreset.low);
    await controller!.initialize();

    if (mounted) {
      setState(() {});
    }
  }

  @override
  void dispose() {
    controller?.dispose();
    super.dispose();
  }

  Future<void> captureImage() async {
    if (controller != null && controller!.value.isInitialized) {
      try {
        final image = await controller!.takePicture();
        setState(() {
          capturedImage = image;
        });
      } catch (e) {
        print('Errore durante la cattura dell\'immagine: $e');
      }
    }
  }

  void sendImageAndData(BuildContext context) async 
  {
    await captureImage();

    if (capturedImage != null) 
    {
      // Mostra il caricamento
        showDialog(
          context: context,
          builder: (BuildContext context) {
            return Center(
              child: CircularProgressIndicator(),
            );
          },
        );

      final channel = IOWebSocketChannel.connect('ws://192.168.1.89:8767');

      var id = Provider.of<CodeModel>(context, listen: false)._code;
      List<int> image = await File(capturedImage!.path).readAsBytes();
      Uint8List imageBytes = Uint8List.fromList(image);

      final mess = 
      {
        'id': id,
        'image': imageBytes,
        'fieldValue': _textFieldController.text,
      };

      // Invia i dati al server
      channel.sink.add(await jsonEncode(mess));
      // Attendi la risposta del server
      await for (var response in channel.stream) 
      {
        if (response == "1") {
          showDialog(
          context: context,
          builder: (BuildContext context) {
            return AlertDialog(
              title: Text("Errore"),
              content: Text("Connessione non autorizzata"),
              actions: [
                TextButton(
                  onPressed: () {
                    Navigator.of(context).pop();
                  },
                  child: Text("OK"),
                ),
              ],
            );
          },
        );
        break;
        }
        else if (response == "2") {
          // Chiudi la finestra di dialogo di caricamento
          Navigator.of(context).pop();
          showDialog(
          context: context,
          builder: (BuildContext context) {
            return AlertDialog(
              title: Text("Successo"),
              content: Text("Registrazione effettuata con successo"),
              actions: [
                TextButton(
                  onPressed: () {
                    Navigator.of(context).pop();
                  },
                  child: Text("OK"),
                ),
              ],
            );
          },
          
        );
        break;
        } else if (response == "3") {
          // Chiudi la finestra di dialogo di caricamento
          Navigator.of(context).pop();
          showDialog(
          context: context,
          builder: (BuildContext context) {
            return AlertDialog(
              title: Text("Errore"),
              content: Text("L'immagine non rappresenta un volto, si prega di riprovare"),
              actions: [
                TextButton(
                  onPressed: () {
                    Navigator.of(context).pop();
                  },
                  child: Text("OK"),
                ),
              ],
            );
          },
        );
        break;
        } else {
          // Chiudi la finestra di dialogo di caricamento
          Navigator.of(context).pop();
          showDialog
          (
            context: context,
            builder: (BuildContext context) {
              return AlertDialog(
                title: Text("Errore"),
                content: Text("Questo utente è già registrato come " + response),
                actions: [
                  TextButton(
                    onPressed: () {
                      Navigator.of(context).pop();
                    },
                    child: Text("OK"),
                  ),
                ],
              );
            },
          );
          break;
        }
      }

      // Chiudi la connessione
      channel.sink.close();     
    }
  }

  @override
  Widget build(BuildContext context) 
  {
    return Scaffold(
      appBar: AppBar(
        title: Text('Registrazione'),
      ),
      body: SingleChildScrollView(
        child: Column(
          children: [
            if (controller != null && controller!.value.isInitialized)
              CameraPreview(controller!),
            Padding(
              padding: const EdgeInsets.all(16.0),
              child: TextField(
                controller: _textFieldController,
                decoration: InputDecoration(labelText: 'Nome'),
                onChanged: (value) {
                  setState(() {
                    isFieldValid = value.isNotEmpty;
                  });
                },
              ),
            ),
            ElevatedButton(
              onPressed: isFieldValid
                  ? () {
                      // Invia l'immagine e il valore del campo tramite WebSocket
                      sendImageAndData(context);
                    }
                  : null, // Disabilita il pulsante se il campo non è compilato
              child: Text('Registra'),
            ),
            if (capturedImage != null)
              Image.file(File(capturedImage!.path)), // Mostra l'immagine catturata
          ],
        ),
      ),
    );
  }
}