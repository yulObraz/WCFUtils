using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;

namespace WCFUtils {
    public class RawContentTypeMapper : WebContentTypeMapper {
        static readonly RawContentTypeMapper instance = new RawContentTypeMapper();

        public static RawContentTypeMapper Instance {
            get {
                return instance;
            }
        }

        public override WebContentFormat GetMessageFormatForContentType(string contentType) {
            return WebContentFormat.Raw;
        }
    }
}
