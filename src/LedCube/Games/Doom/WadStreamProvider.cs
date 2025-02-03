using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DoomSharp.Core;
using DoomSharp.Core.Abstractions;
using DoomSharp.Core.Data;

namespace LedCube.Games.Doom
{
    public sealed class WadStreamProvider : IWadStreamProvider
    {
        public async Task<WadFile?> LoadFromFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                DoomGame.Console.WriteLine("Error: File path is null or empty.");
                return null;
            }

            if (!File.Exists(file))
            {
                DoomGame.Console.WriteLine($"Error: File does not exist: {file}");
                return null;
            }

            try
            {
                // Use asynchronous file access for better performance on constrained systems
                await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                using var br = new BinaryReader(fs, Encoding.ASCII, false);

                DoomGame.Console.WriteLine($"Adding WAD file: {file}");

                if (string.Equals(Path.GetExtension(file), ".wad", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse the WAD file
                    return await Task.FromResult(ParseWadFile(file, br));
                }
                else
                {
                    DoomGame.Console.WriteLine($"Unsupported file type: {Path.GetExtension(file)}");
                }
            }
            catch (Exception ex)
            {
                DoomGame.Console.WriteLine($"Error loading WAD file {file}: {ex.Message}");
            }

            return null;
        }

        private static WadFile? ParseWadFile(string file, BinaryReader reader)
        {
            try
            {
                // Read the WAD file header
                var header = new WadFile.WadInfo
                {
                    Identification = Encoding.ASCII.GetString(reader.ReadBytes(4)).TrimEnd('\0'),
                    NumLumps = reader.ReadInt32(),
                    InfoTableOfs = reader.ReadInt32()
                };

                if (!string.Equals(header.Identification, "IWAD", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(header.Identification, "PWAD", StringComparison.OrdinalIgnoreCase))
                {
                    DoomGame.Console.WriteLine($"Invalid WAD file type for {file}. Expected IWAD or PWAD.");
                    return null;
                }

                var wadFile = new WadFile(reader)
                {
                    Header = header
                };

                DoomGame.Console.WriteLine($"WAD file type: {header.Identification}, Lumps: {header.NumLumps}");

                // Read lump metadata
                var lumps = new List<WadLump>(wadFile.LumpCount);

                reader.BaseStream.Seek(header.InfoTableOfs, SeekOrigin.Begin);

                for (int i = 0; i < header.NumLumps; i++)
                {
                    var lump = WadFile.FileLump.ReadFromWadData(reader);
                    lumps.Add(new WadLump(wadFile, lump));
                }

                wadFile.Lumps = lumps;
                return wadFile;
            }
            catch (Exception ex)
            {
                DoomGame.Console.WriteLine($"Error parsing WAD file {file}: {ex.Message}");
                return null;
            }
        }
    }
}