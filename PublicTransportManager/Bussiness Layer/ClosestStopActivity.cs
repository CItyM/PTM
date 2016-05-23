using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Widget;
using Android.Gms.Maps;
using Android.Locations;
using Android.Gms.Maps.Model;
using Android.Content;
using Android.Support.V4.View;
using Android.Runtime;
using Android.Views.InputMethods;

using Newtonsoft.Json;

using Clans.Fab;

using V7SearchView = Android.Support.V7.Widget.SearchView;
using v7AlertDialog = Android.Support.V7.App.AlertDialog.Builder;
using V7Toolbar = Android.Support.V7.Widget.Toolbar;
using FloatingActionButton = Clans.Fab.FloatingActionButton;
using Math = Java.Lang.Math;
using JLD = Java.Lang.Double;

namespace PublicTransportManager
{
    [Activity(Label = "@string/closest_stop", Theme = "@style/Base.Theme.DesignDemo", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class ClosestStopActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener, GoogleMap.IInfoWindowAdapter
    {
        private static readonly string mDbName = "Database.sqlite";
        private static readonly string mPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

        private DrawerLayout mDrawerLayout;
        private V7Toolbar mToolbar;
        private V7SearchView mSearchView;
        private v7AlertDialog mAlertDialog;
        private NavigationView mNavigationView;
        private SupportMapFragment mSuppMapFragment;
        private GoogleMap mGoogleMap;
        private View mInfoWindow;
        private WebClient mWebClient;
        private FloatingActionMenu mMenuFAB;
        private FloatingActionButton mGetPositionFAB;
        private FloatingActionButton mDrawRouteFAB;
        private string mCurrentPostionString;
        private string mClosestStopString;
        private string mLineId;
        private string mSnippedStation;
        private string mSnippedUser;
        private bool isSVCliced = false;
        private bool isFABRCliced = false;
        private List<LatLng> mListAllStations;
        private List<string> mAllLinesId;
        private List<string> mListLinesIdFromStation;
        private LatLng mLatLngSource;
        private LatLng mLatLngDestination;
        private LatLng mMyLocation;
        private GPSTracker mGps;
        private Stations mClosestStation;
        private DistanceDuration mDistanceDuration;
        private Database mDB;

        protected override void OnCreate(Bundle bundle)
        {

            base.OnCreate(bundle);
            if (!CommonHelperClass.FnIsGooglePlayServicesInstalled(this))
            {
                Finish();
            }

            // Set our view from the "closest_stop_layout" layout resource
            SetContentView(Resource.Layout.closest_stop_layout);

            SetUpGoogleMap();

            mToolbar = FindViewById<V7Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(mToolbar);

            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            mDrawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            mNavigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            mNavigationView.SetNavigationItemSelectedListener(this);

            mMenuFAB = (FloatingActionMenu)FindViewById(Resource.Id.menu);

            mMenuFAB.HideMenuButton(false);

            mMenuFAB.PostDelayed(() => mMenuFAB.ShowMenuButton(true), -200);

            mGetPositionFAB = (FloatingActionButton)FindViewById(Resource.Id.position_fab);

            mDrawRouteFAB = (FloatingActionButton)FindViewById(Resource.Id.route_fab);

            mDB = new Database(Path.Combine(mPath, mDbName));

            mListAllStations = mDB.AllStationsLocations();
            mAllLinesId = mDB.AllLinesID();
            mListLinesIdFromStation = new List<string>();
        }
        protected override void OnResume()
        {
            base.OnResume();
            mGetPositionFAB.Click += MGetPositionFAB_Click;
            mDrawRouteFAB.Click += MDrawRouteFAB_Click;
        }
        //------------------------------------------------------------------------
        //Map and onmap proceses set up       
        void SetUpGoogleMap()
        {
            if (!CommonHelperClass.FnIsConnected(this))
            {
                Toast.MakeText(this, Constants.strNoInternet, ToastLength.Short).Show();
                return;
            }
            mSuppMapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map);
            var mapReadyCallback = new OnMapReadyClass();
            mapReadyCallback.MapReadyAction += delegate (GoogleMap map)
            {
                mGoogleMap = map;
                UpdateCameraPosition(new LatLng(42.697708, 23.321802), 10);
                map.SetInfoWindowAdapter(this);
            };

            mSuppMapFragment.GetMapAsync(mapReadyCallback);

        }
        private async void ProcessOnMap()
        {
            await LocationToLatLng();

            UpdateCameraPosition(mLatLngDestination, 15);

            await FromMethodToVariable();

            if (mLatLngSource != null && mLatLngDestination != null)
                DrawPath(mCurrentPostionString, mClosestStopString);

        }
        private async void DrawPath(string strSource, string strDestination)
        {
            string strFullDirectionURL = string.Format(Constants.strGoogleDirectionUrl, strSource, strDestination);
            string strJSONDirectionResponse = await FnHttpRequest(strFullDirectionURL);

            if (strJSONDirectionResponse != Constants.strException)
            {
                RunOnUiThread(() =>
                {
                    if (mGoogleMap != null)
                    {
                        mGoogleMap.Clear();
                        MarkOnMap(Constants.strTextMyPosition
                                 , mSnippedUser
                                 , mLatLngSource
                                 , Resource.Drawable.ic_location);
                        MarkOnMap(Constants.strStation
                                 , mSnippedStation
                                 , new LatLng(mClosestStation.CoordinatesX, mClosestStation.CoordinatesY)
                                 , Resource.Drawable.ic_bus_stop_pointer);
                    }
                });
                SetDirectionQuery(strJSONDirectionResponse);
            }
            else
            {
                RunOnUiThread(() =>
                   Toast.MakeText(this, Constants.strUnableToConnect, ToastLength.Short).Show());
            }

        }
        private void SetDirectionQuery(string strJSONDirectionResponse)
        {
            var objRoutes = JsonConvert.DeserializeObject<GoogleDirectionClass>(strJSONDirectionResponse);

            //objRoutes.routes.Count  --may be more then one 
            if (objRoutes.routes.Count > 0)
            {
                string encodedPoints = objRoutes.routes[0].overview_polyline.points;

                var lstDecodedPoints = DecodePolylinePoints(encodedPoints);
                //convert list of location point to array of latlng type
                var latLngPoints = new LatLng[lstDecodedPoints.Count];
                int index = 0;
                foreach (Location loc in lstDecodedPoints)
                {
                    latLngPoints[index++] = new LatLng(loc.lat, loc.lng);
                }

                var polylineoption = new PolylineOptions();
                polylineoption.InvokeColor(Android.Graphics.Color.Red);
                polylineoption.Geodesic(true);
                polylineoption.Add(latLngPoints);
                RunOnUiThread(() =>
                mGoogleMap.AddPolyline(polylineoption));

            }

        }


        private List<Location> DecodePolylinePoints(string encodedPoints)
        {
            if (string.IsNullOrEmpty(encodedPoints))
                return null;
            var poly = new List<Location>();
            char[] polylinechars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;

            try
            {
                while (index < polylinechars.Length)
                {
                    // calculate next latitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length)
                        break;

                    currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                    //calculate next longitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length && next5bits >= 32)
                        break;

                    currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                    Location p = new Location();
                    p.lat = Convert.ToDouble(currentLat) / 100000.0;
                    p.lng = Convert.ToDouble(currentLng) / 100000.0;
                    poly.Add(p);
                }
            }
            catch
            {
                RunOnUiThread(() =>
                  Toast.MakeText(this, Constants.strPleaseWait, ToastLength.Short).Show());
            }
            return poly;
        }

        private async Task<bool> LocationToLatLng()
        {
            try
            {
                var geo = new Geocoder(this);
                var sourceAddress = await geo.GetFromLocationNameAsync(mCurrentPostionString, 1);
                sourceAddress.ToList().ForEach((addr) =>
                {
                    mLatLngSource = new LatLng(addr.Latitude, addr.Longitude);
                });

                var destAddress = await geo.GetFromLocationNameAsync(mClosestStopString, 1);
                destAddress.ToList().ForEach((addr) =>
                {
                    mLatLngDestination = new LatLng(addr.Latitude, addr.Longitude);
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void MarkOnMap(string title, string snippet, LatLng pos, int resourceId)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var marker = new MarkerOptions();
                    marker.SetPosition(pos);
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(resourceId));
                    marker.SetTitle(title);
                    marker.SetSnippet(snippet);


                    mGoogleMap.AddMarker(marker);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private void UpdateCameraPosition(LatLng pos, int zoomIndex)
        {
            try
            {
                CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
                builder.Target(pos);
                builder.Zoom(zoomIndex);
                builder.Bearing(45);
                builder.Tilt(10);
                CameraPosition cameraPosition = builder.Build();
                CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
                mGoogleMap.AnimateCamera(cameraUpdate);
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);

            }
        }

        private async Task<string> FnHttpRequest(string strUri)
        {
            mWebClient = new WebClient();
            string strResultData;
            try
            {
                strResultData = await mWebClient.DownloadStringTaskAsync(new Uri(strUri));
                Console.WriteLine(strResultData);
            }
            catch
            {
                strResultData = Constants.strException;
            }
            finally
            {
                if (mWebClient != null)
                {
                    mWebClient.Dispose();
                    mWebClient = null;
                }
            }

            return strResultData;
        }

        private string FnHttpRequestOnMainThread(string strUri)
        {
            mWebClient = new WebClient();
            string strResultData;
            try
            {
                strResultData = mWebClient.DownloadString(new Uri(strUri));
                Console.WriteLine(strResultData);
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                strResultData = Constants.strException;
            }
            finally
            {
                if (mWebClient != null)
                {
                    mWebClient.Dispose();
                    mWebClient = null;
                }
            }

            return strResultData;
        }

        public View GetInfoContents(Marker marker)
        {

            mInfoWindow = LayoutInflater.Inflate(Resource.Layout.info_window, null, false);
            TextView title = (TextView)mInfoWindow.FindViewById(Resource.Id.title);
            title.Text = marker.Title;

            TextView snipet = (TextView)mInfoWindow.FindViewById(Resource.Id.snippet);
            snipet.Text = marker.Snippet;

            return mInfoWindow;

        }

        public View GetInfoWindow(Marker marker)
        {
            return null;
        }

        //--------------------------------------------------------------------------------------------------------------------------------
        //Calculating distance
        private string LatLngToString(double latitude, double longitude)
        {
            //transforming ',' form the basic ToSting() to '.', needed for path drawing
            string original = latitude + "," + longitude;
            string[] parts = original.Split(new char[] { ',' });
            string result = string.Format("{0}.{1}, {2}.{3}", parts[0], parts[1], parts[2], parts[3]);

            return result;
        }

        private string CalcClosest(List<LatLng> stationsList, LatLng myLocation)
        {
            double max = 9999999999999999999999999999999999.0;
            int stationID;
            LatLng closestLatLng = new LatLng(0.0, 0.0);
            string result;

            for (int i = 0; i < stationsList.Count; i++)
            {
                if (DistFrom(myLocation.Latitude, myLocation.Longitude, stationsList[i].Latitude, stationsList[i].Longitude) < max)
                {
                    max = DistFrom(myLocation.Latitude, myLocation.Longitude, stationsList[i].Latitude, stationsList[i].Longitude);
                    closestLatLng = new LatLng(stationsList[i].Latitude, stationsList[i].Longitude);
                }
            }
            result = LatLngToString(closestLatLng.Latitude, closestLatLng.Longitude);
            //get the id of the closest station from the database
            stationID = mDB.GetStationID(closestLatLng.Latitude, closestLatLng.Longitude);
            //get the closest station
            mClosestStation = mDB.GetStation(stationID);
            //get the id's of the lines passes through the station
            mListLinesIdFromStation = mDB.LinesIDFromStationsID(stationID);

            return result;
        }

        //get the distance to the closest stop matematicaly 
        private double DistFrom(double latStart, double lngStart, double latEnd, double lngEnd)
        {
            double earthRadius = 3958.75;
            double dLat = Math.ToRadians(latEnd - latStart);
            double dLng = Math.ToRadians(lngEnd - lngStart);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(Math.ToRadians(latStart)) * Math.Cos(Math.ToRadians(latEnd)) *
            Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double dist = earthRadius * c;

            int meterConversion = 1609;

            //using Java.Lang.Double
            return new JLD(dist * meterConversion).DoubleValue();
        }

        //get the distance and the travel dirations form the json info recieve from the server
        private async Task<DistanceDuration> DistatnceNDurationJSON(LatLng mypos, LatLng dest)
        {

            string strFullDirectionURLForDD = string.Format(Constants.strGoogleDirectionUrl
                                                     , LatLngToString(mypos.Latitude, mypos.Longitude)
                                                     , LatLngToString(dest.Latitude, dest.Longitude));

            string strJSONDirectionResponse = await FnHttpRequest(strFullDirectionURLForDD);

            //meake a query to the server
            var objRoutes = JsonConvert.DeserializeObject<GoogleDirectionClass>(strJSONDirectionResponse);

            string routeLenght = objRoutes.routes[0].legs[0].distance.value.ToString();
            string travelDuration = objRoutes.routes[0].legs[0].duration.value.ToString();

            return new DistanceDuration { Distance = routeLenght, Duration = travelDuration };

        }



        //mthod who makes the activity to wait unil recieve data from the request
        private async Task<bool> FromMethodToVariable()
        {
            try
            {
                mDistanceDuration = await DistatnceNDurationJSON(mMyLocation,
                    new LatLng(mClosestStation.CoordinatesX, mClosestStation.CoordinatesY));

                mSnippedUser = await mGps.GetAddress(mLatLngSource);
                mSnippedStation = mClosestStation.Name + "- " + mDistanceDuration.Distance + " метра, " + mDistanceDuration.Duration + " минути";
                if (isSVCliced == true)
                {
                    string strMessage = "Най- близката спирка до Вас, oт линя "
                                       + mLineId + " e "
                                       + mClosestStation.Name + "\nТя се намира на "
                                       + mDistanceDuration + " от Вас.";

                    ShowAlert("Инфо", strMessage);
                    isSVCliced = false;
                }

                if (isFABRCliced == true)
                {
                    string strMessage = "";

                    foreach (var l in mListLinesIdFromStation)
                    {
                        mLineId += l + " ";
                    }

                    if (mListLinesIdFromStation.Count == 1)
                    {
                        strMessage = "Най- близката спирка до Вас е " +
                                      mClosestStation.Name + "\nТя се намира на " +
                                      mDistanceDuration.ToString() + " време от Вас.\nПрез нея минава линия: " +
                                      mLineId;
                    }
                    else {
                        strMessage = "Най- близката спирка до Вас е " +
                                           mClosestStation.Name + "\nТя се намира на " +
                                           mDistanceDuration + " време от Вас.\nПрез нея минават линии: " +
                                           mLineId;
                    }


                    ShowAlert("Инфо", strMessage);

                    isFABRCliced = false;

                }
                //reset the string 
                mLineId = "";
                //clear the list
                for (int i = 0; i < mListLinesIdFromStation.Count; i++)
                {
                    mListLinesIdFromStation.RemoveAt(i);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------
        //UI set up
        private void MGetPositionFAB_Click(object sender, EventArgs e)
        {
            // create class object
            mGps = new GPSTracker(this);

            // check if GPS enabled     
            if (mGps.CanGetLocation())
            {

                double latitude = mGps.GetLatitude();
                double longitude = mGps.GetLongitude();

                mMyLocation = new LatLng(latitude, longitude);

                string result = LatLngToString(latitude, longitude);

                mCurrentPostionString = result;

                Toast.MakeText(this, Constants.strDone, ToastLength.Long).Show();
            }
            else {
                // can't get location because GPS or Network is not enabled
                // Ask user to enable GPS/network in settings
                mGps.ShowSettingsAlert();
            }

        }

        private void MDrawRouteFAB_Click(object sender, EventArgs e)
        {
            isFABRCliced = true;
            try
            {
                mClosestStopString = CalcClosest(mListAllStations, mMyLocation);

                ProcessOnMap();

            }
            catch (Exception)
            {
                ShowAlert("Грешка", "Моля, първо проверете местоположението си");
            }

        }

        private void ShowAlert(string title, string message)
        {
            mAlertDialog = new v7AlertDialog(this);
            try
            {

                mAlertDialog.SetTitle(title);
                mAlertDialog.SetMessage(message);
                mAlertDialog.SetNeutralButton("OK", HandleNeutralButtonClick);
                mAlertDialog.Show();
            }
            catch (Exception)
            {
            }

        }

        private void HandleNeutralButtonClick(object sender, DialogClickEventArgs e)
        {
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.sample_actions, menu);

            var item = menu.FindItem(Resource.Id.action_search);

            var searchView = MenuItemCompat.GetActionView(item);

            mSearchView = searchView.JavaCast<V7SearchView>();

            ((EditText)searchView.FindViewById(Resource.Id.search_src_text)).SetHint(Resource.String.search_hint);

            mSearchView.QueryTextSubmit += MSearchView_QueryTextSubmit;

            return true;
        }

        private void MSearchView_QueryTextSubmit(object sender, V7SearchView.QueryTextSubmitEventArgs e)
        {
            isSVCliced = true;
            mLineId = e.Query.Trim();
            mGoogleMap.Clear();
            try
            {
                mClosestStopString = CalcClosest(mDB.GetAllStationsLocationFromLine(mLineId), mMyLocation);

                mGoogleMap.Clear();
                ProcessOnMap();


            }
            catch (Exception)
            {
                bool flag = false;
                for (int i = 0; i < mAllLinesId.Count; i++)
                {
                    if (mLineId == mAllLinesId[i])
                    {
                        flag = true;
                    }
                }
                if (flag == true)
                {
                    ShowAlert("Грешка", "Моля, първо проверете местоположението си");
                    flag = false;
                }
                else
                {
                    Toast.MakeText(this, "Грешна линия", ToastLength.Short).Show();
                    flag = false;
                }
                
            }
            View view = CurrentFocus;
            if (view != null)
            {
                InputMethodManager imm = (InputMethodManager)GetSystemService(InputMethodService);
                imm.HideSoftInputFromWindow(view.WindowToken, 0);
            }

        }

        //private void Get
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    mDrawerLayout.OpenDrawer(GravityCompat.Start);
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public bool OnNavigationItemSelected(IMenuItem menuItem)
        {
            if (menuItem.IsChecked)
            {
                menuItem.SetChecked(false);
            }
            else
            {
                menuItem.SetChecked(true);
            }

            mDrawerLayout.CloseDrawers();

            switch (menuItem.ItemId)
            {
                case Resource.Id.nav_check_line:
                    StartActivity(typeof(CheckLineActivity));
                    return true;
            }
            return true;
        }


    }
}
