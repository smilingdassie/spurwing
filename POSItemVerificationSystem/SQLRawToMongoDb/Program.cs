using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;

namespace SqlToMongoImporter
{
    class Program
    {
        // Configuration
        static string SqlConnectionString = "Server=.;Database=Collect1;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=60;Max Pool Size=100;";

        static string MongoConnectionString = "mongodb://localhost:27017";
        static string MongoDatabaseName = "LoyaltyData";
        static int BatchSize = 5000; // Smaller batch size to avoid timeouts
        static int CommandTimeout = 300; // Increased command timeout to 5 minutes

        static async Task Main(string[] args)
        {
            try
            {
                // Override configuration if provided
                if (args.Length >= 1) SqlConnectionString = args[0];
                if (args.Length >= 2) MongoConnectionString = args[1];

                Console.WriteLine("SQL to MongoDB Data Transfer");
                Console.WriteLine("----------------------------");

                // Connect to MongoDB
                var mongoClient = new MongoClient(MongoConnectionString);
                var database = mongoClient.GetDatabase(MongoDatabaseName);

                // Get distinct entity types
                var entityTypes = await GetDistinctEntityTypes();
                Console.WriteLine($"Found {entityTypes.Count} distinct entity types");

                // Process each entity type
                foreach (var entityType in entityTypes)
                {
                    Console.WriteLine($"Processing entity type: {entityType}");
                    await ProcessEntityTypeWithRetry(entityType, database);
                }

                Console.WriteLine("Data transfer complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static async Task<List<string>> GetDistinctEntityTypes()
        {
            var entityTypes = new List<string>();
            int retryCount = 0;
            int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(SqlConnectionString))
                    {
                        await connection.OpenAsync();

                        string query = "select SourceEntityName from Sample_FromStratech_Transaction_Loyalty;";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.CommandTimeout = CommandTimeout;

                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string entityType = reader.GetString(0);
                                    entityTypes.Add(entityType);
                                }
                            }
                        }
                    }

                    // If we get here, we succeeded
                    break;
                }
                catch (SqlException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        Console.WriteLine($"Failed to get entity types after {maxRetries} attempts: {ex.Message}");
                        throw;
                    }

                    Console.WriteLine($"SQL error getting entity types (attempt {retryCount}): {ex.Message}");
                    Console.WriteLine($"Retrying in 5 seconds...");
                    await Task.Delay(5000); // Wait 5 seconds before retrying
                }
            }

            return entityTypes;
        }

        static async Task ProcessEntityTypeWithRetry(string entityType, IMongoDatabase database)
        {
            int retryCount = 0;
            int maxRetries = 3;
            int lastProcessedOffset = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    await ProcessEntityType(entityType, database, lastProcessedOffset);
                    // If we get here, processing completed successfully
                    break;
                }
                catch (SqlException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        Console.WriteLine($"Failed to process entity type {entityType} after {maxRetries} attempts: {ex.Message}");
                        throw;
                    }

                    Console.WriteLine($"SQL error processing {entityType} (attempt {retryCount}): {ex.Message}");
                    Console.WriteLine($"Retrying in 10 seconds from last successful offset: {lastProcessedOffset}");
                    await Task.Delay(10000); // Wait 10 seconds before retrying
                }
            }
        }

        static async Task ProcessEntityType(string entityType, IMongoDatabase database, int startOffset = 0)
        {
            // Create or get collection for this entity type
            var collection = database.GetCollection<BsonDocument>(entityType);

            // Track statistics
            int totalRecords = 0;
            int processedRecords = 0;
            int errorCount = 0;
            DateTime startTime = DateTime.Now;
            int currentOffset = startOffset;

            // Get count of records for this entity type
            using (SqlConnection connection = new SqlConnection(SqlConnectionString))
            {
                await connection.OpenAsync();

                string countQuery = "SELECT COUNT(*) FROM [RawData].[FromStratech_Transaction_Loyalty] WITH(NOLOCK) WHERE SourceEntityName = @EntityType";

                using (SqlCommand command = new SqlCommand(countQuery, connection))
                {
                    command.CommandTimeout = CommandTimeout;
                    command.Parameters.AddWithValue("@EntityType", entityType);
                    totalRecords = (int)await command.ExecuteScalarAsync();
                }

                Console.WriteLine($"Found {totalRecords} records for {entityType}");

                // Fetch and process records in batches
                int batchSize = BatchSize;

                while (currentOffset < totalRecords)
                {
                    string query = @"
                        SELECT 
                            FromID, 
                            RAWID, 
                            RAWDATE, 
                            JSON_RawData,
                            EffectiveFrom
                        FROM [RawData].[FromStratech_Transaction_Loyalty] 
                        WHERE SourceEntityName = @EntityType
                        ORDER BY FromID
                        OFFSET @Offset ROWS
                        FETCH NEXT @BatchSize ROWS ONLY";

                    List<BsonDocument> batch = new List<BsonDocument>();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = CommandTimeout;
                        command.Parameters.AddWithValue("@EntityType", entityType);
                        command.Parameters.AddWithValue("@Offset", currentOffset);
                        command.Parameters.AddWithValue("@BatchSize", batchSize);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    // Get data
                                    long fromId = reader.GetInt64(0);
                                    string rawId = reader.GetString(1);
                                    DateTime rawDate = reader.GetDateTime(2);

                                    // Get JSON data
                                    string jsonData = null;
                                    if (!reader.IsDBNull(3))
                                    {
                                        jsonData = reader.GetString(3);
                                    }

                                    DateTimeOffset effectiveFrom = reader.GetDateTimeOffset(4);

                                    if (!string.IsNullOrEmpty(jsonData))
                                    {
                                        // Parse the JSON
                                        JObject jsonObj = JObject.Parse(jsonData);

                                        // Add metadata
                                        jsonObj["_metadata"] = new JObject
                                        {
                                            ["FromID"] = fromId,
                                            ["RAWID"] = rawId,
                                            ["RAWDATE"] = rawDate,
                                            ["EffectiveFrom"] = effectiveFrom,
                                            ["ImportedAt"] = DateTime.Now
                                        };

                                        // Convert to BsonDocument and add to batch
                                        BsonDocument doc = BsonDocument.Parse(jsonObj.ToString());
                                        batch.Add(doc);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errorCount++;
                                    Console.WriteLine($"Error processing record: {ex.Message}");
                                }
                            }
                        }
                    }

                    // Insert batch into MongoDB with retry
                    if (batch.Count > 0)
                    {
                        await InsertBatchWithRetry(collection, batch);
                    }

                    // Update progress
                    processedRecords += batch.Count;
                    currentOffset += batchSize;

                    // Report progress
                    double percentComplete = (double)processedRecords / totalRecords * 100;
                    TimeSpan elapsed = DateTime.Now - startTime;
                    double recordsPerSecond = processedRecords / elapsed.TotalSeconds;

                    Console.WriteLine($"  Progress: {percentComplete:F2}% ({processedRecords}/{totalRecords}) - {recordsPerSecond:F2} records/sec");
                }
            }

            TimeSpan totalTime = DateTime.Now - startTime;
            Console.WriteLine($"Completed {entityType}: {processedRecords} records in {totalTime.TotalSeconds:F2} seconds");
            Console.WriteLine($"Errors: {errorCount}");

            // Create indexes for better performance
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("_metadata.RAWID")));

            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("_metadata.EffectiveFrom")));

            Console.WriteLine("Created indexes for faster queries");
        }

        static async Task InsertBatchWithRetry(IMongoCollection<BsonDocument> collection, List<BsonDocument> batch)
        {
            int retryCount = 0;
            int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    await collection.InsertManyAsync(batch, new InsertManyOptions { IsOrdered = false });
                    break; // Success, exit the retry loop
                }
                catch (MongoWriteException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        Console.WriteLine($"Failed to insert batch after {maxRetries} attempts: {ex.Message}");
                        throw;
                    }

                    Console.WriteLine($"MongoDB write error (attempt {retryCount}): {ex.Message}");
                    Console.WriteLine($"Retrying in 3 seconds...");
                    await Task.Delay(3000); // Wait 3 seconds before retrying
                }
            }
        }
    }
}