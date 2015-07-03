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
            return View();
        }
        public ActionResult ObtenerSimilares(string URL)
        {
            URL = JsonConvert.DeserializeObject<String>(URL);
            Task<String> tarea=getOriginal(URL);
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
            return PartialView("PlayListSpotify", listaItems);
            
        }
        private List<String> GetSimilaresDatabase(List<Double> descriptoresEntrada)
        {
            List<Double> listaItems = new List<Double>();
            List<Double> descEntradaNormalizado = Normalizar(descriptoresEntrada);
            int centroide = DiferenciaCentroides(descEntradaNormalizado);
            listaItems = CancionesEnCluster(centroide);
            return new List<string>();
        }
        private List<Double> CancionesEnCluster(int centroide)
        {
            List<Double> listaCanciones = new List<Double>();
            List<List<Double>> clusters = csvtoMatrix("clusters");
            List<Double> CancionesSimilares = clusters[centroide];
            return listaCanciones;
        }
        private int DiferenciaCentroides(List<Double> descriptoresEntrada)
        {
            int centroide=0;
            List<List<Double>> centroides = csvtoMatrix("centroides");
            Double distanciamax = 0;
            for(int k=0;k<centroides.Count;k++)
            {
                List<Double> fila =centroides[k];
                Double distanciatemporal=0;
                for (int i = 0; i < fila.Count; i++)
                {
                    distanciatemporal += (fila[i] - descriptoresEntrada[i]) * (fila[i] - descriptoresEntrada[i]);
                }
                if (distanciamax < distanciatemporal)
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
                listaItems.Add((descriptoresEntrada[i] - (maxYmin[1])[i]) / ((maxYmin[0])[i]- (maxYmin[1])[i]));
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
        private void ExportarCSVFormat(){
            ModeloSimilitudEntities db = new ModeloSimilitudEntities();
            List<canciones> musicas = db.canciones.ToList();
            foreach (canciones cancion in musicas)
            {
                String descriptoresXlinea = cancion.energy.ToString() + "," + cancion.liveness.ToString() + "," + cancion.tempo.ToString() + "," + cancion.speechiness.ToString() + "," + cancion.acousticness.ToString() + "," + cancion.loudness.ToString() + "," + cancion.valence.ToString() + "," + cancion.danceability.ToString() + "," + cancion.instrumentalness.ToString()+","+cancion.key.ToString();
                System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\Matlab\descriptores.txt", descriptoresXlinea+Environment.NewLine );
            }
        }
        
        private void CargadoCanciones(){
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
                        System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\log\registros.txt", song.song_id+"\n");
                    }
                }
                
            }
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
                System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\log\registros.txt", ex+ "\n\n");
                return "";
            }
        }

        public  async Task<String> getOriginal(String url)
        {
            var client = new HttpClient();
            var requestContent = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("api_key", "ERYL0FA7VZ24XQMOO"),
                new KeyValuePair<string, string>("url", url),
            });
            Task<HttpResponseMessage> task = client.PostAsync("http://developer.echonest.com/api/v4/track/upload", requestContent);
            HttpResponseMessage response = task.Result;
            HttpContent responseContent = response.Content;
            
            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                result=await reader.ReadToEndAsync();
                return result;
            }

        }

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
