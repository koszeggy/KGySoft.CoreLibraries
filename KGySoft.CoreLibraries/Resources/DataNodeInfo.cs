#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DataNodeInfo.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2017 - All Rights Reserved
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
using KGySoft.Reflection;

#endregion

namespace KGySoft.Resources
{
    internal class DataNodeInfo
    {
        #region Fields

        internal bool CompatibleFormat;
        internal string Name;
        internal string Comment;
        internal string TypeName;
        internal string AssemblyAliasValue;
        internal string MimeType;
        internal string ValueData;
        internal string BasePath;
        internal int Line; //only used to track position in the reader
        internal int Column; //only used to track position in the reader

        #endregion

        #region Methods

        #region Static Methods

        internal static DataNodeInfo InitFromWinForms(object nodeInfoWinForms)
        {
            object pos = Accessors.DataNodeInfo_GetReaderPosition(nodeInfoWinForms);
            return new DataNodeInfo
            {
                CompatibleFormat = true,
                Name = Accessors.DataNodeInfo_GetName(nodeInfoWinForms),
                Comment = Accessors.DataNodeInfo_GetComment(nodeInfoWinForms),
                TypeName = Accessors.DataNodeInfo_GetTypeName(nodeInfoWinForms),
                MimeType = Accessors.DataNodeInfo_GetMimeType(nodeInfoWinForms),
                ValueData = Accessors.DataNodeInfo_GetValueData(nodeInfoWinForms),
                Line = Accessors.Point_GetY(pos),
                Column = Accessors.Point_GetX(pos)
            };
        }

        #endregion

        #region Instance Methods

        internal DataNodeInfo Clone() => (DataNodeInfo)MemberwiseClone();

        internal void DetectCompatibleFormat()
        {
            CompatibleFormat = MimeType != ResXCommon.KGySoftSerializedObjectMimeType
                && (TypeName == null || (!TypeName.StartsWith(ResXCommon.ResXFileRefNameKGySoft, StringComparison.Ordinal) && !TypeName.StartsWith(ResXCommon.ResXNullRefNameKGySoft, StringComparison.Ordinal)));
        }

        #endregion

        #endregion
    }
}
