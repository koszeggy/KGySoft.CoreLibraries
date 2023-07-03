#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.DataTypesEnumerator.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Collections.Generic;

using KGySoft.Collections;

#endregion

namespace KGySoft.Serialization.Binary
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// A special lightweight enumerator for encoded <see cref="DataTypes"/> collections.
        /// </summary>
        private sealed class DataTypesEnumerator
        {
            #region Fields

            private readonly IList<DataTypes> dataTypes;
            private DataTypes current;
            private int index;
            private Stack<(DataTypes, int)>? restorePoints;

            #endregion

            #region Properties

            internal DataTypes CurrentSeparated => IsCollectionType(current) ? GetCollectionDataType(current) : GetElementDataType(current);

            internal DataTypes Current => current;

            #endregion

            #region Constructors

            internal DataTypesEnumerator(IList<DataTypes> source, bool moveFirst = false)
            {
                dataTypes = source;
                if (moveFirst)
                    MoveNext();
            }

            #endregion

            #region Methods

            #region Public Methods

            public override string ToString() => DataTypeToString(current);

            #endregion

            #region Internal Methods

            internal bool MoveNext()
            {
                if (index < dataTypes.Count)
                {
                    current = dataTypes[index];
                    index += 1;
                    return true;
                }

                current = DataTypes.Null;
                return false;
            }

            internal bool MoveNextExtracted()
            {
                if (current == DataTypes.Null && index >= dataTypes.Count)
                    return false;

                if (!IsCollectionType(current))
                    return MoveNext();

                current = GetElementDataType(current);
                return current != DataTypes.Null || MoveNext();
            }

            [System.Obsolete]
            internal DataTypesEnumerator Clone() =>
                new DataTypesEnumerator(dataTypes)
                {
                    current = current,
                    index = index
                };


            internal void Reset()
            {
                index = 0;
                current = DataTypes.Null;
            }

            internal void Save()
            {
                restorePoints ??= new Stack<(DataTypes, int)>();
                restorePoints.Push((current, index));
            }

            internal void Restore()
            {
                if (restorePoints is { Count: > 0 } stack)
                    (current, index) = stack.Pop();
                else
                    Throw.InvalidOperationException(Res.InternalError("Restore without Save"));
            }

            internal IList<DataTypes> ExtractCurrentSegment()
            {
                Save();
                var result = ReadToNextSegment();
                Restore();
                return result.dataTypes; // TODO: just result
            }

            internal DataTypesEnumerator ReadToNextSegment()
            {
                Debug.Assert(current != DataTypes.Null || index < dataTypes.Count, "Enumeration is already finished");
                var result = new List<DataTypes>(dataTypes.Count - index + 1);
                int skip = 1;
                do
                {
                    result.Add(current);
                    skip += IsCollectionType(current)
                        ? GetNumberOfElementTypes(current) - (GetElementDataType(current) == DataTypes.Null ? 1 : 2)
                        : -1;
                } while (MoveNext() && skip > 0);

                Debug.Assert(skip == 0, "Failed to read current segment");
                return new DataTypesEnumerator(result, true);
            }

            #endregion

            #endregion
        }
    }
}
