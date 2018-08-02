#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GenerateObjectSettings.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings


#endregion

using System;
using System.Collections.Generic;

namespace KGySoft.Libraries
{
    public sealed class GenerateObjectSettings
    {
        public int CollectionsLength { get; set; } = 1;

        public bool AllowNulls { get; set; }

        public RandomString? StringCreationOptions { get; set; }

        // TODO: stringek hossza? Egyéb típusok min/maxa (akár csak allow negative)? Float stratégia?
        // TODO: datetime/ simadate/múlt/jövő/vagypl. 100 éven belül?

        public bool TryAutoResolveDelegates { get; set; }
        public bool TryAutoResolveInterfaces { get; set; } = true;
        public bool TryAutoResolveAbstractTypes { get; set; } = true;
        public bool TryUseDerivedTypes { get; set; }

        // vagy ctor nélkül és 
        public bool UseDefaultCtorAndPublicProperties { get; set; }

        // todo - kell egyáltalán? Hiszen akkor már nem véletlen - inkább legyen jó az auto set
        // Vagy: pl. Assemblyre/typeokra oldjon fel, ahol matcheket keres, vagy MethodInfo collectionre, amikből választ... ehh, ez már béna
        public Func<Type, Delegate> ResolveDelegate { get; set; }

        // called for interfaces, abstract types (including base delegate, multicastdelegate, array, enum) and every non-sealed classes
        public Func<Type, Type> ResolveType { get; set; }
    }
}
