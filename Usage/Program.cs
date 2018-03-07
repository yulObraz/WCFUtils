using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace WCFUtils.Usage
{
    class Program
    {
        public static string SendRequest(string uri, string method, string contentType, string body, bool log = true)
        {
            string responseBody = null;

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Method = method;
            if (!String.IsNullOrEmpty(contentType))
            {
                req.ContentType = contentType;
            }
            if (body != null)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                req.GetRequestStream().Write(bodyBytes, 0, bodyBytes.Length);
                req.GetRequestStream().Close();
            }

            HttpWebResponse resp;
            try
            {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException e)
            {
                resp = (HttpWebResponse)e.Response;
            }
            Console.WriteLine("{0} {1} HTTP/{2} {3} {4}", method, uri, resp.ProtocolVersion, (int)resp.StatusCode, resp.StatusDescription);
            foreach (string headerName in resp.Headers.AllKeys)
            {
                Console.WriteLine("{0}: {1}", headerName, resp.Headers[headerName]);
            }
            Console.WriteLine();
            Stream respStream = resp.GetResponseStream();
            if (respStream != null)
            {
                responseBody = new StreamReader(respStream).ReadToEnd();
                if(log) {
                    Console.WriteLine(responseBody);
                }
            }
            else
            {
                Console.WriteLine("HttpWebResponse.GetResponseStream returned null");
            }
            Console.WriteLine();
            Console.WriteLine("  *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*  ");
            Console.WriteLine();

            return responseBody;
        }

        class MyRawMapper : WebContentTypeMapper
        {
            public override WebContentFormat GetMessageFormatForContentType(string contentType)
            {
                return WebContentFormat.Raw;
            }
        }

        static void Main(string[] args) {
            //netsh http add urlacl url=http://+:8080/ user=\Everyone
            //string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            string baseAddress = "http://localhost:8000/Service";
            ServiceHost host = new NewtonsoftJsonServiceHost(typeof(Service), true, new Uri(baseAddress));
            Console.WriteLine("Opening the host");
            host.Open();

            SendRequest(baseAddress + "/json/GetPerson", "GET", null, null);
            SendRequest(baseAddress + "/json/EchoPet", "POST", "application/json", "{\"name\":\"Fido\",\"color\":\"Black and white\",\"markings\":\"None\",\"id\":1}");
            SendRequest(baseAddress + "/json/Add", "POST", "application/json", "{\"x\":111,\"z\":null,\"w\":[1,2],\"v\":{\"a\":1},\"y\":222}");
            SendRequest(baseAddress + "/json/Add?x=15&y=20", "GET", null, null);

            try {
                SendRequest(baseAddress + "/json/Throw", "GET", null, null);
            } catch(Exception ex) {
                Console.WriteLine(ex);
            }
            Console.WriteLine("Now using the client formatter");
            ChannelFactory<ITestService> newFactory = NewtonsoftJsonServiceHost.GetClientFactory<ITestService>(new Uri(baseAddress), null, null/*"anonymous", "anonymous"*/, c => {
                var ser = c.NewtonsoftSettings().JsonSerializer;
                ser.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter());
                ser.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
            });

            ITestService newProxy = newFactory.CreateChannel();
            Console.WriteLine(newProxy.GetPerson());
            Console.WriteLine(newProxy.AddGet(444, 555));
            Console.WriteLine(newProxy.EchoPet(new Pet { Color = "gold", Id = 2, Markings = "Collie", Name = "Lassie", BirthDay = DateTime.UtcNow.AddMonths(-4) }));
            try {
                newProxy.Throw(1);
            } catch(FaultException<CommonFault> fault) {
                Console.WriteLine(fault.Detail.Error);
            }
            SendRequest(baseAddress + "/json/GetPerson", "GET", null, null);//once more
            
            //Console.WriteLine("Press ENTER to close");
            //Console.ReadLine();
            host.Close();

            Console.WriteLine("Host closed");
            Console.WriteLine("Host2 opened");

            host = new TestServiceHost(typeof(Service), true, new Uri(baseAddress));
            Console.WriteLine("Opening the host2");
            host.Open();

            SendRequest(baseAddress + "/json/GetPerson", "GET", null, null);//different settings


            //Console.WriteLine("Press ENTER to close");
            //Console.ReadLine();
            host.Close();
            Console.WriteLine("Host closed");

            ServiceHost host2 = new ServiceHost(typeof(Service), new Uri(baseAddress + "/soap"));
            var ssss = host2.AddServiceEndpoint(typeof(ITestService), new BasicHttpBinding(), "soap");
            var serv2 = new System.ServiceModel.Description.ServiceMetadataBehavior() { };
            serv2.HttpGetEnabled = true;
            serv2.MetadataExporter.PolicyVersion = System.ServiceModel.Description.PolicyVersion.Policy15;
            host2.Description.Behaviors.Add(serv2);
            var mex = host2.AddServiceEndpoint(System.ServiceModel.Description.ServiceMetadataBehavior.MexContractName, System.ServiceModel.Description.MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
            host2.Open();
            var result = SendRequest(baseAddress + "/soap?singlewsdl", "GET", null, null, false);
            result = result.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<?xml-stylesheet type=\"text/xsl\" href=\"wsdl-viewer.xsl\"?>\n");
            File.WriteAllText("../../Metadata.xml", result);

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host2.Close();
            Console.WriteLine("Host closed");
        }
    }
}
