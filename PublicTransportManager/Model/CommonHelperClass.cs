using System;
using Android.Net;
using Android.Content;
using Android.Gms.Common;
using Android.Util;

namespace PublicTransportManager
{
	static class CommonHelperClass
	{
		internal static Boolean FnIsConnected(Context context)
		{
			try
			{
				var connectionManager = (ConnectivityManager)context.GetSystemService (Context.ConnectivityService); 
				NetworkInfo networkInfo = connectionManager.ActiveNetworkInfo; 
				if (networkInfo != null && networkInfo.IsConnected) 
				{
					return true;
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine ( ex.Message );
				//ensure access network state is enbled
				return false;
			}
			return false;
		}

        internal static bool FnIsGooglePlayServicesInstalled(Context context)
        {
            int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(context);
            if (queryResult == ConnectionResult.Success)
            {
                Log.Info("Closest Stop Avrivity", "Google Play Services is installed on this device.");
                return true;
            }

            if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
            {
                string errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
                Log.Error("ManActivity", "There is a problem with Google Play Services on this device: {0} - {1}", queryResult, errorString);

                // Show error dialog to let user debug google play services
            }
            return false;
        }
    }
}

