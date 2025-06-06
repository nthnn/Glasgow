/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Provides functionality for serializing and deserializing .NET objects to and from a custom binary format
    /// used internally by GlasgowDB. The format includes a type identifier byte followed by the raw data 
    /// representation. This utility supports a variety of core .NET types including integers, floating-point numbers, 
    /// strings, date/time, booleans, and binary blobs. Objects are prefixed with a <see cref="DataTypes"/> identifier 
    /// to facilitate accurate deserialization.
    /// </summary>
    internal sealed class SerializerUtil
    {
        /// <summary>
        /// Serializes the specified .NET object into binary format using the provided <see cref="BinaryWriter"/>.
        /// This method begins by writing a type identifier byte from the <see cref="DataTypes"/> enumeration, 
        /// followed by the actual serialized data for the object.
        /// </summary>
        /// <param name="Writer">
        /// An instance of <see cref="BinaryWriter"/> that is used to write the serialized output to a stream.
        /// </param>
        /// <param name="Value">
        /// The object to serialize. Supported types include:
        /// <list type="bullet">
        /// <item><see cref="null"/> (written as <see cref="DataTypes.Null"/>)</item>
        /// <item><see cref="int"/> (written as 4 bytes)</item>
        /// <item><see cref="long"/> (written as 8 bytes)</item>
        /// <item><see cref="double"/> (IEEE 754 format)</item>
        /// <item><see cref="float"/> (IEEE 754 format)</item>
        /// <item><see cref="decimal"/> (high-precision financial format)</item>
        /// <item><see cref="string"/> (length-prefixed UTF-8 encoding)</item>
        /// <item><see cref="bool"/> (1 byte)</item>
        /// <item><see cref="DateTime"/> (serialized via <c>DateTime.ToBinary()</c>)</item>
        /// <item><see cref="byte[]"/> (length-prefixed binary blob)</item>
        /// </list>
        /// </param>
        /// <exception cref="NotSupportedException">
        /// Thrown if the type of <paramref name="Value"/> is not supported by the serializer.
        /// </exception>
        public static void SerializeObject(
            BinaryWriter Writer,
            object Value
        )
        {
            if (Null.IsNullObject(Value))
            {
                Writer.Write((byte)DataTypes.Null);
            }
            else if (Value is int)
            {
                Writer.Write((byte)DataTypes.Integer);
                Writer.Write((int)Value);
            }
            else if (Value is long)
            {
                Writer.Write((byte)DataTypes.Long);
                Writer.Write((long)Value);
            }
            else if (Value is double)
            {
                Writer.Write((byte)DataTypes.Double);
                Writer.Write((double)Value);
            }
            else if (Value is float)
            {
                Writer.Write((byte)DataTypes.Float);
                Writer.Write((float)Value);
            }
            else if (Value is decimal)
            {
                Writer.Write((byte)DataTypes.Decimal);
                Writer.Write((decimal)Value);
            }
            else if (Value is string)
            {
                Writer.Write((byte)DataTypes.String);
                Writer.Write((string)Value);
            }
            else if (Value is bool)
            {
                Writer.Write((byte)DataTypes.Boolean);
                Writer.Write((bool)Value);
            }
            else if (Value is DateTime)
            {
                Writer.Write((byte)DataTypes.DateTime);
                Writer.Write(((DateTime)Value).ToBinary());
            }
            else if (Value is byte[])
            {
                Writer.Write((byte)DataTypes.Blob);

                byte[] blob = (byte[])Value;
                Writer.Write(blob.Length);
                Writer.Write(blob);
            }
            else throw new NotSupportedException(
                $"Data type not supported for serialization: {Value.GetType().Name}"
            );
        }

        /// <summary>
        /// Deserializes a .NET object from binary format using the provided <see cref="BinaryReader"/>.
        /// This method reads an initial byte which indicates the type of the object, followed by 
        /// the corresponding serialized data.
        /// </summary>
        /// <param name="Reader">
        /// An instance of <see cref="BinaryReader"/> that provides the stream data to deserialize.
        /// </param>
        /// <returns>
        /// The deserialized object. The type returned corresponds to the identifier byte read from the stream:
        /// <list type="bullet">
        /// <item><see cref="Null"/> for null values</item>
        /// <item><see cref="int"/>, <see cref="long"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/></item>
        /// <item><see cref="string"/>, <see cref="bool"/>, <see cref="DateTime"/></item>
        /// <item><see cref="byte[]"/> for binary blobs</item>
        /// </list>
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the type identifier read from the stream does not match any known <see cref="DataTypes"/>.
        /// </exception>
        public static object DeserializeObject(BinaryReader Reader)
        {
            byte typeId = Reader.ReadByte();
            switch ((DataTypes)typeId)
            {
                case DataTypes.Null:
                    return Null.GetNullObject();

                case DataTypes.Integer:
                    return Reader.ReadInt32();

                case DataTypes.Long:
                    return Reader.ReadInt64();

                case DataTypes.Double:
                    return Reader.ReadDouble();

                case DataTypes.Float:
                    return Reader.ReadSingle();

                case DataTypes.Decimal:
                    return Reader.ReadDecimal();

                case DataTypes.String:
                    return Reader.ReadString();

                case DataTypes.Boolean:
                    return Reader.ReadBoolean();

                case DataTypes.DateTime:
                    return DateTime.FromBinary(Reader.ReadInt64());

                case DataTypes.Blob:
                    return Reader.ReadBytes(Reader.ReadInt32());

                default:
                    throw new NotSupportedException(
                        $"Unknown data type encountered during deserialization: {typeId}"
                    );
            }
        }
    }
}
