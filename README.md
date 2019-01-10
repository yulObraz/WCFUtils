# WCFUtils - WCF serialization using newtonsoft json.

Based on https://github.com/carlosfigueira/WCFSamples/tree/master/MessageFormatter/MessageFormatter

Replaces WCF serialization to allow use at the same time SOAP and json interfaces. 
So wsdl can be used to generate json client.
With this implementation could be replaced .net client or server part. 
Added extension point for settings for different capitalization styles.

Service interface requires attributes with implicit limitations (I suppose it were possible to avoid them). But sample shows workable methods.

## License
The original code samplesat https://code.msdn.microsoft.com/site/search?f%5B0%5D.Type=User&f%5B0%5D.Value=CarlosFigueira are published with Apache 2.0 licence. I don't want to add any limitations. 
Based on https://github.com/carlosfigueira/WCFSamples/tree/master/MessageFormatter/MessageFormatter
