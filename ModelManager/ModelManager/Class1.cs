using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModelManagerSD
{ 

    public class SafetensorsMetadataInjector
    {

        int imgCount;
        /// <summary>
        /// Scans a folder for .txt files and counts tag occurrences.
        /// </summary>
        public Dictionary<string, int> ParseCaptions(string folderPath)
        {
            var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"[ERROR] Folder not found: {folderPath}");
                return frequencies;
            }

            // Get all .txt files
            string[] files = Directory.GetFiles(folderPath, "*.png");
            imgCount = files.Length; 

            foreach (string file in files)
            {
                try
                {
                    var txtPath = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + ".txt";
                    string content = File.ReadAllText(txtPath);

                    // Split by comma, remove empty entries, and trim whitespace
                    string[] tags = content.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string tag in tags)
                    {
                        string cleanTag = tag.Trim().ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(cleanTag)) continue;

                        if (frequencies.ContainsKey(cleanTag))
                            frequencies[cleanTag]++;
                        else
                            frequencies[cleanTag] = 1;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Could not read {file}: {ex.Message}");
                }
            }

            return frequencies;
        }

        /// <summary>
        /// Injects Kohya-style metadata into a .safetensors file.
        /// </summary>
        public void InjectMetadata(string inputPath, string outputPath, Dictionary<string, int> tagFrequencies)
        {
            if (!File.Exists(inputPath)) throw new FileNotFoundException(inputPath);

            using var fsIn = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fsIn);

            // 1. Read header length
            ulong headerLength = reader.ReadUInt64();

            // 2. Read the header string
            byte[] headerBytes = reader.ReadBytes((int)headerLength);
            string headerJson = Encoding.UTF8.GetString(headerBytes);

            // 3. Parse JSON Header
            var header = JsonNode.Parse(headerJson).AsObject();

            // 4. Handle __metadata__
            if (!header.ContainsKey("__metadata__"))
                header["__metadata__"] = new JsonObject();

            var metadata = header["__metadata__"].AsObject();

            // 5. Calculate stats
            // We assume the number of images is based on the number of unique txt files parsed 
            // Or total tag frequency logic. Usually, in Kohya, img_count is the number of images.
            // For this purpose, we'll use a count provided or total tags. 
            // To be precise, let's just use the sum of a specific tag or a passed count.
            int totalImgCount = imgCount;//tagFrequencies.Values.Max(); // Approximate or specific        

            var tagFreq = new Dictionary<string, object>
        {
            { $"1_", tagFrequencies }
        };

            // 6. Inject (must be serialized as strings)
            metadata["ss_tag_frequency"] = JsonSerializer.Serialize(tagFreq);
            //metadata["ss_dataset_dirs"] = JsonSerializer.Serialize(datasetDirs);
            //metadata["ss_resolution"] = "1024,1024";
            metadata["ss_num_train_images"] = totalImgCount.ToString();
            //metadata["ss_session_id"] = Guid.NewGuid().ToString(); // Optional helper

            // 7. Write Output
            using var fsOut = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fsOut);

            string newHeaderJson = header.ToJsonString();
            byte[] newHeaderBytes = Encoding.UTF8.GetBytes(newHeaderJson);

            writer.Write((ulong)newHeaderBytes.Length);
            writer.Write(newHeaderBytes);

            // Stream tensors
            fsIn.Position = (long)(8 + headerLength);
            fsIn.CopyTo(fsOut);

            Console.WriteLine($"[OK] Successfully wrote {tagFrequencies.Count} tags to {outputPath}");
        }
    }
}
