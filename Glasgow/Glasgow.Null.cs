/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Provides a type-safe, singleton-based representation of null or missing values
    /// within the GlasgowDB system, especially in scenarios where <c>null</c> cannot 
    /// be used directly or unambiguously.
    ///
    /// <para>
    /// In many serialization, storage, or transport layers, the native <c>null</c> reference 
    /// in .NET may lose semantic meaning or cause ambiguity, especially when dealing with 
    /// heterogeneous data structures or binary serialization formats that don't inherently 
    /// preserve <c>null</c> as a unique concept.
    /// </para>
    ///
    /// <para>
    /// To solve this, the GlasgowDB system introduces a special singleton object that acts 
    /// as a "boxed null." This object can be safely stored, compared, and transferred in 
    /// any context where a non-null reference is required but the semantic value is intended 
    /// to represent the absence of meaningful data.
    /// </para>
    /// </summary>
    public sealed class Null
    {
        /// <summary>
        /// A globally unique, immutable singleton instance used to represent a logical "null" 
        /// value across all GlasgowDB internals. This object is guaranteed to be reference-equal 
        /// to itself across any comparison during the lifetime of the application.
        /// 
        /// <para>
        /// This is a private implementation detail, exposed through public accessors and 
        /// comparison methods to prevent misuse.
        /// </para>
        /// </summary>
        private static readonly object _nullObject = new();

        /// <summary>
        /// Retrieves the singleton instance used to represent null or missing values in GlasgowDB.
        /// 
        /// <para>
        /// This method provides a consistent reference to the special null object. Consumers 
        /// can use it to assign values that semantically mean "no value" in a database context.
        /// </para>
        ///
        /// <para>Example usage:</para>
        /// <code>
        /// var nullValue = Null.GetNullObject();
        /// myRow["LastName"] = nullValue;
        /// </code>
        /// 
        /// <returns>
        /// A reference-equal, immutable object representing a logical null within the database.
        /// </returns>
        /// </summary>
        public static object GetNullObject()
        {
            return _nullObject;
        }

        /// <summary>
        /// Determines whether a given object is the singleton null representation used by GlasgowDB.
        ///
        /// <para>
        /// This is a reference comparison that returns <c>true</c> only when the input object is 
        /// exactly the same instance as the internal null marker.
        /// </para>
        ///
        /// <para>Example usage:</para>
        /// <code>
        /// if (Null.IsNullObject(value))
        ///     Console.WriteLine("Value is considered null in GlasgowDB.");
        /// </code>
        /// 
        /// <param name="obj">The object to evaluate.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is the singleton null marker; otherwise, <c>false</c>.
        /// </returns>
        /// </summary>
        public static bool IsNullObject(object obj)
        {
            return obj == _nullObject;
        }
    }
}
