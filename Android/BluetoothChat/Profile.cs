using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using Java.IO;
using System.IO;
using System.Threading.Tasks;
using Android.Util;

namespace BluetoothChat
{
    public class Profile
    {
        public Guid ProfileEK { get; private set; }
        public String ProfileName { get; set; }
        public List<int[]> PointsOfInterest { get; private set; }

        public Profile()
        {
            ProfileEK = Guid.NewGuid();
            PointsOfInterest = new List<int[]>();
        }

        public Profile(String json)
        {
            Profile p = JsonConvert.DeserializeObject<Profile>(json);

            ProfileEK = p.ProfileEK;
            ProfileName = p.ProfileName;
            PointsOfInterest = p.PointsOfInterest;
        }

        public int AddPointAt(int position, int duration, int target)
        {
            if(position > PointsOfInterest.Count() ||
               position < 0)
                return -1;

            PointsOfInterest.Insert(position, new int[] { duration, target });

            return 0;
        }

        public int MovePoint(int from, int to)
        {
            if(from >= PointsOfInterest.Count() ||
               from < 0 ||
               to >= PointsOfInterest.Count() ||
               to < 0)
                return -1;

            int[] movingPoint = PointsOfInterest[from];
            PointsOfInterest.RemoveAt(from);
            PointsOfInterest.Insert(to, movingPoint);

            return 0;
        }

        public int DeletePointAt(int position)
        {
            if(position >= PointsOfInterest.Count() ||
               position < 0)
                return -1;

            PointsOfInterest.RemoveAt(position);

            return 0;
        }

        public String ConvertToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public async Task<bool> SaveProfile(Context context)
        {

            try
            {
                var outputStream = context.OpenFileOutput("Profiles.txt", FileCreationMode.Private);
                await outputStream.WriteAsync(Encoding.ASCII.GetBytes(ConvertToJsonString()), 0, ConvertToJsonString().Count());
                outputStream.Close();
            }
            catch (Exception e)
            {
                Log.Debug("BluetoothChat", e.StackTrace);
                return false;
            }
            return true;
        }
    }
}