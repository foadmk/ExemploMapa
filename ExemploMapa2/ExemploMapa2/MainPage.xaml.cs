using Newtonsoft.Json;
using Plugin.Geolocator;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;

namespace ExemploMapa2
{
    public partial class MainPage : ContentPage
	{
        Pin destino, voce;

        Plugin.Geolocator.Abstractions.IGeolocator locator;

        public MainPage()
		{
			InitializeComponent();

            ServicePointManager
            .ServerCertificateValidationCallback +=
            (sender, cert, chain, sslPolicyErrors) => true;

            locator = CrossGeolocator.Current;
            locator.StartListeningAsync(TimeSpan.FromSeconds(30), 10.0);
            locator.PositionChanged += (object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e) => gps_position_changed(sender, e);

            map.IsEnabled = false;


        }

        private void gps_position_changed(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
        {
            voce = generate_pin(e.Position.Latitude, e.Position.Longitude, "VOCE", "Você", Color.Green);
            updatePins();
        }

        private void updatePins()
        {
            map.Pins.Clear();
            map.Pins.Add(voce);
            if (destino != null) map.Pins.Add(destino);
        }

        private Pin generate_pin(double lat, double lng, string id, string code, Color cor)
        {
            return new Pin
            {
                Type = PinType.Place,
                Icon = BitmapDescriptorFactory.DefaultMarker(cor),
                Position = new Position(lat, lng),
                Tag = id,
                Label = code,
                IsDraggable = true
            };
        }
        async protected override void OnAppearing()
        {

            var gps = await locator.GetPositionAsync();

            map.MoveToRegion(new MapSpan(new Position(gps.Latitude, gps.Longitude), 0.001, 0.001), false);

            map.IsEnabled = true;

            base.OnAppearing();
        }

        private void atualizar()
        {
            destino = generate_pin(-25.4360807, -49.2743035, "DESTINO", "Praça Rui Barbosa", Color.Red);
            updateRoute();
        }

        async private void updateRoute()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en");

            HttpClient c = new HttpClient();

            string url = $"http://maps.googleapis.com/maps/api/directions/json?origin={voce.Position.Latitude},{voce.Position.Longitude}&destination={destino.Position.Latitude},{destino.Position.Longitude}&mode=walking";

            try
            {
                HttpResponseMessage response =
                    await c.GetAsync(url);

                var resp = JsonConvert.DeserializeObject<RouteObject>(await response.Content.ReadAsStringAsync());

                Xamarin.Forms.GoogleMaps.Polyline polyline = new Xamarin.Forms.GoogleMaps.Polyline();

                int distancia = 0;

                polyline.Positions.Add(new Position(voce.Position.Latitude, voce.Position.Longitude));

                if (resp.routes != null && resp.routes.Count > 0)
                { 
                    foreach (var route in resp.routes[0].legs[0].steps)
                    {
                        polyline.Positions.Add(new Position(route.start_location.lat, route.start_location.lng));
                        polyline.Positions.Add(new Position(route.end_location.lat, route.end_location.lng));
                        distancia += route.distance.value;
                    }
                }

                polyline.Positions.Add(new Position(destino.Position.Latitude, destino.Position.Longitude));

                lblDistance.Text = $"Distância Entre Você e o Destino {distancia} metros";

                polyline.StrokeColor = Color.Aqua;
                polyline.StrokeWidth = 10f;

                map.Polylines.Clear();

                map.Polylines.Add(polyline);
            }
            catch (Exception e)
            {
                await DisplayAlert("Erro", e.Message, "OK");
            }

            updatePins();

        }

        async private void map_PinDragEnd(Object sender, PinDragEventArgs e)
        {
            if (await DisplayAlert(e.Pin.Label, "Confirma Alteração na Posição do(a) "+ e.Pin.Label, "Sim", "Não"))
            {
                switch (e.Pin.Tag)
                {
                    case "DESTINO":
                        destino = e.Pin;
                        break;
                }
            }
            else
            {
                switch (e.Pin.Tag)
                {
                    case "DESTINO":
                        destino = generate_pin(-25.4360807, -49.2743035, "DESTINO", "Praça Rui Barbosa", Color.Red);
                        break;
                }
            }

            updateRoute();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var b = sender as Button;
            switch (b.Text)
            {
                case "Street":
                    map.MapType = MapType.Street;
                    break;
                case "Hybrid":
                    map.MapType = MapType.Hybrid;
                    break;
                case "Satellite":
                    map.MapType = MapType.Satellite;
                    break;
                default:
                    atualizar();
                    break;

            }
        }
    }
}
