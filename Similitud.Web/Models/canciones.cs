//------------------------------------------------------------------------------
// <auto-generated>
//    Este código se generó a partir de una plantilla.
//
//    Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//    Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Similitud.Web.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class canciones
    {
        public string track_id { get; set; }
        public string title { get; set; }
        public string song_id { get; set; }
        public string artist_id { get; set; }
        public string artist_mbid { get; set; }
        public string artist_name { get; set; }
        public double duration { get; set; }
        public Nullable<double> artist_familiarity { get; set; }
        public Nullable<double> artist_hotttnesss { get; set; }
        public int year { get; set; }
        public double energy { get; set; }
        public double liveness { get; set; }
        public double tempo { get; set; }
        public double speechiness { get; set; }
        public double acousticness { get; set; }
        public double loudness { get; set; }
        public double valence { get; set; }
        public double danceability { get; set; }
        public double instrumentalness { get; set; }
        public string id_spotify { get; set; }
        public Nullable<int> key { get; set; }
    }
}
