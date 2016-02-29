using System;
using System.Net;
using System.Web;

/* 
 * Sample code to obtain location info (COUNTRY/ZIP/CITY/STREET ADDRESS from a geo code 
 * You have to add a reference to System.Web for UrlEncode
 */

/* (c) "Neil Young" (neil.young@freenet.de)
 * 
 * This script/program is provided "as is".
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * GNU General Public License, see <http://www.gnu.org/licenses/>.
 */

namespace csShared.Geo
{
  public class Geocoding
  {
    static string MY_GOOGLE_API_KEY = "ABQIAAAA61Zfghqchn1_2tR1VCcfbxSaaQaIH1503oSfMVAmBYkKztETKBQDKdINsFaCvje_Y3lr70o0f-ipnQ"; //"ABQIAAAA61Zfghqchn1_2tR1VCcfbxR1nvaVR8ae5x-Yr3p8_vANKZKcAxTH7J0OcDSku_ocWgHLtP4X01xUnw";


    public static string Reverse(KmlPoint from, KmlPoint to)
    {

      try
      {
        String url = String.Format("http://maps.google.com/maps/nav?hl=nl&gl=nl&output=csv&oe=utf-8&key={0}&q=", MY_GOOGLE_API_KEY);
        String parms = String.Format("from:{0}, {1} to:{2}, {3}",
            from.Latitude,
            from.Longitude,
            to.Latitude,
            to.Longitude);
        parms = HttpUtility.UrlEncode(parms);

        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(new Uri(url + parms));
        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        System.IO.StreamReader sr = new System.IO.StreamReader(res.GetResponseStream());
        String res_s = sr.ReadToEnd();
        /*
         * res_s is JSON. Parse it
         */
        res.Close();
        return res_s;
      }
      catch
      {
        Console.WriteLine("Error connecting to google");
        return "";
      }
    }
  }
}
