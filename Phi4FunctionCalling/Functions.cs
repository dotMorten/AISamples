using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Devices.Geolocation;

namespace Phi4ConsoleApp
{
    public class Functions
    {
        [KernelFunction, Description("Fetches weather updates for a given city using the RapidAPI Weather API.")]
        public string get_weather_updates([Description("The name of the city for which to retrieve weather information.")] string city) => "Sunny, 78F, clear skies";
        /*
        [KernelFunction, Description("Retrieves the current time in UTC.")]
        public string GetCurrentUtcTime() => DateTime.UtcNow.ToString("R");
        
        [KernelFunction, Description("Retrieves the current time in the local time zone.")]
        public string GetCurrentLocalTime() => DateTime.Now.ToString("R");

        [KernelFunction, Description("Returns the location of the user.")]
        public async Task<string> GetUserLocation()
        {
            Debug.Write($"Function: GetUserLocation()");

            Geolocator locator = new Geolocator();
            var location = await locator.GetGeopositionAsync();
            var response = $"Latitude: {location.Coordinate.Latitude}, Longitude: {location.Coordinate.Longitude}";
            Debug.WriteLine(response);
            return response;
        }

        [KernelFunction, Description("Returns the distance in kilometers between two points described by their latitudes and longitudes")]
        public double GetDistance(double latitudeFrom, double longitudeFrom, double latitudeTo, double longitudeTo)
        {
            return GetDistanceVincenty(latitudeFrom, longitudeFrom, latitudeTo, longitudeTo, 6378137, 6356752.31424518) / 1000; //Uses WGS84 values
        }*/

        private const double D2R = 0.01745329251994329576923690768489; //Degrees to radians

        private static double GetDistanceVincenty(double lat1, double lon1, double lat2, double lon2, double semiMajor, double semiMinor)
        {
            var a = semiMajor;
            var b = semiMinor;
            var f = (a - b) / a; //flattening
            var L = (lon2 - lon1) * D2R;
            var U1 = Math.Atan((1 - f) * Math.Tan(lat1 * D2R));
            var U2 = Math.Atan((1 - f) * Math.Tan(lat2 * D2R));
            var sinU1 = Math.Sin(U1);
            var cosU1 = Math.Cos(U1);
            var sinU2 = Math.Sin(U2);
            var cosU2 = Math.Cos(U2);

            double lambda = L;
            double lambdaP;
            double cosSigma, cosSqAlpha, sinSigma, cos2SigmaM, sigma, sinLambda, cosLambda;

            int iterLimit = 100;
            do
            {
                sinLambda = Math.Sin(lambda);
                cosLambda = Math.Cos(lambda);
                sinSigma = Math.Sqrt((cosU2 * sinLambda) * (cosU2 * sinLambda) +
                    (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) * (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));
                if (sinSigma == 0)
                    return 0;  // co-incident points

                cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
                sigma = Math.Atan2(sinSigma, cosSigma);
                double sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
                cosSqAlpha = 1 - sinAlpha * sinAlpha;
                cos2SigmaM = cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;
                if (double.IsNaN(cos2SigmaM))
                    cos2SigmaM = 0;  // equatorial line: cosSqAlpha=0 (§6)
                double C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));
                lambdaP = lambda;
                lambda = L + (1 - C) * f * sinAlpha *
                    (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
            } while (Math.Abs(lambda - lambdaP) > 1e-12 && --iterLimit > 0);

            if (iterLimit == 0) return double.NaN;  // formula failed to converge

            var uSq = cosSqAlpha * (a * a - b * b) / (b * b);
            var A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
            var B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
            var deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
                B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM * cos2SigmaM)));
            var s = b * A * (sigma - deltaSigma);

            s = Math.Round(s, 3); // round to 1mm precision
            return s;
        }
    }
}
