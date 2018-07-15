using System;
using System.Xml.Linq;

namespace KGySoft.Libraries.Serialization
{
    internal class XElementSerializer
    {
        private readonly XmlSerializationOptions options;

        public XElementSerializer(XmlSerializationOptions options)
        {
            this.options = options;
        }

        public XElement Serialize(object o)
        {
            throw new NotImplementedException();
        }
    }
}
