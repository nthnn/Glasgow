/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

using System.Security.Cryptography;
using System.Text;

namespace Glasgow
{
    /// <summary>
    /// Handles secure serialization and deserialization of a <see cref="GlasgowDB"/> instance to and from disk.
    /// 
    /// This class provides AES-256-CBC encryption for secure database persistence, using a passphrase-derived key
    /// via PBKDF2 and a randomly generated Initialization Vector (IV) embedded at the start of the file.
    /// 
    /// All data (including table structure and row content) is stored in a binary format after encryption.
    /// 
    /// <para>
    /// <b>Security Considerations:</b>
    /// <list type="bullet">
    ///   <item><description>Encryption uses AES-256-CBC for strong symmetric encryption.</description></item>
    ///   <item><description>Keys are derived from passphrases using PBKDF2 to resist brute force attacks.</description></item>
    ///   <item><description>The IV is stored unencrypted at the beginning of the file and is used for key derivation and decryption.</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <b>Typical Usage:</b><br/>
    /// - Use <see cref="SaveToFile"/> to persist a <see cref="GlasgowDB"/> securely.<br/>
    /// - Use <see cref="LoadFromFile"/> to recover it using the same passphrase.
    /// </para>
    /// </summary>
    public class DatabaseIO
    {
        /// <summary>
        /// Loads and decrypts a <see cref="GlasgowDB"/> instance from the specified encrypted file.
        /// 
        /// If the file does not exist, an empty instance is returned (i.e., a new <see cref="GlasgowDB"/> with no tables).
        /// 
        /// <para>
        /// Decryption uses the AES-256-CBC algorithm with a passphrase-derived key using PBKDF2. 
        /// The IV required for decryption is read from the beginning of the file.
        /// </para>
        /// </summary>
        /// <param name="FileName">Path to the encrypted file containing the serialized database.</param>
        /// <param name="PassKey">Passphrase to derive the decryption key. Must match the passphrase used to save the file.</param>
        /// <returns>
        /// A populated <see cref="GlasgowDB"/> instance if decryption and deserialization succeed,
        /// or an empty instance if the file is missing.
        /// </returns>
        /// <exception cref="EndOfStreamException">
        /// Thrown if the file is too short to contain a valid IV or unexpectedly ends during reading.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the passphrase is incorrect or the file is corrupted, leading to decryption failure
        /// or unexpected binary format during deserialization.
        /// </exception>
        public static GlasgowDB LoadFromFile(string FileName, string PassKey)
        {
            var db = new GlasgowDB();
            if (File.Exists(FileName))
            {
                using var fileStream = new FileStream(
                    FileName,
                    FileMode.Open,
                    FileAccess.Read
                );

                byte[] iv = new byte[16];
                int bytesRead = fileStream.Read(iv, 0, iv.Length);

                if (bytesRead < iv.Length)
                    throw new EndOfStreamException(
                        "File is corrupted or too short to read IV."
                    );

                using var aes = Aes.Create();
                aes.Key = CryptographyUtil.DeriveKey(PassKey, iv);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                try
                {
                    using var cryptoStream = new CryptoStream(
                        fileStream,
                        aes.CreateDecryptor(),
                        CryptoStreamMode.Read
                    );
                    using var binaryReader = new BinaryReader(
                        cryptoStream,
                        Encoding.UTF8,
                        leaveOpen: true
                    );

                    int numTables = binaryReader.ReadInt32();
                    for (int i = 0; i < numTables; i++)
                    {
                        string tableName = binaryReader.ReadString();
                        var table = new Table(tableName);

                        int numColumns = binaryReader.ReadInt32();
                        for (int j = 0; j < numColumns; j++)
                            table.ColumnNames.Add(binaryReader.ReadString());

                        int numRows = binaryReader.ReadInt32();
                        for (int j = 0; j < numRows; j++)
                        {
                            var row = new Dictionary<string, object>();
                            foreach (var colName in table.ColumnNames)
                                row[colName] = SerializerUtil.DeserializeObject(
                                    binaryReader
                                );

                            table.Rows.Add(row);
                        }

                        db.Database.Add(tableName, table);
                    }
                }
                catch (CryptographicException ex)
                {
                    throw new InvalidOperationException(
                        "Decryption failed. Incorrect passkey or corrupted file.",
                        ex
                    );
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        "File format error during deserialization.",
                        ex
                    );
                }
            }

            return db;
        }

        /// <summary>
        /// Saves the given <see cref="GlasgowDB"/> instance to an encrypted file at the specified path,
        /// using AES-256 in CBC mode. The file will begin with a randomly generated IV.
        /// 
        /// A random Initialization Vector (IV) is generated for each write and stored at the beginning of the file.
        /// The encryption key is derived from the provided passphrase and this IV using PBKDF2.
        /// </summary>
        /// <param name="Database">
        /// The <see cref="GlasgowDB"/> to serialize and encrypt.
        /// </param>
        /// <param name="FileName">
        /// The destination file path. If the file already exists, it will be overwritten.
        /// </param>
        /// <param name="PassKey">
        /// The passphrase used to derive the AES encryption key. Must be provided again to decrypt via <see cref="LoadFromFile"/>.
        /// </param>
        public static void SaveToFile(
            GlasgowDB Database,
            string FileName,
            string PassKey
        )
        {
            using var fileStream = new FileStream(
                FileName,
                FileMode.Create,
                FileAccess.Write
            );
            using var aes = Aes.Create();

            aes.GenerateIV();
            fileStream.Write(aes.IV, 0, aes.IV.Length);

            aes.Key = CryptographyUtil.DeriveKey(PassKey, aes.IV);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var cryptoStream = new CryptoStream(
                fileStream,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write
            );
            using var binaryWriter = new BinaryWriter(
                cryptoStream,
                Encoding.UTF8,
                leaveOpen: true
            );

            binaryWriter.Write(Database.Database.Count);
            foreach (var tableName in Database.Database.Keys)
            {
                var table = Database.Database[tableName];
                binaryWriter.Write(table.Name);

                binaryWriter.Write(table.ColumnNames.Count);
                foreach (var colName in table.ColumnNames)
                    binaryWriter.Write(colName);

                binaryWriter.Write(table.Rows.Count);
                foreach (var row in table.Rows)
                    foreach (var colName in table.ColumnNames)
                    {
                        object? value = row.ContainsKey(colName) ? row[colName] : null;
                        SerializerUtil.SerializeObject(
                            binaryWriter,
                            value ?? Null.GetNullObject()
                        );
                    }
            }
        }
    }
}
