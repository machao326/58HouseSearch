﻿using System;
using System.Collections.Generic;
using System.Linq;
using HouseMapAPI.Common;
using HouseMapAPI.Dapper;
using HouseMapAPI.Models;
using HouseMapAPI.Service;
using Newtonsoft.Json.Linq;

namespace HouseMapAPI.Service
{
    public class DashboardService
    {
        private RedisService redisService;


        private ConfigDapper _configDapper;

        public DashboardService(RedisService redisService, ConfigDapper configDapper)
        {
            this.redisService = redisService;
            _configDapper = configDapper;

        }

        public List<HouseDashboard> LoadDashboard()
        {
            var dashboards = redisService.ReadCache<List<HouseDashboard>>(RedisKey.HouseDashboard.Key,
             RedisKey.HouseDashboard.DBName);
            if (dashboards == null)
            {
                dashboards = new List<HouseDashboard>();
                var configs = _configDapper.FindAll();
                foreach (var config in configs)
                {
                    var configJson = JToken.Parse(config.ConfigurationValue);
                    var dash = new HouseDashboard()
                    {
                        Source = config.ConfigurationName,
                        CityName = configJson["cityname"] != null
                        ? configJson["cityname"].ToString()
                        : configJson["cityName"].ToString(),
                        HouseSum = 9999,
                        LastRecordPubTime = DateTime.Now
                    };
                }
                redisService.WriteObject(RedisKey.HouseDashboard.Key, dashboards, RedisKey.HouseDashboard.DBName);
            }
            return dashboards;
        }

        public dynamic LoadCityDashboards()
        {
            var id = 1;
            var dashboards = LoadDashboard()
            .GroupBy(d => d.CityName)
            .Select(i => new
            {
                id = id++,
                cityName = i.Key,
                sources = i.ToList()
            });
            return dashboards;
        }

        public Object LoadCities()
        {
            var id = 0;
            return LoadDashboard().Select(d => d.CityName)
            .Distinct().Select(city => new { id = id++, Name = city }).ToList();
        }

    }
}
