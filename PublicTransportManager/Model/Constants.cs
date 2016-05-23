using System;

namespace PublicTransportManager
{
	static class Constants
	{
        //google api keys and urls for the Directions API
		internal static string strGoogleServerKey = "AIzaSyA7NIwcidPW3RufmMiEq-SWgfm3zwnQiAg";
		internal static string strGoogleServerDirKey= "AIzaSyA7NIwcidPW3RufmMiEq-SWgfm3zwnQiAg";
		internal static string strGoogleDirectionUrl= "https://maps.googleapis.com/maps/api/directions/json?origin={0}&destination={1}&mode=walking&sensor=false&units=metric&key=" + strGoogleServerDirKey+"";
		internal static string strGeoCodingUrl="https://maps.googleapis.com/maps/api/geocode/json?{0}&key="+strGoogleServerKey+"";

        internal static string strStation = "Спирка";
		internal static string strTextMyPosition="Моята позиция";
        internal static string strDone = "Местоположението е взето";

        internal static string strException= "Exception";

        internal static string strNoInternet="Няма интернет връзка. Моля проверете интернет връзката си"; 
		internal static string strPleaseWait="Моля изчакайте...";
		internal static string strUnableToConnect="Неуспех при свързването със сървъра!\nМоля пробвайте отново по-късно";
        
    }
}

