﻿using Dapper;
using HouseMapAPI.Common;
using HouseMapAPI.DBEntity;
using HouseMapAPI.Models;
using HouseMapAPI.Service;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace HouseMapAPI.Dapper
{
    public class HouseDapper : BaseDapper
    {
        public HouseDapper(IOptions<AppSettings> configuration, RedisService redisService)
        : base(configuration, redisService)
        {
        }


        public IEnumerable<HouseInfo> SearchHouses(HouseCondition condition)
        {
            if (string.IsNullOrEmpty(condition.Source))
            {
                var houseList = new List<HouseInfo>();
                // 因为会走几个表,默认每个表取N条
                //var houseSources = GetCityHouseSources(condition.CityName);
                var limitCount = condition.HouseCount / ConstConfigName.HouseTableNameDic.Count;
                foreach (var houseSource in ConstConfigName.HouseTableNameDic)
                {
                    //建荣家园数据质量比较差,默认不出
                    if (houseSource.Key == ConstConfigName.CCBHouse)
                    {
                        continue;
                    }
                    condition.Source = houseSource.Value;
                    condition.HouseCount = limitCount;
                    houseList.AddRange(Search(condition));
                }
                return houseList.OrderByDescending(h => h.PubTime);
            }
            else
            {
                return Search(condition);
            }

        }
        public IEnumerable<HouseInfo> Search(HouseCondition condition)
        {
            string redisKey = condition.RedisKey;
            var houses = new List<HouseInfo>();
            if (!condition.Refresh)
            {
                houses = _redisService.ReadCache<List<HouseInfo>>(redisKey, RedisKey.Houses.DBName);
                if (houses != null && houses.Count > 0)
                {
                    return houses;
                }
            }
            using (IDbConnection dbConnection = GetConnection())
            {
                dbConnection.Open();
                houses = dbConnection.Query<HouseInfo>(condition.QueryText, condition).ToList();
                if (houses != null && houses.Count > 0)
                {
                    _redisService.WriteObject(redisKey, houses, RedisKey.Houses.DBName);
                }
                return houses;
            }
        }



        public HouseInfo GetHouseID(long houseID, string source)
        {
            using (IDbConnection dbConnection = GetConnection())
            {
                dbConnection.Open();

                return dbConnection.Query<HouseInfo>($"SELECT * FROM {ConstConfigName.GetTableName(source)} where ID = @ID",
                  new
                  {
                      ID = houseID
                  }).FirstOrDefault();
            }
        }

    }
}
