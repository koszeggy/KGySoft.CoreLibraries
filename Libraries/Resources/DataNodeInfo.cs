#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DataNodeInfo.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2017 - All Rights Reserved
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
            object pos = Accessors.DataNodeInfo_ReaderPosition_Get(nodeInfoWinForms);
            return new DataNodeInfo
            {
                CompatibleFormat = true,
                Name = Accessors.DataNodeInfo_Name_Get(nodeInfoWinForms),
                Comment = Accessors.DataNodeInfo_Comment_Get(nodeInfoWinForms),
                TypeName = Accessors.DataNodeInfo_TypeName_Get(nodeInfoWinForms),
                MimeType = Accessors.DataNodeInfo_MimeType_Get(nodeInfoWinForms),
                ValueData = Accessors.DataNodeInfo_ValueData_Get(nodeInfoWinForms),
                Line = Accessors.Point_Y_Get(pos),
                Column = Accessors.Point_X_Get(pos)
            };
        }

        #endregion

        #region Instance Methods

        internal DataNodeInfo Clone()
        {
            return (DataNodeInfo)MemberwiseClone();
        }

        internal void DetectCompatibleFormat()
        {
            CompatibleFormat = MimeType != ResXCommon.KGySoftSerializedObjectMimeType
                               && (TypeName == null || (!TypeName.StartsWith(ResXCommon.ResXFileRefNameKGySoft, StringComparison.Ordinal) && !TypeName.StartsWith(ResXCommon.ResXNullRefNameKGySoft, StringComparison.Ordinal)));
        }

        #endregion

        #endregion
    }
}
