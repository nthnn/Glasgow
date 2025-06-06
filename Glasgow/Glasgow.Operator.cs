/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Enumerates the available comparison operators used for filtering, searching, and conditional 
    /// evaluations within GlasgowDB queries.
    ///
    /// <para>
    /// These operators define how a particular field or value in a dataset should be evaluated 
    /// against a given condition during query execution. They are typically used in where-clause 
    /// expressions, filter functions, or conditional indexes.
    /// </para>
    ///
    /// <para>
    /// The applicable operators depend on the data type of the field being compared. For example, 
    /// numeric and temporal fields support range-based comparisons (e.g., <see cref="LESS_THAN"/>), 
    /// whereas string fields support pattern-based matching (e.g., <see cref="CONTAINS"/>).
    /// </para>
    /// </summary>
    public enum Operator
    {
        /// <summary>
        /// Checks if two values are equal.
        /// </summary>
        EQUALS,

        /// <summary>
        /// Checks if two values are not equal.
        /// </summary>
        NOT_EQUALS,

        /// <summary>
        /// Checks if a numeric or DateTime value is less than the comparison value.
        /// </summary>
        LESS_THAN,

        /// <summary>
        /// Checks if a numeric or DateTime value is greater than the comparison value.
        /// </summary>
        GREATER_THAN,

        /// <summary>
        /// Checks if a numeric or DateTime value is less than or equal to the comparison value.
        /// </summary>
        LESS_THAN_OR_EQUAL,

        /// <summary>
        /// Checks if a numeric or DateTime value is greater than or equal to the comparison value.
        /// </summary>
        GREATER_THAN_OR_EQUAL,

        /// <summary>
        /// Checks if a string contains the specified substring (case-insensitive).
        /// </summary>
        CONTAINS,

        /// <summary>
        /// Checks if a string starts with the specified substring (case-insensitive).
        /// </summary>
        STARTS_WITH,

        /// <summary>
        /// Checks if a string ends with the specified substring (case-insensitive).
        /// </summary>
        ENDS_WITH
    }
}
