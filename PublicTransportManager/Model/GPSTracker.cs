using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Provider;
using Android.Widget;
using Android.Util;
using Android.Locations;

using ALLocations = Android.Locations.Location;
using System.Threading.Tasks;
using Android.Gms.Maps.Model;

namespace PublicTransportManager
{
    class GPSTracker : Service, ILocationListener
    {
        private readonly Context mContext;
       
        bool isGPSEnabled = false;
        bool isNetworkEnabled = false;
        bool canGetLocation = false;

        ALLocations location; 

        double latitude; 
        double longitude; 

        private static readonly long MIN_DISTANCE_CHANGE_FOR_UPDATES = 10; // 10 meters

        private static readonly long MIN_TIME_BW_UPDATES = 1000 * 60 * 1; // 1 minute

        protected LocationManager locationManager;

        public GPSTracker(Context context)
        {
            mContext = context;
            GetLocation();
        }

        public ALLocations GetLocation()
        {
            try
            {
                locationManager = (LocationManager)mContext.
                    GetSystemService(LocationService);

                // getting GPS status
                isGPSEnabled = locationManager
                        .IsProviderEnabled(LocationManager.GpsProvider);

                // getting network status
                isNetworkEnabled = locationManager
                        .IsProviderEnabled(LocationManager.NetworkProvider);

                if (!isGPSEnabled && !isNetworkEnabled)
                {
                    // no network provider is enabled
                    Toast.MakeText(mContext, "Разрешете достъпа до мрежата!", ToastLength.Long).Show();
                }
                else {
                    canGetLocation = true;
                    // First get location from Network Provider
                    if (isNetworkEnabled)
                    {
                        locationManager.RequestLocationUpdates(
                                LocationManager.NetworkProvider,
                                MIN_TIME_BW_UPDATES,
                                MIN_DISTANCE_CHANGE_FOR_UPDATES, this);
                        Log.Debug("Network", "Network");
                        if (locationManager != null)
                        {
                            location = locationManager
                                    .GetLastKnownLocation(LocationManager.NetworkProvider);
                            if (location != null)
                            {
                                latitude = GetLatitude();
                                longitude = GetLongitude();
                            }
                        }
                    }

                    // if GPS Enabled get lat/long using GPS Services
                    if (isGPSEnabled)
                    {
                        if (location == null)
                        {
                            locationManager.RequestLocationUpdates(
                                    LocationManager.GpsProvider,
                                    MIN_TIME_BW_UPDATES,
                                    MIN_DISTANCE_CHANGE_FOR_UPDATES, this);
                            Log.Debug("GPS Enabled", "GPS Enabled");
                            if (locationManager != null)
                            {
                                location = locationManager
                                        .GetLastKnownLocation(LocationManager.GpsProvider);
                                if (location != null)
                                {
                                    latitude = GetLatitude();
                                    longitude = GetLongitude();
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Toast.MakeText(mContext, e.ToString(), ToastLength.Long).Show();
            }

            return location;
        }

     
        public double GetLatitude()
        {
            if (location != null)
            {
                latitude = location.Latitude;
            }

            return latitude;
        }

        public double GetLongitude()
        {
            if (location != null)
            {
                longitude = location.Longitude;
            }

            return longitude;
        }

        public async Task<string> GetAddress(LatLng positon)
        {
            var geo = new Geocoder(mContext);
            string address = "";
            var addresses = await geo.GetFromLocationAsync(positon.Latitude, positon.Longitude, 1);

            if (addresses.Any())
            {
                addresses.ToList().ForEach(addr => address = addr.GetAddressLine(0) + "\n" + addr.GetAddressLine(1) + "\n" + addr.GetAddressLine(2));
                return  address;
            }
            else {
                 return address;
            }
        }

        public bool CanGetLocation()
        {
            return canGetLocation;
        }

        public void ShowSettingsAlert()
        {
            AlertDialog.Builder alertDialog = new AlertDialog.Builder(mContext);

            alertDialog.SetTitle("GPS изкючен");   
            alertDialog.SetMessage("Не е пуснат GPS. Желаете ли да отидете в настойките?"); 
            alertDialog.SetPositiveButton("Настройки", HandlePositiveButtonClick);
            alertDialog.SetNegativeButton("Отказ", HandelNegativeButtonClick);
            alertDialog.Show();
        }

        private void HandlePositiveButtonClick(object sender, EventArgs e)
        {

            Intent intent = new Intent(Settings.ActionLocationSourceSettings);
            mContext.StartActivity(intent);
        }

        private void HandelNegativeButtonClick(object sender, EventArgs e)
        {
        }

        public void StopUsingGPS()
        {
            if (locationManager != null)
            {
                locationManager.RemoveUpdates(this);
            }

        }
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public void OnLocationChanged(ALLocations location)
        {
        }

        public void OnProviderDisabled(string provider)
        {
        }

        public void OnProviderEnabled(string provider)
        {
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
        }
    }
}