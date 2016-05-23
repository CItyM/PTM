using System;
using System.IO;
using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V4.Widget;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Widget;
using Android.Support.V4.View;
using Android.Runtime;
using Android.Views.InputMethods;
using Android.Content;
using Clans.Fab;

using V7Toolbar = Android.Support.V7.Widget.Toolbar;
using V7SearchView = Android.Support.V7.Widget.SearchView;
using FloatingActionButton = Clans.Fab.FloatingActionButton;

namespace PublicTransportManager
{
    [Activity(Label = "@string/check_line", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/Base.Theme.DesignDemo", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class CheckLineActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener, GoogleMap.IInfoWindowAdapter
    {
        private static readonly string mDbName = "Database.sqlite";
        private static readonly string mPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);


        private V7Toolbar mToolbar;
        private V7SearchView mSearchView;
        private GoogleMap mGoogleMap;
        private DrawerLayout mDrawerLayout;
        private NavigationView mNavigationView;
        private SupportMapFragment mSuppMapFragment;
        private FloatingActionButton mGetPositionFAB;
        private Marker mMyPositon;
        private LatLng mLatLngSource;
        private LatLng mLatLngDestination;
        private Database mDb;
        private GPSTracker mGps;
        private View mInfoWindow;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            CopyDatabase(mDbName);

            // Set our view from the "check_line_layout" layout resource
            SetContentView(Resource.Layout.check_line_layout);

            mToolbar = FindViewById<V7Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(mToolbar);

            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            mDrawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            mNavigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            mNavigationView.SetNavigationItemSelectedListener(this);

            SetUpGoogleMap();

            mDb = new Database(System.IO.Path.Combine(mPath, mDbName));
            mGetPositionFAB = (FloatingActionButton)FindViewById(Resource.Id.to_my_position_fab);
            mGetPositionFAB.Click += MGetPositionFAB_Click;
        }

        //set up the google map
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
        private void MGetPositionFAB_Click(object sender, EventArgs e)
        {
            if (mMyPositon!=null)
            {
                mMyPositon.Remove();
            }
            // create class object
            mGps = new GPSTracker(this);

            // check if GPS enabled     
            if (mGps.CanGetLocation())
            {

                double latitude = mGps.GetLatitude();
                double longitude = mGps.GetLongitude();
                UpdateCameraPosition(new LatLng(latitude, longitude), 20);
                mMyPositon = mGoogleMap.AddMarker(new MarkerOptions()
                    .SetPosition(new LatLng(latitude, longitude))
                    .SetTitle("Аз")
                    .SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.ic_location)));
            }
            else {
                // can't get location because GPS or Network is not enabled
                // Ask user to enable GPS/network in settings
                mGps.ShowSettingsAlert();
            }

        }

        //decode the encoded polyline route to a list of LatLng points
        private List<LatLng> DecodePolylinePoints(string encodedPoints)
        {
            if (encodedPoints == null || encodedPoints == "") return null;
            List<LatLng> poly = new List<LatLng>();
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
                    LatLng p = new LatLng(Convert.ToDouble(currentLat) / 100000.0,
                        Convert.ToDouble(currentLng) / 100000.0);
                    poly.Add(p);
                }
            }
            catch (Exception)
            {
            }
            return poly;
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
            string lineId = e.Query.Trim();
            mGoogleMap.Clear();
            try
            {
                DrawLinesRoute(lineId);
            }
            catch (Exception)
            {
                Toast.MakeText(this, "Грешна линия", ToastLength.Short).Show();
                mSearchView.ClearAnimation();
            }
            View view = this.CurrentFocus;
            if (view != null)
            {
                InputMethodManager imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(view.WindowToken, 0);
            }
        }

        private void DrawLinesRoute(string lineId)
        {

            var stationsList = mDb.AllSationsFormLine(lineId);
            int middleStation = stationsList.Count / 2;
            foreach (var s in stationsList)
            {
                MarkOnMap(s.Name + "\n" + s.Sign, new LatLng(s.CoordinatesX, s.CoordinatesY), Resource.Drawable.ic_bus_stop_pointer);
            }

            var lineRoute = mDb.GetLine(lineId);
            var points = DecodePolylinePoints(lineRoute.Description);

            for (int i = 0; i < points.Count - 1; i++)
            {
                mLatLngSource = points[i];
                mLatLngDestination = points[i + 1];

                mGoogleMap.AddPolyline(new PolylineOptions().Add(
                          mLatLngSource,
                          mLatLngDestination
                        ).InvokeWidth(5).InvokeColor(Color.Blue).Geodesic(true));
            }
            UpdateCameraPosition(new LatLng(stationsList[middleStation].CoordinatesX, stationsList[middleStation].CoordinatesY), 10);
        }
        void UpdateCameraPosition(LatLng pos, int zoomIndex)
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

        public View GetInfoContents(Marker marker)
        {

            mInfoWindow = LayoutInflater.Inflate(Resource.Layout.info_window, null, false);
            TextView title = (TextView)mInfoWindow.FindViewById(Resource.Id.title);
            title.Text = marker.Title;
                 
            return mInfoWindow;

        }

        public View GetInfoWindow(Marker marker)
        {
            return null;
        }
        void MarkOnMap(string title, LatLng pos, int resourceId)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var marker = new MarkerOptions();
                    marker.SetTitle(title);
                    marker.SetPosition(pos);
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(resourceId));
                    
                    mGoogleMap.AddMarker(marker);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }
        
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    mDrawerLayout.OpenDrawer(GravityCompat.Start);
                    return true;
                case Resource.Id.action_search:
                    OnSearchRequested();
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
                case Resource.Id.nav_stop:
                    StartActivity(typeof(ClosestStopActivity));
                    return true;
            }


            return true;
        }


        private void CopyDatabase(string dataBaseName)
        {
            var dbPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), dataBaseName);

            if (!File.Exists(dbPath))
            {
                var dbAssetStream = Assets.Open(dataBaseName);
                var dbFileStream = new System.IO.FileStream(dbPath, System.IO.FileMode.OpenOrCreate);
                var buffer = new byte[1024];

                int b = buffer.Length;
                int length;

                while ((length = dbAssetStream.Read(buffer, 0, b)) > 0)
                {
                    dbFileStream.Write(buffer, 0, length);
                }

                dbFileStream.Flush();
                dbFileStream.Close();
                dbAssetStream.Close();
            }

        }


    }
}