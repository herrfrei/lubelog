﻿using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using Npgsql;
using System.Text.Json;

namespace CarCareTracker.External.Implementations
{
    public class PGCollisionRecordDataAccess : ICollisionRecordDataAccess
    {
        private NpgsqlDataSource pgDataSource;
        private readonly ILogger<PGCollisionRecordDataAccess> _logger;
        private static string tableName = "collisionrecords";
        public PGCollisionRecordDataAccess(IConfiguration config, ILogger<PGCollisionRecordDataAccess> logger)
        {
            pgDataSource = NpgsqlDataSource.Create(config["POSTGRES_CONNECTION"]);

            _logger = logger;
            try
            {
                //create table if not exist.
                string initCMD = $"CREATE TABLE IF NOT EXISTS app.{tableName} (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)";
                using (var ctext = pgDataSource.CreateCommand(initCMD))
                {
                    ctext.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        public List<CollisionRecord> GetCollisionRecordsByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE vehicleId = @vehicleId";
                var results = new List<CollisionRecord>();
                using (var ctext = pgDataSource.CreateCommand(cmd))
                {
                    ctext.Parameters.AddWithValue("vehicleId", vehicleId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            CollisionRecord collisionRecord = JsonSerializer.Deserialize<CollisionRecord>(reader["data"] as string);
                            results.Add(collisionRecord);
                        }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<CollisionRecord>();
            }
        }
        public CollisionRecord GetCollisionRecordById(int collisionRecordId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE id = @id";
                var result = new CollisionRecord();
                using (var ctext = pgDataSource.CreateCommand(cmd))
                {
                    ctext.Parameters.AddWithValue("id", collisionRecordId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            CollisionRecord collisionRecord = JsonSerializer.Deserialize<CollisionRecord>(reader["data"] as string);
                            result = collisionRecord;
                        }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new CollisionRecord();
            }
        }
        public bool DeleteCollisionRecordById(int collisionRecordId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE id = @id";
                using (var ctext = pgDataSource.CreateCommand(cmd))
                {
                    ctext.Parameters.AddWithValue("id", collisionRecordId);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool SaveCollisionRecordToVehicle(CollisionRecord collisionRecord)
        {
            try
            {
                if (collisionRecord.Id == default)
                {
                    string cmd = $"INSERT INTO app.{tableName} (vehicleId, data) VALUES(@vehicleId, CAST(@data AS jsonb)) RETURNING id";
                    using (var ctext = pgDataSource.CreateCommand(cmd))
                    {
                        ctext.Parameters.AddWithValue("vehicleId", collisionRecord.VehicleId);
                        ctext.Parameters.AddWithValue("data", "{}");
                        collisionRecord.Id = Convert.ToInt32(ctext.ExecuteScalar());
                        //update json data
                        if (collisionRecord.Id != default)
                        {
                            string cmdU = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                            using (var ctextU = pgDataSource.CreateCommand(cmdU))
                            {
                                var serializedData = JsonSerializer.Serialize(collisionRecord);
                                ctextU.Parameters.AddWithValue("id", collisionRecord.Id);
                                ctextU.Parameters.AddWithValue("data", serializedData);
                                return ctextU.ExecuteNonQuery() > 0;
                            }
                        }
                        return collisionRecord.Id != default;
                    }
                }
                else
                {
                    string cmd = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                    using (var ctext = pgDataSource.CreateCommand(cmd))
                    {
                        var serializedData = JsonSerializer.Serialize(collisionRecord);
                        ctext.Parameters.AddWithValue("id", collisionRecord.Id);
                        ctext.Parameters.AddWithValue("data", serializedData);
                        return ctext.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool DeleteAllCollisionRecordsByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE vehicleId = @id";
                using (var ctext = pgDataSource.CreateCommand(cmd))
                {
                    ctext.Parameters.AddWithValue("id", vehicleId);
                    ctext.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}
