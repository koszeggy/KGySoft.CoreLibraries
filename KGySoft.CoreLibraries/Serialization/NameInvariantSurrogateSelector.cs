#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: NameInvariantSurrogateSelector.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    /// <summary>
    /// An <see cref="ISurrogateSelector"/> implementation that makes possible to serialize and deserialize objects by
    /// <see cref="IFormatter"/>s without storing field names. This provides compatibility for obfuscated and non-obfuscated versions of an assembly.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// You can use this surrogate selector for any non-primitive types that does not implement <see cref="ISerializable"/> interface.
    /// <note>
    /// Versioning by this surrogate selector can be accomplished only if new fields are always defined after the old ones on every level of the hierarchy.
    /// You might to use also <see cref="WeakAssemblySerializationBinder"/> to ignore version information of assemblies on deserialization.
    /// </note>
    /// <note type="caution">
    /// Please note that this surrogate selector does not identify field names on deserialization so reordering members may corrupt or fail deserialization.
    /// </note>
    /// </remarks>
    public class NameInvariantSurrogateSelector : ISurrogateSelector, ISerializationSurrogate
    {
        #region Fields

        private ISurrogateSelector next;

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Specifies the next <see cref="ISurrogateSelector"/> for surrogates to examine if the current instance does not have a surrogate for the specified type and assembly in the specified context.
        /// </summary>
        /// <param name="selector">The next surrogate selector to examine.</param>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        public void ChainSelector(ISurrogateSelector selector) => next = selector;

        /// <summary>
        /// Returns the next surrogate selector in the chain.
        /// </summary>
        /// <returns>
        /// The next surrogate selector in the chain or null.
        /// </returns>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        public ISurrogateSelector GetNextSelector() => next;

        /// <summary>
        /// Finds the surrogate that represents the specified object's type, starting with the specified surrogate selector for the specified serialization context.
        /// </summary>
        /// <returns>
        /// The appropriate surrogate for the given type in the given context.
        /// </returns>
        /// <param name="type">The <see cref="Type"/> of object that needs a surrogate.</param>
        /// <param name="context">The source or destination context for the current serialization.</param>
        /// <param name="selector">When this method returns, contains a <see cref="ISurrogateSelector"/> that holds a reference to the surrogate selector where the appropriate surrogate was found.</param>
        /// <exception cref="T:System.Security.SecurityException">The caller does not have the required permission.</exception>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            if (!type.IsPrimitive && type != Reflector.StringType && !type.IsArray && !typeof(ISerializable).IsAssignableFrom(type) )
            {
                selector = this;
                return this;
            }

            if (next != null)
                return next.GetSurrogate(type, context, out selector);

            selector = null;
            return null;

        }

        #endregion

        #region Explicitly Implemented Interface Methods

        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        void ISerializationSurrogate.GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.ArgumentNull);
            if (info == null)
                throw new ArgumentNullException(nameof(info), Res.ArgumentNull);

            Type type = obj.GetType();

            int level = 0;

            // ReSharper disable once PossibleNullReferenceException - cannot be null die to the condition
            for (Type t = type; t != Reflector.ObjectType; t = t.BaseType)
            {
                FieldInfo[] fields = BinarySerializer.GetSerializableFields(t);
                for (int i = 0; i < fields.Length; i++)
                    info.AddValue($"{level.ToString("X", NumberFormatInfo.InvariantInfo)}:{i.ToString("X", NumberFormatInfo.InvariantInfo)}", FieldAccessor.GetAccessor(fields[i]).Get(obj), fields[i].FieldType);

                // marking end of level
                info.AddValue("x" + level.ToString("X", NumberFormatInfo.InvariantInfo), null);
                level++;
            }
        }

        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        object ISerializationSurrogate.SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.ArgumentNull);
            if (info == null)
                throw new ArgumentNullException(nameof(info), Res.ArgumentNull);

            // can occur if the original type was ISerializable and GetObjectData has changed the type to a non-ISerializable one
            // Example: .NET 4.6 EnumEqualityComparer->ObjectEqualityComparer
            if (info.MemberCount == 0)
                return obj;

            Type type = obj.GetType();
            int level;
            int fieldIndex;

            // ordering entries because they can be mixed up by BinaryFormatter
            CircularSortedList<ulong, SerializationEntry> list = new CircularSortedList<ulong, SerializationEntry>();
            foreach (SerializationEntry entry in info)
            {
                int pos;
                if ((pos = entry.Name.IndexOf(':')) > 0 && pos < entry.Name.Length - 1)
                {
                    if (!Int32.TryParse(entry.Name.Substring(0, pos), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out level))
                        throw new SerializationException(Res.BinarySerializationUnexpectedSerializationInfoElement(entry.Name));

                    if (!Int32.TryParse(entry.Name.Substring(pos + 1), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out fieldIndex))
                        throw new SerializationException(Res.BinarySerializationUnexpectedSerializationInfoElement(entry.Name));

                    list.Add(((ulong)level << 32) | (uint)fieldIndex, entry);
                }
                // end of level found
                else if (entry.Name.Length >= 2 && entry.Name[0] == 'x')
                {
                    if (!Int32.TryParse(entry.Name.Substring(1), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out level))
                        throw new SerializationException(Res.BinarySerializationUnexpectedSerializationInfoElement(entry.Name));
                    list.Add(((ulong)level << 32) | UInt32.MaxValue, entry);
                }
                else
                    throw new SerializationException(Res.BinarySerializationUnexpectedSerializationInfoElement(entry.Name));
            }

            FieldInfo[] fields = BinarySerializer.GetSerializableFields(type);
            level = 0;
            fieldIndex = 0;
            foreach (SerializationEntry entry in list.Values)
            {
                if (type == Reflector.ObjectType)
                    throw new SerializationException(Res.BinarySerializationObjectHierarchyChangedSurrogate(obj.GetType()));

                // field found
                if (entry.Name == level.ToString("X", NumberFormatInfo.InvariantInfo) + ":" + fieldIndex.ToString("X", NumberFormatInfo.InvariantInfo))
                {
                    if (fieldIndex >= fields.Length)
                        throw new SerializationException(Res.BinarySerializationMissingFieldSurrogate(type, obj.GetType()));

                    if (!fields[fieldIndex].FieldType.CanAcceptValue(entry.Value))
                        throw new SerializationException(Res.BinarySerializationUnexpectedFieldType(obj.GetType(), entry.Value, type, fields[fieldIndex].Name));

                    FieldInfo field = fields[fieldIndex++];
#if NETSTANDARD2_0
                    if (field.IsInitOnly || !field.IsStatic && obj.GetType().IsValueType)
                        field.SetValue(obj, entry.Value);
                    else
#endif
                    {
                        FieldAccessor.GetAccessor(field).Set(obj, entry.Value);
                    }
                }
                // end of level found
                else if (entry.Name == "x" + level.ToString("X", NumberFormatInfo.InvariantInfo))
                {
                    level++;

                    // ReSharper disable once PossibleNullReferenceException - see first line inside the loop
                    type = type.BaseType;
                    fields = BinarySerializer.GetSerializableFields(type);
                    fieldIndex = 0;
                }
                else
                    throw new SerializationException(Res.BinarySerializationUnexpectedSerializationInfoElement(entry.Name));
            }

            if (type != Reflector.ObjectType)
                throw new SerializationException(Res.BinarySerializationObjectHierarchyChangedSurrogate(obj.GetType()));

            return obj;
        }

        #endregion

        #endregion
    }
}
