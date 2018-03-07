using System.ServiceModel.Channels;
using System.Xml;

namespace WCFUtils
{
    class RawBodyWriter : BodyWriter
    {
        byte[] content;
        private object detail;
        public RawBodyWriter(byte[] content)
            : base(true)
        {
            this.content = content;
        }

        public RawBodyWriter(object detail):base(true) {
            // TODO: Complete member initialization
            this.detail = detail;
        }

        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("Binary");
            writer.WriteBase64(content, 0, content.Length);
            writer.WriteEndElement();
        }
    }
}
