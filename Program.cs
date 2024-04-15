using System.Xml;
using LibIRD;
using SabreTools.Hashing;

namespace ird2nkit
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Get IRD file path from arg
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide an IRD file path as a command line argument");
                return;
            }
            string irdPath = args[0];

            // Parse IRD
            IRD ird = IRD.Read(irdPath);

            // Calculate hash of Disc Key
            byte[] key = ird.DiscKey;
            string? keyCRC = HashTool.GetByteArrayHash(key, HashType.CRC32);

            // Write to NKit scan
            using XmlWriter writer = XmlWriter.Create(Path.ChangeExtension(irdPath, ".nkit"), new XmlWriterSettings { Indent = true });
            writer.WriteStartDocument();

            // Write the root element
            writer.WriteStartElement("NKitScan");
            writer.WriteAttributeString("Version", "1.0");
            writer.WriteAttributeString("System", "PS3");
            writer.WriteAttributeString("Media", "Disc");
            writer.WriteAttributeString("Size", "000000000");

            // Redump-style IRDs contains Enc ISO CRC in UID field
            if (ird.ExtraConfig == 0x0001)
                writer.WriteAttributeString("CRC", $"{ird.UID:X8}");

            // AreaInfo/Area pair for each region
            for (int i = 0; i < ird.RegionCount; i++)
            {
                // AreaInfo
                writer.WriteStartElement("AreaInfo");
                writer.WriteAttributeString("Type", "PS3");
                writer.WriteAttributeString("Session", "0");
                writer.WriteAttributeString("Track", "0");
                writer.WriteAttributeString("Region", $"{i}");
                writer.WriteAttributeString("BlockSize", "2048");
                writer.WriteAttributeString("Mode1", "0");
                writer.WriteAttributeString("Mode2Form1", "0");
                writer.WriteAttributeString("Mode2Form2", "0");
                writer.WriteAttributeString("AreaOffsetBase", "000000000");
                writer.WriteAttributeString("SessionOffsetBase", "000000000");
                if (i == 0)
                {
                    if (string.IsNullOrEmpty(keyCRC))
                    {
                        writer.WriteAttributeString("TitleKeyMissing", "true");
                        writer.WriteAttributeString("DecryptionValid", "false");
                    }
                    else
                    {
                        writer.WriteAttributeString("TitleKeyCrc", keyCRC.ToUpper());
                        writer.WriteAttributeString("DecryptionValid", "true");
                    }
                }
                writer.WriteEndElement();

                // Area
                writer.WriteStartElement("Area");
                writer.WriteAttributeString("ImageOffset", "000000000");
                writer.WriteAttributeString("Type", "FileSystem");
                writer.WriteAttributeString("Size", "000000000");
                writer.WriteAttributeString("MD5", $"{IRD.ByteArrayToHexString(ird.RegionHashes[i])}");

                int sectionCount = 1;
                for (int j = 0; j < sectionCount; j++)
                {
                    // Section
                    writer.WriteStartElement("Section");
                    writer.WriteAttributeString("ImageOffset", "000000000");
                    writer.WriteAttributeString("AreaOffset", "000000000");
                    writer.WriteAttributeString("Size", "00200000");

                    // File
                    writer.WriteStartElement("File");
                    writer.WriteAttributeString("ImageOffset", "000000000");
                    writer.WriteAttributeString("AreaOffset", "000000000");
                    writer.WriteAttributeString("SectionOffset", "000000000");
                    writer.WriteAttributeString("Position", "000000000");
                    writer.WriteAttributeString("Size", "000000000");
                    writer.WriteAttributeString("MD5", $"{IRD.ByteArrayToHexString(ird.FileHashes[0])}"); // Can set this only if file is wholly within section
                    writer.WriteAttributeString("FullSize", "000000000");
                    writer.WriteAttributeString("FullMD5", $"{IRD.ByteArrayToHexString(ird.FileHashes[0])}");
                    writer.WriteAttributeString("DataType", "Data"); // Set to "Nulls" if MD5 matches all nulls
                    writer.WriteAttributeString("File", "/FILENAME.EXT");
                    writer.WriteAttributeString("FS", "Udf, Joliet, Iso9660");
                    writer.WriteEndElement();

                    int filesInSection = 2;
                    for (int k = 1; k < filesInSection; k++)
                    {
                        // Gap
                        // if(fileSize % 2048 != 0) {
                        writer.WriteStartElement("Gap");
                        writer.WriteAttributeString("ImageOffset", "000000000");
                        writer.WriteAttributeString("AreaOffset", "000000000");
                        writer.WriteAttributeString("SectionOffset", "000000000");
                        writer.WriteAttributeString("Position", "000000000");
                        writer.WriteAttributeString("Size", "000000000");
                        //writer.WriteAttributeString("CRC", $"{}"); // Can calculate this manually if desired
                        //writer.WriteAttributeString("MD5", $"{}"); // Can calculate this manually if desired
                        writer.WriteAttributeString("FullSize", "000000000");
                        //writer.WriteAttributeString("FullCRC", $"{}"); // Can calculate this manually if desired
                        //writer.WriteAttributeString("FullMD5", $"{}"); // Can calculate this manually if desired
                        writer.WriteAttributeString("DataType", "Nulls");
                        writer.WriteAttributeString("Gap", "/FILENAME.EXT");
                        writer.WriteEndElement();

                        // Next File in Section
                        writer.WriteStartElement("File");
                        writer.WriteAttributeString("ImageOffset", "000000000");
                        writer.WriteAttributeString("AreaOffset", "000000000");
                        writer.WriteAttributeString("SectionOffset", "000000000");
                        writer.WriteAttributeString("Position", "000000000");
                        writer.WriteAttributeString("Size", "000000000");
                        //writer.WriteAttributeString("MD5", $"{IRD.ByteArrayToHexString(ird.FileHashes[0])}"); // Can set this only if file is wholly within section
                        writer.WriteAttributeString("FullSize", "000000000");
                        writer.WriteAttributeString("FullMD5", $"{IRD.ByteArrayToHexString(ird.FileHashes[0])}");
                        writer.WriteAttributeString("DataType", "Data");
                        writer.WriteAttributeString("File", "/FILENAME.EXT");
                        writer.WriteAttributeString("FS", "Udf, Joliet, Iso9660");
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            // End the root element
            writer.WriteEndElement();

            // End the document
            writer.WriteEndDocument();
        }
    }
}
