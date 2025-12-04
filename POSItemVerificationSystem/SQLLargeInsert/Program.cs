using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Threading.Tasks;

namespace SqlFileExecutor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = "Server=.;Database=Collect1;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=60;Max Pool Size=100;";
            string filePath = @"C:\Users\daniels\Downloads\FromStratech_Transaction_Loyalty.sql";


            if (args.Length >= 1) filePath = args[0];
            if (args.Length >= 2) connectionString = args[1];

            Console.WriteLine($"Processing file: {filePath}");
            Console.WriteLine($"File size: {new FileInfo(filePath).Length / (1024.0 * 1024.0):F2} MB");

            // Settings
            int batchSize = 1000; // Number of statements per batch
            int maxThreads = Environment.ProcessorCount; // Use processor count for parallel threads

            Console.WriteLine($"Using batch size: {batchSize}, parallel threads: {maxThreads}");

            // First, prepare the insert statements
            Console.WriteLine("Reading and preparing INSERT statements...");
            var statements = new List<string>();

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line) &&
                            line.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                        {
                            statements.Add(line);
                        }
                    }
                }

                Console.WriteLine($"Found {statements.Count:N0} INSERT statements");

                if (statements.Count == 0)
                {
                    Console.WriteLine("No INSERT statements found in the file. Please check the file format.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                await ExecuteStatementsParallel(statements, connectionString, batchSize, maxThreads);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static async Task ExecuteStatementsParallel(
            List<string> statements,
            string connectionString,
            int batchSize,
            int maxThreads)
        {
            // Counters for progress tracking
            int totalBatches = (statements.Count + batchSize - 1) / batchSize;
            int completedBatches = 0;
            int successfulStatements = 0;
            int failedStatements = 0;

            // Create batches
            var batches = new List<List<string>>();
            for (int i = 0; i < statements.Count; i += batchSize)
            {
                int count = Math.Min(batchSize, statements.Count - i);
                batches.Add(statements.GetRange(i, count));
            }

            // Start timer
            var stopwatch = Stopwatch.StartNew();

            // Semaphore to limit concurrent executions
            using (var semaphore = new SemaphoreSlim(maxThreads))
            {
                // Create a list to hold all the tasks
                var tasks = new List<Task>();

                // Start a thread for progress reporting
                var progressReporter = Task.Run(async () => {
                    while (completedBatches < totalBatches)
                    {
                        // Calculate progress and rates
                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                        double statementsPerSecond = elapsedSeconds > 0
                            ? (successfulStatements + failedStatements) / elapsedSeconds
                            : 0;

                        double percentComplete = (double)completedBatches / totalBatches * 100;

                        // Estimate remaining time
                        double remainingBatches = totalBatches - completedBatches;
                        double batchesPerSecond = elapsedSeconds > 0
                            ? completedBatches / elapsedSeconds
                            : 0;
                        double remainingSeconds = batchesPerSecond > 0
                            ? remainingBatches / batchesPerSecond
                            : 0;

                        string remainingTime = FormatTimeSpan(TimeSpan.FromSeconds(remainingSeconds));

                        Console.WriteLine($"Progress: {percentComplete:F2}% ({completedBatches}/{totalBatches} batches)");
                        Console.WriteLine($"Statements: {successfulStatements:N0} successful, {failedStatements:N0} failed");
                        Console.WriteLine($"Speed: {statementsPerSecond:F2} statements/sec");
                        Console.WriteLine($"Estimated time remaining: {remainingTime}");
                        Console.WriteLine();

                        await Task.Delay(5000); // Update every 5 seconds
                    }
                });

                // Process each batch
                foreach (var batch in batches)
                {
                    // Wait for a thread to be available
                    await semaphore.WaitAsync();

                    // Start a task to process the batch
                    var task = Task.Run(async () => {
                        try
                        {
                            var result = await ExecuteBatch(batch, connectionString);

                            // Update counters
                            Interlocked.Add(ref successfulStatements, result.successful);
                            Interlocked.Add(ref failedStatements, result.failed);
                            Interlocked.Increment(ref completedBatches);
                        }
                        finally
                        {
                            // Always release the semaphore
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                }

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);

                // Wait for the progress reporter to notice we're done
                await Task.Delay(100);
            }

            stopwatch.Stop();

            // Final report
            Console.WriteLine("\nExecution Complete!");
            Console.WriteLine($"Total statements: {statements.Count:N0}");
            Console.WriteLine($"Successful: {successfulStatements:N0}");
            Console.WriteLine($"Failed: {failedStatements:N0}");
            Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalMinutes:F2} minutes");

            double statementsPerSecond = stopwatch.Elapsed.TotalSeconds > 0
                ? (successfulStatements + failedStatements) / stopwatch.Elapsed.TotalSeconds
                : 0;

            Console.WriteLine($"Average rate: {statementsPerSecond:F2} statements/second");
        }

        static async Task<(int successful, int failed)> ExecuteBatch(List<string> statements, string connectionString)
        {
            int successful = 0;
            int failed = 0;

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Use a transaction for the batch
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandTimeout = 120; // 2 minutes per command

                        foreach (var sql in statements)
                        {
                            try
                            {
                                command.CommandText = sql;
                                await command.ExecuteNonQueryAsync();
                                successful++;
                            }
                            catch (Exception ex)
                            {
                                failed++;

                                // Log error without blocking
                                Task.Run(() => {
                                    try
                                    {
                                        File.AppendAllText("errors.log",
                                            $"Error: {ex.Message}\n{sql}\n\n");
                                    }
                                    catch { } // Ignore logging errors
                                });
                            }
                        }
                    }

                    try
                    {
                        // Commit if any statements succeeded
                        if (successful > 0)
                        {
                            transaction.Commit();
                        }
                        else
                        {
                            transaction.Rollback();
                        }
                    }
                    catch (Exception)
                    {
                        // If commit fails, count all as failed
                        failed += successful;
                        successful = 0;

                        try { transaction.Rollback(); } catch { }
                    }
                }
            }

            return (successful, failed);
        }

        static string FormatTimeSpan(TimeSpan span)
        {
            if (span.TotalDays > 1)
                return $"{span.TotalDays:F1} days";
            else if (span.TotalHours > 1)
                return $"{span.TotalHours:F1} hours";
            else if (span.TotalMinutes > 1)
                return $"{span.TotalMinutes:F1} minutes";
            else
                return $"{span.TotalSeconds:F0} seconds";
        }
    }
}
