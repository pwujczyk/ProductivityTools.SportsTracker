﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProductivityTools.SportsTracker.App.Dto;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ProductivityTools.SportsTracker.App
{
    public class Application
    {
        string UserName, Password;
        private string Address = "https://api.sports-tracker.com/apiserver/v1/";

        HttpClient client;

        HttpClient AnonymousClient
        {
            get
            {
                if (client == null)
                {
                    client = new HttpClient(new LoggingHandler(new HttpClientHandler()));

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                }
                return client;
            }
        }

        HttpClient Client
        {
            get
            {
                if (AnonymousClient.DefaultRequestHeaders.Contains("STTAuthorization") == false)
                {
                    string sessionKey = Login(this.UserName, this.Password);
                    client.DefaultRequestHeaders.Add("STTAuthorization", sessionKey);
                }

                return AnonymousClient;
            }
        }

        public Application(string username, string password)
        {
            this.UserName = username;
            this.Password = password;
        }

        private Uri GetUri(string end)
        {
            Uri url = new Uri($"{Address}{end.Trim('/')}");
            return url;
        }

        public string Login(string login, string password)
        {

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("l", login),
                new KeyValuePair<string, string>("p", password)
            });


            var anonymous = new { l = login, p = password };
            var dataAsString = JsonConvert.SerializeObject(anonymous);
            var content = new StringContent(dataAsString, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = AnonymousClient.PostAsync(GetUri("login"), formContent).Result;
            var resultAsString = response.Content.ReadAsStringAsync().Result;
            JObject jobject = (JObject)JsonConvert.DeserializeObject(resultAsString);
            string sessionKey = jobject["sessionkey"].ToString();
            return sessionKey;
        }

        public List<Training> GetTrainingList()
        {
            var trainings = new List<Training>();
            string resultAsString = Client.GetAsync(GetUri("workouts?limited=true&limit=1000000")).Result.Content.ReadAsStringAsync().Result;
            Rootobject jobject = JsonConvert.DeserializeObject<Rootobject>(resultAsString);
            foreach (var sttraining in jobject.payload)
            {
                var training = new Training();
                training.StartDate = sttraining.StartDate();
                training.Distance = Math.Round(sttraining.totalDistance / 1000, 2);
                training.TrainingType = (TrainingType)sttraining.activityId;
                trainings.Add(training);
            }
            return trainings;
        }
    }
}

