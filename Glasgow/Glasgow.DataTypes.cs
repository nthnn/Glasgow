/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Enumerates all supported data types used by the <see cref="GlasgowDB"/> system for 
    /// serializing and storing values in its internal tabular structure.
    /// 
    /// Each enumeration value corresponds to a specific .NET type and dictates how the value is 
    /// serialized, deserialized, stored, and interpreted within the database engine.
    /// These types ensure compatibility with common .NET primitives and are essential for 
    /// consistent binary serialization handled by <see cref="SerializerUtil"/>.
    /// 
    /// The <see cref="SerializerUtil"/> uses this enum as metadata to identify the type of 
    /// value being serialized or deserialized, enabling type-safe reconstruction of data across 
    /// sessions, storage layers, or transport boundaries.
    /// </summary>
    public enum DataTypes : byte
    {
        /// <summary>
        /// Represents a null or undefined value. This is equivalent to <c>null</c> in .NET and is 
        /// used to store missing or unset entries in a table row.
        /// 
        /// During serialization, this acts as a placeholder without a concrete value. On deserialization, 
        /// it is interpreted as a null reference.
        /// 
        /// <b>Use Case:</b> Optional fields, empty table cells, or schema placeholders.
        /// </summary>
        Null,

        /// <summary>
        /// Represents a 32-bit signed integer (System.Int32).
        /// 
        /// Commonly used for numeric values such as counters, identifiers, or index values that fit 
        /// within the range of -2,147,483,648 to 2,147,483,647.
        /// 
        /// <b>Use Case:</b> Row IDs, user age, quantity, flags, or enum storage.
        /// </summary>
        Integer,

        /// <summary>
        /// Represents a 64-bit signed integer (System.Int64).
        /// 
        /// Suitable for large numeric values that exceed the 32-bit integer range, such as file sizes, 
        /// timestamps (Unix epoch), or large counters.
        /// 
        /// <b>Use Case:</b> Epoch time, database auto-increment keys, large data IDs.
        /// </summary>
        Long,

        /// <summary>
        /// Represents a double-precision floating-point number (System.Double).
        /// 
        /// Useful for storing numeric values requiring high range and precision (15-16 digits of accuracy), 
        /// such as measurements, scientific data, or results from floating-point calculations.
        /// 
        /// <b>Use Case:</b> GPS coordinates, sensor data, weights, temperature.
        /// </summary>
        Double,

        /// <summary>
        /// Represents a Unicode text string (System.String).
        /// 
        /// This type can hold arbitrarily long text values encoded in UTF-8 or UTF-16, depending on 
        /// the internal serialization strategy. It supports internationalization and complex text content.
        /// 
        /// <b>Use Case:</b> Names, descriptions, serialized JSON, user input, messages.
        /// </summary>
        String,

        /// <summary>
        /// Represents a boolean value (System.Boolean), either <c>true</c> or <c>false</c>.
        /// 
        /// Useful for toggles, feature flags, conditionals, and binary state indicators.
        /// 
        /// <b>Use Case:</b> IsActive flag, IsAdmin role, enabled/disabled states.
        /// </summary>
        Boolean,

        /// <summary>
        /// Represents a date and time value (System.DateTime).
        /// 
        /// Serialized using a standard format (e.g., ticks or ISO 8601), this type enables temporal 
        /// data storage such as timestamps, schedules, and audit trails.
        /// 
        /// <b>Use Case:</b> CreatedAt, updatedAt, expiration dates, appointment slots.
        /// </summary>
        DateTime,

        /// <summary>
        /// Represents a binary large object (BLOB) as a byte array (System.Byte[]).
        /// 
        /// This type is suitable for storing raw binary content such as images, documents, encoded data, 
        /// or custom-serialized objects. Size limitations depend on the implementation and system resources.
        /// 
        /// <b>Use Case:</b> Profile pictures, encrypted blobs, audio samples, custom payloads.
        /// </summary>
        Blob,

        /// <summary>
        /// Represents a single-precision floating-point number (System.Single).
        /// 
        /// Offers less precision (7 digits) and range than a <see cref="Double"/>, but uses less memory 
        /// and is often sufficient for performance-sensitive applications or graphical data.
        /// 
        /// <b>Use Case:</b> Game data, vector coordinates, approximate calculations.
        /// </summary>
        Float,

        /// <summary>
        /// Represents a decimal number with high precision (System.Decimal).
        /// 
        /// Ideal for storing financial and monetary values where precision and scale are critical, as 
        /// this type avoids rounding errors common with floating-point types.
        /// 
        /// <b>Use Case:</b> Currency, tax calculations, interest rates, accounting figures.
        /// </summary>
        Decimal
    }
}
