using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Similitud.Web.Models;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using System.Timers;
using System.Threading;
using Newtonsoft.Json;
using Accord.MachineLearning;

namespace Similitud.Web.Controllers
{
    public class PrincipalController : Controller
    {
        String result = "";
        //
        // GET: /Principal/
        [HttpGet]
        public ActionResult Index()
        {
            //CargadoCanciones();
            //ExportarCSVFormat();
            //getSimilaresLastFM();
            //SaveSimilarCanciones();
            return View();
        }

        
        public String getIDSpotify(string titulo, string artista)
        {
            titulo = JsonConvert.DeserializeObject<String>(titulo);
            artista = JsonConvert.DeserializeObject<String>(artista);
            String id;
            String json = "http://developer.echonest.com/api/v4/song/search?api_key=ERYL0FA7VZ24XQMOO&format=json&results=1&artist=" + artista + "&title=" + titulo + "&bucket=id:spotify&bucket=tracks&limit=true&bucket=audio_summary";
            String jsonResult = SONGGET(json);
            JObject jObject = JObject.Parse(jsonResult);
            Array arraySongs = ((jObject["response"])["songs"]).ToArray();
            JToken tokenSongs = (JToken)(arraySongs.GetValue(0));
            Array arrayTracks = tokenSongs["tracks"].ToArray();
            JToken tokenTracks = (JToken)arrayTracks.GetValue(0);
            id = tokenTracks["foreign_id"].ToString();
            return id;
        }//Método para idspotify x título y artista
        private List<String> getIdsSpotify(List<String> listaIndices)
        {
            List<String> getIdsSpotify = new List<String>(); ModeloSimilitudEntities db = new ModeloSimilitudEntities();
            List<canciones> musicas = db.canciones.ToList();
            foreach (String indice in listaIndices)
            {
                getIdsSpotify.Add((musicas[Int32.Parse(indice)]).id_spotify);
            }
            return getIdsSpotify;
        }//Similar al 1ero pero trae una lista
        public ActionResult ObtenerSimilaresxTitulo(string titulo,string artista)
        {
            List<String> IdSpotify = new List<String>();
            try
            {
                titulo = JsonConvert.DeserializeObject<String>(titulo);
                artista = JsonConvert.DeserializeObject<String>(artista);


                String json = "http://developer.echonest.com/api/v4/song/search?api_key=ERYL0FA7VZ24XQMOO&format=json&results=1&artist=" + artista + "&title=" + titulo + "&bucket=id:spotify&bucket=tracks&limit=true&bucket=audio_summary";
                String jsonResult = SONGGET(json);

                JObject jObject = JObject.Parse(jsonResult);
                Array arraySongs = ((jObject["response"])["songs"]).ToArray();
                JToken tokenSongs = (JToken)(arraySongs.GetValue(0));
                JToken tokenSummary = tokenSongs["audio_summary"];
                List<Double> descriptoresEntrada = new List<Double>();
                descriptoresEntrada.Add(Double.Parse(tokenSummary["energy"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["liveness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["tempo"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["speechiness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["acousticness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["loudness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["valence"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["danceability"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["instrumentalness"].ToString()));
                descriptoresEntrada.Add(int.Parse(tokenSummary["key"].ToString()));

                List<String> listaItems = GetSimilaresDatabase(descriptoresEntrada);
                IdSpotify = getIdsSpotify(listaItems);
            }
            catch (Exception e)
            {
                PartialView("PlayListSpotify", IdSpotify);
            }

            return PartialView("PlayListSpotify", IdSpotify);

        }
        private List<String> GetSimilaresDatabase(List<Double> descriptoresEntrada)
        {
            List<Double> listaItems;
            List<Double> descEntradaNormalizado = Normalizar(descriptoresEntrada);
            int centroide = DiferenciaCentroides(descEntradaNormalizado);
            listaItems = CancionesEnCluster(centroide);
            List<List<Double>> matrizNormalizado = csvtoMatrix("descriptoresNormalizados");
            List<String> listaSimilares = new List<String>();
            List<int> listaIndicesSimilares = new List<int>();

            for (int j = 0; j < 15; j++)
            {
                int indiceBuscado = 0,indiceLista=0;
                double distancia = 9999;
                for (int i = 0; i < listaItems.Count;i++)
                {
                    if (!listaIndicesSimilares.Contains(i))
                    {
                        Double distanciaTemporal;
                        int indiceNormal = Int32.Parse(listaItems[i].ToString()) - 1;//porque el indice en la matriz es uno menos

                        distanciaTemporal = distanciaEuclidea(descEntradaNormalizado, matrizNormalizado[indiceNormal]);

                        if (distanciaTemporal < distancia)
                        {
                            distancia = distanciaTemporal;
                            indiceBuscado = indiceNormal;
                            indiceLista = i;
                        }
                    }
                }
                listaIndicesSimilares.Add(indiceLista);
                listaSimilares.Add(indiceBuscado.ToString());

            }
            return listaSimilares;
        }
        string SONGGET(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                //WebResponse errorResponse = ex.Response;
                //using (Stream responseStream = errorResponse.GetResponseStream())
                //{
                //    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                //    String errorText = reader.ReadToEnd();
                //    // log errorText
                //}
                //throw;
                System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\log\registros.txt", ex + "\n\n");
                return "";
            }
        }
        public ActionResult ObtenerSimilares(string URL)
        {
            List<String> IdSpotify = new List<String>();
            try
            {
                URL = JsonConvert.DeserializeObject<String>(URL);
                Task<String> tarea = getOriginal(URL);
                String jsonOriginal = tarea.Result;
                JObject jObject = JObject.Parse(jsonOriginal);
                JToken tokenResponse = jObject["response"];
                JToken tokenTrack = tokenResponse["track"];
                String ID = tokenTrack["id"].ToString();
                String urlTrackUpload = "http://developer.echonest.com/api/v4/track/profile?api_key=ERYL0FA7VZ24XQMOO&format=json&id=" + ID + "&bucket=audio_summary";


                String jsonResultTrack = SONGGET(urlTrackUpload);
                JObject jObjectResult = JObject.Parse(jsonResultTrack);
                JToken tokenResponseResult = jObjectResult["response"];
                JToken tokenTrackResult = tokenResponseResult["track"];
                String status = tokenTrackResult["status"].ToString();

                while (!status.Equals("complete"))
                {
                    int tiempoDelay = 1000;
                    Thread.Sleep(tiempoDelay);
                    jsonResultTrack = SONGGET(urlTrackUpload);
                    jObjectResult = JObject.Parse(jsonResultTrack);
                    tokenResponseResult = jObjectResult["response"];
                    tokenTrackResult = tokenResponseResult["track"];
                    status = tokenTrackResult["status"].ToString();
                }

                JToken tokenAudioSummary = tokenTrackResult["audio_summary"];
                List<Double> descriptoresEntrada = new List<Double>();
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["energy"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["liveness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["tempo"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["speechiness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["acousticness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["loudness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["valence"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["danceability"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenAudioSummary["instrumentalness"].ToString()));
                descriptoresEntrada.Add(int.Parse(tokenAudioSummary["key"].ToString()));

                List<String> listaItems = GetSimilaresDatabase(descriptoresEntrada);
                IdSpotify = getIdsSpotify(listaItems);
            }
            catch (Exception e)
            {
                PartialView("PlayListSpotify", IdSpotify);
            }

            return PartialView("PlayListSpotify", IdSpotify);

        }
        public async Task<String> getOriginal(String url)
        {
            var client = new HttpClient();
            var requestContent = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("api_key", "ERYL0FA7VZ24XQMOO"),
                new KeyValuePair<string, string>("url", url),
            });
            Task<HttpResponseMessage> task = client.PostAsync("http://developer.echonest.com/api/v4/track/upload", requestContent);
            HttpResponseMessage response = task.Result;
            HttpContent responseContent = response.Content;

            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                result = await reader.ReadToEndAsync();
                return result;
            }

        }

        #region GetSimilaresDatabase
        private Double distanciaEuclidea(List<Double> vectorOriginal, List<Double> vectorbusqueda)
        {
            Double distancia=0;
            for (int i = 0; i < vectorOriginal.Count; i++)
            {
                distancia += (vectorOriginal[i] - vectorbusqueda[i]) * (vectorOriginal[i] - vectorbusqueda[i]);
            }
            return distancia;
        }
        private List<Double> CancionesEnCluster(int centroide)
        {
            List<List<Double>> clusters = csvtoMatrix("clusters");
            List<Double> CancionesSimilares = clusters[centroide];
            return CancionesSimilares;
        }
        private int DiferenciaCentroides(List<Double> descriptoresEntrada)
        {
            int centroide=0;
            List<List<Double>> centroides = csvtoMatrix("centroides");
            Double distanciamax = 10;
            for(int k=0;k<centroides.Count;k++)
            {
                Double distanciatemporal = distanciaEuclidea(centroides[k], descriptoresEntrada);
                if (distanciatemporal<distanciamax)
                {
                    distanciamax = distanciatemporal;
                    centroide = k;
                }
            }
            return centroide;
        }
        private List<Double> Normalizar(List<Double> descriptoresEntrada)
        {
            List<Double> listaItems = new List<Double>();
            List<List<Double>> maxYmin = csvtoMatrix("maxYmin");
            for (int i = 0; i < descriptoresEntrada.Count; i++)
            {
                double valornormalizado = (descriptoresEntrada[i] - (maxYmin[1])[i]) / ((maxYmin[0])[i] - (maxYmin[1])[i]);
                //if (i == 0 || i == 1) { valornormalizado = valornormalizado* 2; }
                listaItems.Add(valornormalizado);

            }
            return listaItems;
        }
        private List<List<Double>> csvtoMatrix(string nombre){
            List<List<Double>> matrizDescriptores = new List<List<double>>();
            var reader = new StreamReader(System.IO.File.OpenRead(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\Matlab\"+nombre+".txt"));
            List<Double> list = new List<Double>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                List<String> values = line.Split(',').ToList();
                list = values.Select(Double.Parse).ToList();
                matrizDescriptores.Add(list);
            }
            return matrizDescriptores;
        }
        #endregion

        #region FuncionesCreación
        private Dictionary<String, String> getNames()
        {
            Dictionary<String, String> canciones = new Dictionary<String, String>();
            canciones.Add("Smoke the weed", "Snoop Lion");
            canciones.Add("Cheerleader", "OMI");
            canciones.Add("Man Down", "Rihanna");
            canciones.Add("I'm still in love with you", "Sean Paul");
            canciones.Add("No woman no cry", "Bob Marley");
            canciones.Add("Sweat", "Inner Circle");
            canciones.Add("Is this love", "Bob Marley");
            canciones.Add("Me gustas tú", "Manu chao");
            canciones.Add("Three little birds", "bob marley");
            canciones.Add("Red red wine", "UB40");

            canciones.Add("Fanática Sensual", "Plan B");
            canciones.Add("Ay Vamos", "J. Balvin");
            canciones.Add("Mi vecinita", "Plan B");
            canciones.Add("Travesuras", "Nicky Jam");
            canciones.Add("Choca", "Plan B");
            //canciones.Add("Don Omar", "Perdido en tus ojos");
            canciones.Add("Passion Whine", "Farruko");
            canciones.Add("Voy a beber", "Nicky Jam");
            canciones.Add("Una noche mas", "Kevin Roldan");
            canciones.Add("Sigueme y te sigo", "Daddy Yankee");

            canciones.Add("Usted no sabe", "Alexandre Pires");
            canciones.Add("Quien te dijo eso", "Luis Fonsi");
            canciones.Add("Que me alcance la vida", "Sin banderas");
            canciones.Add("Me duele amarte", "Reik");
            canciones.Add("Me dedique a perderte", "Alejandro Fernandez");
            canciones.Add("Just the way you are", "Billy Joel");
            canciones.Add("I just want to be your everything", "Andy Gibb");
            canciones.Add("Every time you go away", "Paul Young");
            canciones.Add("Careless Whisper", "George Michael");
            canciones.Add("It must have been love", "Roxette");
            canciones.Add("Baby come Back", "Player");

            canciones.Add("Sweet Child O' mine", "Guns N' Roses");
            canciones.Add("Thunderstruck", "AC/DC");
            canciones.Add("Stairway to heaven", "Led Zeppelin");
            canciones.Add("Enter sandman ", "Metallica");
            canciones.Add("Welcome to the Jungle", "Guns N' Roses");
            canciones.Add("Run to the hills", "Iron maden");
            canciones.Add("One", "Metallica");
            canciones.Add("Stay away", "Nirvana");
            canciones.Add("Chop Suey", "System of a Down");
            canciones.Add("Smells like teen spirit", "Nirvana");

            canciones.Add("Dr. Dre", "Snoop Dogg");
            canciones.Add("In da club", "50 cent");
            canciones.Add("Without me", "Eminem");

            canciones.Add("Flor palida", "Marc Anthony");
            canciones.Add("Pequeñas cosas", "Willie gonzales");
            canciones.Add("Tu me haces falta", "Eddie Santiago");
            canciones.Add("Sin sentimiento", "Grupo Niche");
            canciones.Add("En barranquilla me quedo", "Joe Arroyo");
            canciones.Add("Desnudate mujer", "Frankie Ruiz");
            canciones.Add("El cantante", "Hector Lavoe");
            canciones.Add("Casi te envidio", "Andy Montañez");
            canciones.Add("Esa mujer", "Tony Vega");
            canciones.Add("De mi enamorate", "Tito Nieves");

            canciones.Add("Don't know why", "Norah Jones");
            canciones.Add("La vie en rose", "Louis Armstrong");
            canciones.Add("It had to be you", "Ray Charles");
            canciones.Add("Fly Me To The Moon", "Frank Sinatra");
            canciones.Add("Feeling Good", "Nina Simone");
            canciones.Add("Don't start me talkin", "Sugar Blue");
            canciones.Add("My babe", "Little walter");
            canciones.Add("Rock me", "James Cotton");
            canciones.Add("All Night Boogie", "Howlin' Wolf");


            return canciones;
        }
        private void CargadoCanciones()
        {
            ModeloSimilitudEntities db = new ModeloSimilitudEntities();
            subset_track_metadataEntities sm = new subset_track_metadataEntities();

            List<songs> musicas = sm.songs.ToList();
            string id_song, json, jsonResult;
            for (int i = 7212; i <= musicas.Count; i++)
            {
                songs song = musicas[i];
                int milliseconds = 3000;
                Thread.Sleep(milliseconds);
                id_song = song.song_id;
                json = "http://developer.echonest.com/api/v4/song/profile?api_key=ERYL0FA7VZ24XQMOO&format=json&results=1&id=" + id_song + "&bucket=id:spotify&bucket=tracks&limit=true&bucket=audio_summary";
                jsonResult = SONGGET(json);
                if (!jsonResult.Equals(""))
                {
                    string cancionesNoEncontradas = "";
                    canciones cancion = new canciones();
                    try
                    {
                        JObject jObject = JObject.Parse(jsonResult);
                        Array arraySongs = ((jObject["response"])["songs"]).ToArray();
                        JToken tokenSongs = (JToken)(arraySongs.GetValue(0));
                        JToken tokenSummary = tokenSongs["audio_summary"];

                        cancion.energy = Double.Parse(tokenSummary["energy"].ToString());
                        cancion.liveness = Double.Parse(tokenSummary["liveness"].ToString());
                        cancion.tempo = Double.Parse(tokenSummary["tempo"].ToString());
                        cancion.speechiness = Double.Parse(tokenSummary["speechiness"].ToString());
                        cancion.acousticness = Double.Parse(tokenSummary["acousticness"].ToString());
                        cancion.loudness = Double.Parse(tokenSummary["loudness"].ToString());
                        cancion.valence = Double.Parse(tokenSummary["valence"].ToString());
                        cancion.danceability = Double.Parse(tokenSummary["danceability"].ToString());
                        cancion.instrumentalness = Double.Parse(tokenSummary["instrumentalness"].ToString());
                        cancion.key = int.Parse(tokenSummary["key"].ToString());

                        Array arrayTracks = tokenSongs["tracks"].ToArray();
                        JToken tokenTracks = (JToken)arrayTracks.GetValue(0);
                        cancion.id_spotify = tokenTracks["foreign_id"].ToString();

                        cancion.track_id = song.track_id;
                        cancion.title = song.title;
                        cancion.song_id = song.song_id;
                        cancion.artist_id = song.artist_id;
                        cancion.artist_mbid = song.artist_mbid;
                        cancion.artist_name = song.artist_name;
                        cancion.duration = Double.Parse(song.duration.ToString());
                        cancion.artist_familiarity = song.artist_familiarity;
                        cancion.artist_hotttnesss = song.artist_hotttnesss;
                        cancion.year = int.Parse(song.year.ToString());
                        db.canciones.Add(cancion);
                        int numberOfObjects = db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\log\registros.txt", song.song_id + "\n");
                    }
                }

            }
        }//desde DB file echo nest
        private void SaveSimilarCanciones()
        {
            ModeloSimilitudEntities db = new ModeloSimilitudEntities();

            List<similares> ListaSimilares = db.similares.ToList();
            string id_song, json, jsonResult;
            for (int i = 11547; i < ListaSimilares.Count; i++)
            {
                ModeloSimilitudEntities database = new ModeloSimilitudEntities();
                similares similar = ListaSimilares[i];
                int milliseconds = 3000;
                Thread.Sleep(milliseconds);
                String artist_similar = similar.Artist_Similar;
                String song_similar = similar.Song_Similar;

                json = "http://developer.echonest.com/api/v4/song/search?api_key=ERYL0FA7VZ24XQMOO&format=json&results=1&artist=" + artist_similar + "&title=" + song_similar + "&bucket=id:spotify&bucket=tracks&limit=true&bucket=audio_summary";
                jsonResult = SONGGET(json);
                if (!jsonResult.Equals(""))
                {
                    string cancionesNoEncontradas = "";
                    canciones cancion = new canciones();
                    try
                    {
                        JObject jObject = JObject.Parse(jsonResult);
                        Array arraySongs = ((jObject["response"])["songs"]).ToArray();
                        JToken tokenSongs = (JToken)(arraySongs.GetValue(0));
                        JToken tokenSummary = tokenSongs["audio_summary"];

                        cancion.energy = Double.Parse(tokenSummary["energy"].ToString());
                        cancion.liveness = Double.Parse(tokenSummary["liveness"].ToString());
                        cancion.tempo = Double.Parse(tokenSummary["tempo"].ToString());
                        cancion.speechiness = Double.Parse(tokenSummary["speechiness"].ToString());
                        cancion.acousticness = Double.Parse(tokenSummary["acousticness"].ToString());
                        cancion.loudness = Double.Parse(tokenSummary["loudness"].ToString());
                        cancion.valence = Double.Parse(tokenSummary["valence"].ToString());
                        cancion.danceability = Double.Parse(tokenSummary["danceability"].ToString());
                        cancion.instrumentalness = Double.Parse(tokenSummary["instrumentalness"].ToString());
                        cancion.key = int.Parse(tokenSummary["key"].ToString());

                        Array arrayTracks = tokenSongs["tracks"].ToArray();
                        JToken tokenTracks = (JToken)arrayTracks.GetValue(0);
                        cancion.id_spotify = tokenTracks["foreign_id"].ToString();

                        cancion.track_id = tokenTracks["id"].ToString();
                        cancion.title = tokenSongs["title"].ToString();
                        cancion.song_id = tokenSongs["id"].ToString();
                        cancion.artist_id = tokenSongs["artist_id"].ToString();
                        cancion.artist_mbid = "";//falta
                        cancion.artist_name = tokenSongs["artist_name"].ToString();
                        cancion.duration = Double.Parse(tokenSummary["duration"].ToString());
                        cancion.artist_familiarity = 0;//falta
                        cancion.artist_hotttnesss = 0;//falta
                        cancion.year = 0;//falta
                        database.canciones.Add(cancion);
                        int numberOfObjects = database.SaveChanges();
                    }
                    catch (Exception e)
                    {

                        System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\log\noencontradosEchoNest.txt", similar.Song_Similar + "-" + similar.Artist_Similar + i + Environment.NewLine);
                    }
                }

            }
        }
        private void ExportarCSVFormat()
        {
            ModeloSimilitudEntities db = new ModeloSimilitudEntities();
            List<canciones> musicas = db.canciones.ToList();
            foreach (canciones cancion in musicas)
            {
                String descriptoresXlinea = cancion.energy.ToString() + "," + cancion.tempo.ToString() + "," + cancion.speechiness.ToString() + "," + cancion.acousticness.ToString() + "," + cancion.loudness.ToString() + "," + cancion.valence.ToString() + "," + cancion.danceability.ToString() + "," + cancion.instrumentalness.ToString() + "," + cancion.key.ToString();
                System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\Matlab\descriptores.txt", descriptoresXlinea + Environment.NewLine);
            }
        }
        public void getSimilaresLastFM()
        {
            ModeloSimilitudEntities db = new ModeloSimilitudEntities();
            Dictionary<String, String> canciones = getNames();
            foreach (String cancion in canciones.Keys)
            {
                int tiempo = 2000;
                Thread.Sleep(tiempo);
                String artista = canciones[cancion];
                String jsonRequest = "http://ws.audioscrobbler.com/2.0/?method=track.getsimilar&artist=" + artista + "&track=" + cancion + "&api_key=7e609960174aab0894676a8cca49548b&format=json";
                try
                {
                    String jsonResultSimilars = SONGGET(jsonRequest);
                    JObject jObject = JObject.Parse(jsonResultSimilars);
                    JToken tokenSimilars = jObject["similartracks"];
                    Array arrayTracks = tokenSimilars["track"].ToArray();
                    for (int i = 0; i < arrayTracks.Length; i++)
                    {
                        similares similar = new similares();
                        JToken tokenAudioSimilar = (JToken)arrayTracks.GetValue(i);
                        similar.Artist_Original = artista;
                        similar.Song_Original = cancion;
                        JToken tokenArtistSimilar = tokenAudioSimilar["artist"];
                        similar.Artist_Similar = tokenArtistSimilar["name"].ToString();
                        similar.Song_Similar = tokenAudioSimilar["name"].ToString();
                        similar.Valor_Similitud = i;
                        db.similares.Add(similar);
                        int numberOfObjects = db.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\log\noencontrados.txt", cancion + "-" + artista + Environment.NewLine);
                }

            }
        }
        #endregion

        #region CodigoInservible
        public static async void Task2()
        {
            var client = new HttpClient();
            string ruta=@"C:\tesisData\dataset\rock\rock.00007.au";
            string url="audio/mpeg"+ Path.GetExtension(ruta).Replace(".", "")+ ";base64," + Convert.ToBase64String(System.IO.File.ReadAllBytes(ruta));
            var requestContent = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("api_key", "ERYL0FA7VZ24XQMOO"),
                new KeyValuePair<string, string>("url", url),
            });
            Task<HttpResponseMessage> task = client.PostAsync("http://developer.echonest.com/api/v4/track/upload", requestContent);
            HttpResponseMessage response = await task;
            HttpContent responseContent = response.Content;
            String result;
            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                result = await reader.ReadToEndAsync();
            }

        }

        public void subirArchivo(){
            string url = "http://developer.echonest.com/api/v4/track/upload?api_key=ERYL0FA7VZ24XQMOO&filetype=au";
            string file = @"C:\tesisData\dataset\rock\rock.00007.au";
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("api_key", "ERYL0FA7VZ24XQMOO");
            nvc.Add("filetype", "au");
            nvc.Add("track", "rock.00007.au");

            UploadFilesToRemoteUrl(url, file);
        }

        private string UploadFilesToRemoteUrl(string url, string file)
        {
            // Create a boundry
            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

            // Create the web request
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            httpWebRequest.Method = "POST";
            httpWebRequest.KeepAlive = true;

            httpWebRequest.Credentials =
            System.Net.CredentialCache.DefaultCredentials;

            // Get the boundry in bytes
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            // Get the header for the file upload
            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\";filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

            // Add the filename to the header
            string header = string.Format(headerTemplate, "file", file);

            //convert the header to a byte array
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);

            // Add all of the content up.
            httpWebRequest.ContentLength = new FileInfo(file).Length + headerbytes.Length + (boundarybytes.Length * 2) + 2;

            // Get the output stream
            Stream requestStream = httpWebRequest.GetRequestStream();

            // Write out the starting boundry
            requestStream.Write(boundarybytes, 0, boundarybytes.Length);

            // Write the header including the filename.
            requestStream.Write(headerbytes, 0, headerbytes.Length);

            // Open up a filestream.
            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

            // Use 4096 for the buffer
            byte[] buffer = new byte[4096];

            int bytesRead = 0;
            // Loop through whole file uploading parts in a stream.
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                requestStream.Write(buffer, 0, bytesRead);
                requestStream.Flush();
            }

            boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            // Write out the trailing boundry
            requestStream.Write(boundarybytes, 0, boundarybytes.Length);

            // Close the request and file stream
            requestStream.Close();
            fileStream.Close();

            WebResponse webResponse = httpWebRequest.GetResponse();

            Stream responseStream = webResponse.GetResponseStream();
            StreamReader responseReader = new StreamReader(responseStream);

            string responseString = responseReader.ReadToEnd();

            // Close response object.
            webResponse.Close();

            return responseString;
        }

        string Probando4()
        {
            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create("http://developer.echonest.com/api/v4/track/upload?api_key=ERYL0FA7VZ24XQMOO&filetype=au");
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
            byte[] byteArray = System.IO.File.ReadAllBytes(@"C:\tesisData\dataset\rock\rock.00007.au");
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            Console.WriteLine(responseFromServer);
            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }

        string SONPOST2(string song)
        {
            var request = (HttpWebRequest)WebRequest.Create("http://developer.echonest.com/api/v4/track/upload");

            var postData = "api_key=ERYL0FA7VZ24XQMOO";
            postData += "&format=json";
            postData += "&track=\"C:\\tesisData\\dataset\\rock\\rock.au\"";
            postData += "&filetype=au";
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            //request.ContentType = "application/octet-stream";
            //request.ContentType = "multipart/form-data";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            return responseString;
        }


        public static async void probando4()
        {
            using (var client = new WebClient())
            {

                byte[] archivo = System.IO.File.ReadAllBytes(@"C:\tesisData\dataset\rock\rock.00007.au");
                Uri address = new Uri("http://developer.echonest.com/api/v4/track/upload?api_key=ERYL0FA7VZ24XQMOO&filetype=au");


                Task<byte[]> task = client.UploadDataTaskAsync(address, "POST", archivo);
                byte[] a = await task;
                var responseString = Encoding.Default.GetString(a);

            }

        }
        //private string GetDataURL(string ruta)
        //{
        //    try
        //    {
        //        return "data:image/"
        //               + Path.GetExtension(ruta).Replace(".", "")
        //               + ";base64,"
        //               + Convert.ToBase64String(System.IO.File.ReadAllBytes(ruta));

        //    }
        //    catch (Exception ex)
        //    {
        //        return "../img/fondo.png";
        //    }
        //}

        #endregion
    }
}
