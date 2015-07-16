using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Similitud.Web.Models;
using Accord.MachineLearning;
using System.Net;
using System.IO;
using System.Text;
using System.Data.SqlClient;

namespace Similitud.Web.Controllers
{
    public class KNNController : Controller
    {
        //
        // GET: /KNN/

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ObtenerSimilaresxTituloKNN(string titulo, string artista)
        {
            List<String> listaItems = new List<String>();
            String lista = "";
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
                //descriptoresEntrada.Add(Double.Parse(tokenSummary["liveness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["tempo"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["speechiness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["acousticness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["loudness"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["valence"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["danceability"].ToString()));
                descriptoresEntrada.Add(Double.Parse(tokenSummary["instrumentalness"].ToString()));
                descriptoresEntrada.Add(int.Parse(tokenSummary["key"].ToString()));

                listaItems = GetSimilaresDatabaseKNN(descriptoresEntrada);
                lista = string.Join(",", listaItems);
            }
            catch (Exception e)
            {
                PartialView("PlayListSpotify", lista);
            }

            return PartialView("PlayListSpotify", lista);

        }
        private List<String> GetSimilaresDatabaseKNN(List<Double> descriptoresEntrada)
        {
            ModeloSimilitudEntities db=new ModeloSimilitudEntities();
            Double[] vectorEntrada=descriptoresEntrada.ToArray();
            vectorEntrada = Normalizar(vectorEntrada);
            Double[][] matriz = csvtoMatrix("descriptoresNormalizados");

            int[] pertenencia=new int[matriz.Length];
            for(int i=0;i<pertenencia.Length;i++){
                pertenencia[i]=1;
            }
            pertenencia[23] = 2;

            KNearestNeighbors knn = new KNearestNeighbors(k: 10, inputs: matriz, outputs: pertenencia);
            int answer = knn.Compute(vectorEntrada);
            int[] a = new int[1]; a[0] = 1;
            Double[][] cercanos = knn.GetNearestNeighbors(vectorEntrada,out a);
            
            List<String> listaSimilares = new List<String>();
            List<canciones> dbcanciones = db.canciones.ToList();
            for (int i = 0; i < matriz.Length; i++)
            {
                if (cercanos.Contains(matriz[i]))
                {
                    listaSimilares.Add(dbcanciones[i].id_spotify.Substring(14));
                }
            }


                //string select="select * from canciones where energy={0} and liveness={1} and tempo={2} and speechiness={3} and acousticness={4} and loudness={5} and valence={6} and danceability={7} and instrumentalness={8} and key={9}";
                //string select2 = "select * from canciones";
                //for(int j=0;j<cercanos.Length;j++){
                //    object[] parameters = new object[10];
                //    for (int i = 0; i < 10; i++)
                //    {
                //            SqlParameter param = new SqlParameter("i", cercanos[j][i]);
                //            parameters[i] = cercanos[j][i];
                //        }
                //        var stores = db.Database.SqlQuery<canciones>(select, parameters).ToList();
                //        listaSimilares.Add(stores[0].id_spotify);
                //}


                return listaSimilares;
        }
        private Double[][] csvtoMatrix(string nombre)
        {
            List<List<Double>> matrizDescriptores = new List<List<double>>();
            var reader = new StreamReader(System.IO.File.OpenRead(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\Matlab\" + nombre + ".txt"));
            List<Double> list = new List<Double>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                List<String> values = line.Split(',').ToList();
                list = values.Select(Double.Parse).ToList();
                matrizDescriptores.Add(list);
            }
            int tam = matrizDescriptores.Count();
            Double[][] matriz=new Double[tam][];
            for (int i = 0; i < tam; i++)
            {
                matriz[i] = matrizDescriptores[i].ToArray();
            }
            return matriz;
        }
        private Double[] Normalizar(Double[] descriptoresEntrada)
        {
            Double[] listaItems = new Double[descriptoresEntrada.Length];
            Double[][] maxYmin = csvtoMatrix("maxYmin");
            for (int i = 0; i < descriptoresEntrada.Length; i++)
            {
                double valornormalizado = (descriptoresEntrada[i] - maxYmin[1][i]) / (maxYmin[0][i] - maxYmin[1][i]);
                listaItems[i]=valornormalizado;
            }
            return listaItems;
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
                System.IO.File.AppendAllText(@"C:\Users\FastSolution\Documents\Visual Studio 2013\Projects\BusquedaSimilitud\Similitud.Web\App_Data\log\registros.txt", ex + "\n\n");
                return "";
            }
        }
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



        #region Codigo Inservible
        private Double[][] Desnormalizar(Double[][] vectoresEncontrados)
        {
            Double[][] vectorDesnormalizado = new Double[vectoresEncontrados.GetLength(0)][];
            Double[][] maxYmin = csvtoMatrix("maxYmin");
            int alto = vectoresEncontrados.Length;
            for (int i = 0; i < alto; i++)
            {
                Double[] fila = new Double[10];
                for (int j = 0; j < 10; j++)
                {
                    fila[j] = (vectoresEncontrados[i][j] * (maxYmin[0][j] - maxYmin[1][j])) + maxYmin[1][j];
                }
                vectorDesnormalizado[i] = fila;
            }
            return vectorDesnormalizado;
        }
        private Double[][] getCancionesDB()
        {
            ModeloSimilitudEntities db = new ModeloSimilitudEntities();
            List<canciones> musicas = db.canciones.ToList();
            int tamano = musicas.Count;
            Double[][] listaCanciones = new Double[tamano][];
            for (int i = 0; i < musicas.Count; i++)
            {
                canciones cancion = musicas[i];
                listaCanciones[i] = new Double[] { cancion.energy, cancion.liveness, cancion.tempo, cancion.speechiness, cancion.acousticness, cancion.loudness, cancion.valence, cancion.danceability, cancion.instrumentalness, Double.Parse(cancion.key.ToString()) };
            }
            return listaCanciones;
        }
#endregion
    }
}
