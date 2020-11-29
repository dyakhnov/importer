using System;
using System.IO;
using Newtonsoft.Json;
using Npgsql;
using Dapper;

namespace importer
{
    public class Program
    {
        public static void DisplaySummary(NpgsqlConnection connection, int transmissionId)
        {
            Console.WriteLine("- summary:");
            var rows = connection.Query(
                "SELECT location, SPLIT_PART(category, ' > ', 3) AS l3category, SUM(qty) AS total " +
                "FROM data " +
                "WHERE transmission_id = @transmissionId " +
                "GROUP BY location, l3category " +
                "ORDER BY location, l3category",
                new { transmissionId });
            foreach (var row in rows)
            {
                Console.WriteLine("  - {0} - {1} - {2}", row.location, row.l3category, row.total);
            }
        }
        public static int ProcessData(NpgsqlConnection connection, Transmission data)
        {
            int transmissionId = 0;
            using (var transaction = connection.BeginTransaction())
            {
                transmissionId = connection.ExecuteScalar<int>(
                    "INSERT INTO transmissions (transmission_id, dt_imported)" +
                    " VALUES (@transmissionId, @dtNow);" +
                    "SELECT currval('transmissions_id_seq');",
                    new {
                        transmissionId = data.transmissionsummary.id,
                        dtNow = DateTime.Now 
                    });
                foreach (Product product in data.products)
                {
                    connection.Execute(
                        "INSERT INTO data (sku, description, category, price, location, qty, transmission_id)" +
                        " VALUES (@sku, @description, @category, @price, @location, @qty, @transmissionId)",
                        new
                        {
                            sku = product.sku,
                            description = product.description,
                            category = product.category,
                            price = product.price,
                            location = product.location,
                            qty = product.qty,
                            transmissionId
                        });
                }
                transaction.Commit();
            }
            return transmissionId;
        }
        public static bool ProcessFile(NpgsqlConnection connection, string fileName)
        {
            Transmission data = new Transmission();
            // Trying to read file and deserialize JSON
            try
            {
                data = JsonConvert.DeserializeObject<Transmission>(File.ReadAllText(fileName));
            }
            catch
            {
                Console.WriteLine("- can't read JSON data");
                return false;
            }
            // Check for data consistency
            if (data.products.Length != data.transmissionsummary.recordcount)
            {
                Console.WriteLine("- records count {0} does not match summary {1}", data.products.Length, data.transmissionsummary.recordcount);
                return false;
            }
            int qty = 0;
            foreach (Product product in data.products)
            {
                qty += product.qty;
            }
            if (qty != data.transmissionsummary.qtysum)
            {
                Console.WriteLine("- quantity sum {0} does not match summary {1}", qty, data.transmissionsummary.qtysum);
                return false;
            }
            // Check for transmission ID
            if (data.transmissionsummary.id == null)
            {
                Console.WriteLine("- transmissionId is missing");
                return false;
            } else if (data.transmissionsummary.id.Length != 36) {
                Console.WriteLine("- invalid transmissionId, must be 36 chars"); // UUID
                return false;
            }
            Console.WriteLine("- transmissionId: {0}", data.transmissionsummary.id);
            int transmissionId = connection.ExecuteScalar<int>(
                "SELECT id" +
                " FROM transmissions" +
                " WHERE transmission_id = @transmissionId",
                new { transmissionId = data.transmissionsummary.id });
            if (transmissionId != 0)
            {
                Console.WriteLine("- transmissionId already processed");
                return false;
            }
            transmissionId = ProcessData(connection, data);
            if (transmissionId == 0)
            {
                return false;
            }
            DisplaySummary(connection, transmissionId);
            return true;
        }
        public static int[] ProcessDirectory(NpgsqlConnection connection, string sourceDirectory, string destinationDirectory)
        {
            Console.WriteLine("Processing directory {0}", sourceDirectory);
            string[] filesList = Directory.GetFiles(sourceDirectory, "*.json");
            int totalFiles = 0;
            int processedFiles = 0;
            foreach (string sourceFilename in filesList)
            {
                totalFiles++;
                string fileName = Path.GetFileName(sourceFilename);
                Console.WriteLine();
                Console.WriteLine("Processing {0}...", fileName);
                try
                {
                    if (ProcessFile(connection, sourceFilename))
                    {
                        processedFiles++;
                    } else
                    {
                        Console.WriteLine("- skipping");
                    }
                } catch(Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    string destinationFilename = Path.Combine(destinationDirectory, fileName);
                    if(File.Exists(destinationFilename))
                    {
                        File.Delete(destinationFilename);
                    }
                    File.Move(sourceFilename, destinationFilename);
                }
            }
            return new[] { processedFiles, totalFiles };
        }
        public static void Main()
        {
            string sourceDirectory = Environment.GetEnvironmentVariable("SRC");
            if(sourceDirectory == null)
            {
                Console.WriteLine("Can't find source directory.");
                System.Environment.Exit(1);
            }
            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine("Invalid source directory, {0}.", sourceDirectory);
                System.Environment.Exit(1);
            }
            string destinationDirectory = Environment.GetEnvironmentVariable("DST");
            if (destinationDirectory == null)
            {
                Console.WriteLine("Can't find destination directory.");
                System.Environment.Exit(1);
            }
            if (!Directory.Exists(destinationDirectory))
            {
                Console.WriteLine("Invalid destination directory, {0}.", destinationDirectory);
                System.Environment.Exit(1);
            }
            NpgsqlConnection connection = new NpgsqlConnection(String.Format(
                "Server={0};User Id={1};Password={2};Database={3};",
                Environment.GetEnvironmentVariable("DB_HOST"),
                Environment.GetEnvironmentVariable("DB_USER"),
                Environment.GetEnvironmentVariable("DB_PASS"),
                Environment.GetEnvironmentVariable("DB_NAME")));
            try
            {
                connection.Open();
            }
            catch(Exception e)
            {
                Console.WriteLine("Can't connect to the database.");
                Console.WriteLine(e);
                System.Environment.Exit(1);
            }
            int[] results = ProcessDirectory(connection, sourceDirectory, destinationDirectory);
            connection.Close();
            Console.WriteLine();
            Console.WriteLine("Processed {0} new files out of {1} total.", results[0], results[1]);
        }
    }
}
