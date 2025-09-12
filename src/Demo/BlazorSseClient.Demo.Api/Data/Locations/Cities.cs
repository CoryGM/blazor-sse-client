namespace BlazorSseClient.Demo.Api.Data.Locations
{
    public static class Cities
    {
        private readonly static List<Location> _cities =
        [
            new Location("New York, USA", 40.7128, -74.0060),
            new Location("Los Angeles, USA", 34.0522, -118.2437),
            new Location("Chicago, USA", 41.8781, -87.6298),
            new Location("Houston, USA", 29.7604, -95.3698),
            new Location("Phoenix, USA", 33.4484, -112.0740),
            new Location("Philadelphia, USA", 39.9526, -75.1652),
            new Location("San Antonio, USA", 29.4241, -98.4936),
            new Location("San Diego, USA", 32.7157, -117.1611),
            new Location("Dallas, USA", 32.7767, -96.7970),
            new Location("San Jose, USA", 37.3382, -121.8863),
            new Location("London, UK", 51.5074, -0.1278),
            new Location("Berlin, Germany", 52.5200, 13.4050),
            new Location("Tokyo, Japan", 35.6895, 139.6917),
            new Location("Sydney, Australia", -33.8688, 151.2093),
            new Location("Toronto, Canada", 43.651070, -79.347015),
            new Location("Vancouver, Canada", 49.2827, -123.1207),
            new Location("Paris, France", 48.8566, 2.3522),
            new Location("Rome, Italy", 41.9028, 12.4964),
            new Location("Madrid, Spain", 40.4168, -3.7038),
            new Location("Amsterdam, Netherlands", 52.3676, 4.9041),
            new Location("Beijing, China", 39.9042, 116.4074),
            new Location("Moscow, Russia", 55.7558, 37.6173),
            new Location("Dubai, UAE", 25.2048, 55.2708),
            new Location("Mumbai, India", 19.0760, 72.8777),
            new Location("Cape Town, South Africa", -33.9249, 18.4241),
            new Location("Buenos Aires, Argentina", -34.6037, -58.3816),
            new Location("Sao Paulo, Brazil", -23.5505, -46.6333),
            new Location("Mexico City, Mexico", 19.4326, -99.1332),
            new Location("Seoul, South Korea", 37.5665, 126.9780),
            new Location("Singapore", 1.3521, 103.8198),
            new Location("Bangkok, Thailand", 13.7563, 100.5018),
            new Location("Jakarta, Indonesia", -6.2088, 106.8456),
            new Location("Istanbul, Turkey", 41.0082, 28.9784),
            new Location("Cairo, Egypt", 30.0444, 31.2357),
            new Location("Minneapolis, USA", 44.9778, -93.2650),
            new Location("Seattle, USA", 47.6062, -122.3321),
            new Location("Miami, USA", 25.7617, -80.1918),
            new Location("Boston, USA", 42.3601, -71.0589),
            new Location("Atlanta, USA", 33.7490, -84.3880),
            new Location("Milwaukee, USA", 43.0389, -87.9065),
            new Location("Denver, USA", 39.7392, -104.9903),
            new Location("Detroit, USA", 42.3314, -83.0458),
            new Location("St. Paul, USA", 44.9537, -93.0900)
        ];

        /// <summary>
        /// Return the full list of cities in alphabetical order.
        /// </summary>
        public static IEnumerable<Location> GetCities => [.. _cities.OrderBy(x => x.Name)];

        /// <summary>
        /// Gets a random city from the list.
        /// </summary>
        /// <returns></returns>
        public static Location GetRandomCity()
        {
            var rnd = new Random();
            int index = rnd.Next(_cities.Count);
            return _cities[index];
        }
    }
}
