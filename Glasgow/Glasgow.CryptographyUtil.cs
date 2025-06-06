/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

using System.Security.Cryptography;

namespace Glasgow
{
    /// <summary>
    /// Provides utility methods for cryptographic operations used throughout GlasgowDB,
    /// including secure key derivation from passwords or passphrases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is intended to centralize cryptographic routines such as PBKDF2-based
    /// key derivation, ensuring consistent parameters (iteration count, hash algorithm)
    /// across the application. All methods are static, as they do not maintain internal state.
    /// </para>
    /// <para>
    /// <strong>Security Considerations:</strong>
    /// <list type="bullet">
    ///   <item>
    ///     Always use a sufficiently random, unique salt when deriving keys. Reusing salts
    ///     across different data or users can weaken security by making pre-computation attacks easier.
    ///   </item>
    ///   <item>
    ///     The iteration count (100,000) strikes a balance between performance and security as of 2025.
    ///     Adjust upward if hardware allows, to increase resistance against brute-force attacks.
    ///   </item>
    ///   <item>
    ///     The derived key length is fixed at 32 bytes (256 bits), suitable for AES-256.
    ///     If a different cipher or key size is required, consider exposing a parameter to change output length.
    ///   </item>
    ///   <item>
    ///     PBKDF2 with HMAC-SHA256 is widely supported and considered secure for password-based key derivation.
    ///     However, if future requirements demand a memory-hard function (e.g., Argon2), this utility should be updated.
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class CryptographyUtil
    {
        /// <summary>
        /// Derives a 256-bit (32-byte) symmetric encryption key from the provided passphrase and salt,
        /// using the PBKDF2 (Password-Based Key Derivation Function 2) algorithm (RFC 2898) with HMAC-SHA256
        /// and 100,000 iterations.
        /// </summary>
        /// <param name="PassKey">
        /// The user-supplied passphrase or password from which to derive the key. This should be a strong,
        /// high-entropy string (e.g., a randomly generated password or user-provided phrase). Using weak or
        /// guessable passphrases can severely reduce the security of the derived key.
        /// </param>
        /// <param name="Salt">
        /// A cryptographic salt value, represented as a byte array. The salt must be at least 16 bytes long
        /// and should be generated using a secure random number generator (e.g., <see cref="RandomNumberGenerator"/>).
        /// Each distinct usage (e.g., per encrypted database file or per user) should employ a unique, unpredictable salt.
        /// </param>
        /// <returns>
        /// A byte array of length 32, containing the derived 256-bit key. This key is suitable for use with
        /// AES-256 or other symmetric encryption algorithms requiring a 32-byte key.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <strong>Algorithm Details:</strong>
        /// <list type="bullet">
        ///   <item>
        ///     Instantiates an <see cref="Rfc2898DeriveBytes"/> (PBKDF2) object with:
        ///     <list type="number">
        ///       <item>The provided <paramref name="PassKey"/> as the password.</item>
        ///       <item>The provided <paramref name="Salt"/> as the salt.</item>
        ///       <item>An iteration count of 100,000, meaning the HMAC-SHA256 function is applied repeatedly
        ///           100,000 times to slow down brute-force attacks.</item>
        ///       <item>The underlying hash algorithm <see cref="HashAlgorithmName.SHA256"/>.</item>
        ///     </list>
        ///   </item>
        ///   <item>
        ///     Calls <c>GetBytes(32)</c> to produce a 32-byte key derived from the passphrase and salt.
        ///   </item>
        ///   <item>
        ///     Disposes the <see cref="Rfc2898DeriveBytes"/> instance immediately after key extraction,
        ///     ensuring no sensitive data lingers in memory longer than necessary.
        ///   </item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Performance and Security Tradeoff:</strong> The iteration count (100,000) is chosen to be
        /// sufficiently high to deter brute-force attacks while still allowing reasonably fast key derivation
        /// on modern hardware. If low-latency is critical and hardware is less capable, consider lowering
        /// the iteration count (at the cost of reduced security). Conversely, if your threat model demands
        /// stronger resistance, increase the iteration count or switch to a memory-hard KDF.
        /// </para>
        /// <para>
        /// <strong>Error Handling:</strong> This method throws an <see cref="ArgumentNullException"/> if either
        /// <paramref name="PassKey"/> or <paramref name="Salt"/> is null. If <paramref name="Salt"/> is empty
        /// or poorly random, the derived key may be less secure.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="PassKey"/> or <paramref name="Salt"/> is null.
        /// </exception>
        public static byte[] DeriveKey(string PassKey, byte[] Salt)
        {
            if (PassKey == null)
                throw new ArgumentNullException(nameof(PassKey), "PassKey must not be null.");
            if (Salt == null)
                throw new ArgumentNullException(nameof(Salt), "Salt must not be null.");

            using var pbkdf2 = new Rfc2898DeriveBytes(
                PassKey,
                Salt,
                100000,
                HashAlgorithmName.SHA256
            );
            return pbkdf2.GetBytes(32);
        }
    }
}
