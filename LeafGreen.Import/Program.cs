﻿using LeafGreen.Entities;
using LeafGreen.SqlProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace LeafGreen.Import
{
    public class Program
    {
        private static PlantSqlProvider _sql;
        public static void Main(string[] args)
        {
            _sql = new PlantSqlProvider("Server=localhost;Database=LeafGreen;Trusted_Connection=True;");
            Console.WriteLine("Welcome to the plant processor!");
            Console.WriteLine("------------------------------------------------------------------");
            if (File.Exists("failed-inserts.json"))
            {
                Console.WriteLine("Retrying failed plants...");
                ProcessFailedItems();
            }
            else
            {
                Console.WriteLine("Inserting plants....");
                var plants = Task.Run(async () => { return await ProcessAllPlants(); }).Result;
                if(plants.Count == 0)
                {
                    Console.WriteLine($"All plants inserted successfully!");
                    Console.WriteLine("Finished inserting plants! Press enter to exit import application.");
                    Console.ReadLine();
                }
                else
                {
                    //write our failed json frol
                    Console.WriteLine($"{plants.Count} plants failed to insert...");
                    Console.WriteLine("Writing out json file of plants....");
                    System.IO.File.WriteAllText("failed-inserts.json", JsonConvert.SerializeObject(plants));
                    Console.WriteLine("File written... Press enter to exit....");
                    Console.ReadLine();
                }
            }
        }

        private static List<Plant> ProcessFailedItems()
        {
            return null;
        }

        private async static Task<List<Plant>> ProcessAllPlants()
        {

            var fileContents = System.IO.File.ReadAllLines("allplants.txt");
            var plants = new List<Plant>();
            foreach (var line in fileContents)
            {
                if (line != "\"Symbol\",\"Synonym Symbol\",\"Scientific Name with Author\",\"Common Name\",\"Family\"")
                {
                    var item = line.Replace("\"", "");
                    var splitRecord = item.Split(',');
                    var symbol = splitRecord[0];
                    var sciNameAndAuthor = splitRecord[2];
                    var regexedNameAndAuthor = Regex.Split(sciNameAndAuthor, "^([A-z]*\\s[A-z]*){1}");
                    regexedNameAndAuthor = regexedNameAndAuthor.ToList().Where(x => x != string.Empty).ToArray();
                    string author = string.Empty;
                    string scientificName = string.Empty;
                    if (regexedNameAndAuthor.Count() > 0)
                    {
                        scientificName = regexedNameAndAuthor[0].Trim();
                    }
                    if (regexedNameAndAuthor.Count() > 1)
                    {
                        author = regexedNameAndAuthor[1].Trim();
                    }
                    var commonName = splitRecord[3];
                    var family = splitRecord[4];


                    plants.Add(new Plant()
                    {
                        Symbol = symbol,
                        Author = author,
                        ScientificName = scientificName,
                        CommonName = commonName,
                        Family = family
                    });
                }
            }

            var uninsertedPlants = await _sql.InsertPlantListAsync(plants);

            return uninsertedPlants;
        }
    }
}