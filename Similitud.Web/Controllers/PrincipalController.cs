using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Similitud.Web.Models;
using System.Net;
using System.IO;
using System.Text;

namespace Similitud.Web.Controllers
{
    public class PrincipalController : Controller
    {
        //
        // GET: /Principal/
        subset_artist_similarityEntities1 db = new subset_artist_similarityEntities1();
        public ActionResult Index()
        {
            List<artists> artistas = db.artists.ToList();
            String idartista = artistas[1].artist_id.ToString();
            String json = "http://developer.echonest.com/api/v4/song/search?api_key=ERYL0FA7VZ24XQMOO&format=json&results=1&artist_id=" + idartista + "&bucket=id:spotify&bucket=tracks&limit=true";
            String jsonResult = GET(json);
            return View();
        }
        string GET(string url)
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
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();
                    // log errorText
                }
                throw;
            }
        }
    }
}
