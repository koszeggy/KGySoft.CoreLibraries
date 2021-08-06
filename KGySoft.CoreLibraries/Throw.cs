#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Throw.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Resources;
using System.Runtime.Serialization;

using KGySoft.Annotations;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft
{
    /// <summary>
    /// Helper class for throwing exceptions from performance-critical types.
    /// For example, a generic indexer can be 2.5x times faster if it contains no throw but a method call
    /// just because the presence of throw prevents inlining.
    /// </summary>
    internal static class Throw
    {
        #region Methods

        #region Internal Methods

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentNullException(Argument arg) => throw CreateArgumentNullException(arg, Res.ArgumentNull);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T ArgumentNullException<T>(Argument arg) => throw CreateArgumentNullException(arg, Res.ArgumentNull);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentNullException(Argument arg, string message) => throw CreateArgumentNullException(arg, message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentNullException(string arg) => throw CreateArgumentNullException(arg, Res.ArgumentNull);

        internal static void ThrowIfNullIsInvalid<T>(object? value, Argument? arg = null)
        {
            if (value == null && default(T) != null)
                Throw.ArgumentNullException(arg ?? Argument.value);
        }

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentException(string message, Exception? inner = null) => throw CreateArgumentException(null, message, inner);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentException(Argument arg, string message) => throw CreateArgumentException(arg, message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentException(Argument arg, string message, Exception? inner) => throw CreateArgumentException(arg, message, inner);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T ArgumentException<T>(Argument arg, string message) => throw CreateArgumentException(arg, message);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentOutOfRangeException(Argument arg) => throw CreateArgumentOutOfRangeException(arg, Res.ArgumentOutOfRange);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentOutOfRangeException(Argument arg, string message) => throw CreateArgumentOutOfRangeException(arg, message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ArgumentOutOfRangeException(string paramName, string message) => throw CreateArgumentOutOfRangeException(paramName, message);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void IndexOutOfRangeException() => throw CreateIndexOutOfRangeException(Res.IndexOutOfRange);

        [ContractAnnotation("=> halt")]
        [DoesNotReturn]
        internal static void EnumArgumentOutOfRange<TEnum>(Argument arg, TEnum value) where TEnum : struct, Enum 
            => throw CreateArgumentOutOfRangeException(arg, Res.EnumOutOfRange(value));

        [ContractAnnotation("=> halt")]
        [DoesNotReturn]
        internal static void EnumArgumentOutOfRangeWithValues<TEnum>(Argument arg, TEnum value) where TEnum : struct, Enum
            => throw CreateArgumentOutOfRangeException(arg, Res.EnumOutOfRangeWithValues(value));
        
        [ContractAnnotation("=> halt")]
        [DoesNotReturn]
        internal static void FlagsEnumArgumentOutOfRange<TEnum>(Argument arg, TEnum value) where TEnum : struct, Enum
            => throw CreateArgumentOutOfRangeException(arg, Res.FlagsEnumOutOfRange(value));

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void KeyNotFoundException() => throw CreateKeyNotFoundException(Res.IDictionaryKeyNotFound);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void KeyNotFoundException(string message) => throw CreateKeyNotFoundException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T KeyNotFoundException<T>(string message) => throw CreateKeyNotFoundException(message);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void InvalidOperationException(string message) => throw CreateInvalidOperationException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T InvalidOperationException<T>(string message) => throw CreateInvalidOperationException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T InvalidOperationException<T>(string message, Exception inner) => throw CreateInvalidOperationException(message, inner);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void InternalError(string internalError) => throw CreateInvalidOperationException(Res.InternalError(internalError));
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T InternalError<T>(string internalError) => throw CreateInvalidOperationException(Res.InternalError(internalError));

        internal static void MissingManifestResourceException(string message) => throw new MissingManifestResourceException(message);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void NotSupportedException() => throw CreateNotSupportedException(Res.NotSupported);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T NotSupportedException<T>() => throw CreateNotSupportedException(Res.NotSupported);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void NotSupportedException(string message) => throw CreateNotSupportedException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T NotSupportedException<T>(string message) => throw CreateNotSupportedException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void NotSupportedException(string message, Exception inner) => throw CreateNotSupportedException(message, inner);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ObjectDisposedException() => throw CreateObjectDisposedException(Res.ObjectDisposed);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T ObjectDisposedException<T>() => throw CreateObjectDisposedException(Res.ObjectDisposed);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ObjectDisposedException(string name) => throw CreateObjectDisposedException(Res.ObjectDisposed, name);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ReflectionException(string message) => throw CreateReflectionException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T ReflectionException<T>(string message) => throw CreateReflectionException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void ReflectionException(string message, Exception? inner) => throw CreateReflectionException(message, inner);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void TypeLoadException(string message) => throw CreateTypeLoadException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void TypeLoadException(string message, Exception inner) => throw CreateTypeLoadException(message, inner);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void FileNotFoundException(string message, string fileName) => throw new FileNotFoundException(message, fileName);

        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void SerializationException(string message) => throw CreateSerializationException(message);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static T SerializationException<T>(string message, Exception? inner = null) => throw CreateSerializationException(message, inner);
        [ContractAnnotation("=> halt")][DoesNotReturn]internal static void SerializationException(string message, Exception inner) => throw CreateSerializationException(message, inner);

        [DoesNotReturn]internal static void PlatformNotSupportedException(string message) => throw CreatePlatformNotSupportedException(message);
        [DoesNotReturn]internal static T PlatformNotSupportedException<T>(string message) => throw CreatePlatformNotSupportedException(message);

        #endregion

        #region Private Methods

        private static Exception CreateArgumentNullException(string arg, string message) => new ArgumentNullException(arg, message);
        private static Exception CreateArgumentNullException(Argument arg, string message) => CreateArgumentNullException(Enum<Argument>.ToString(arg), message);
        private static Exception CreateArgumentException(Argument? arg, string message, Exception? inner = null) => arg.HasValue ? new ArgumentException(message, Enum<Argument>.ToString(arg.Value), inner) : new ArgumentException(message, inner);
        private static Exception CreateArgumentOutOfRangeException(string paramName, string message) => new ArgumentOutOfRangeException(paramName, message);
        private static Exception CreateArgumentOutOfRangeException(Argument arg, string message) => CreateArgumentOutOfRangeException(Enum<Argument>.ToString(arg), message);
        private static Exception CreateIndexOutOfRangeException(string message) => new IndexOutOfRangeException(message);
        private static Exception CreateKeyNotFoundException(string message) => new KeyNotFoundException(message);
        private static Exception CreateInvalidOperationException(string message, Exception? inner = null) => new InvalidOperationException(message, inner);
        private static Exception CreateNotSupportedException(string message, Exception? inner = null) => new NotSupportedException(message, inner);
        private static Exception CreateObjectDisposedException(string message, string? name = null) => new ObjectDisposedException(name, message);
        private static Exception CreateReflectionException(string message, Exception? inner = null) => new ReflectionException(message, inner);
        private static Exception CreateTypeLoadException(string message, Exception? inner = null) => new TypeLoadException(message, inner);
        private static Exception CreateSerializationException(string message, Exception? inner = null) => new SerializationException(message, inner);
        private static Exception CreatePlatformNotSupportedException(string message) => new PlatformNotSupportedException(message);

        #endregion

        #endregion
    }
}