using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QuickLearn.LogicApps
{
    public class Callback<TConfiguration>
    {

        /// <summary>
        /// Gets or sets the configuration for the push trigger callback as provided in the Logic Apps designer
        /// </summary>
        public TConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the callback URI with inline credentials
        /// </summary>
        public Uri UriWithCredentials { get; set; }

        /// <summary>
        /// Gets the raw callback URI (without inline credentials)
        /// </summary>
        public Uri RawUri
        {
            get
            {
                return new Uri(UriWithCredentials.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.Unescaped));
            }
        }

        /// <summary>
        /// Gets the user name that was parsed out of the callback URI
        /// </summary>
        public string UserName
        {
            get
            {
                return string.IsNullOrWhiteSpace(UriWithCredentials.UserInfo) ? null :
                                UriWithCredentials.UserInfo.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }

        /// <summary>
        /// Gets the password that was parsed out of the callback URI
        /// </summary>
        public string Password
        {
            get
            {
                return string.IsNullOrWhiteSpace(UriWithCredentials.UserInfo) ? null :
                                UriWithCredentials.UserInfo.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
                                .Skip(1)
                                .Aggregate((s, p) => string.Concat(s, p));
            }
        }

        private string authorizationHeader
        {
            get
            {
                return !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password)
                                ? string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", UserName, Password))))
                                : string.Empty;
            }
        }

        /// <summary>
        /// Creates a new instance of the callback class
        /// </summary>
        /// <remarks>
        /// You will need to set the UriWithCredentials if you are using this constructor
        /// </remarks>
        public Callback() { }

        /// <summary>
        /// Creates a new instance of the callback class using a provided URI with inline credentials
        /// </summary>
        /// <param name="uriWithCredentials">URI with inline credentials as provided by the interested Logic App</param>
        public Callback(Uri uriWithCredentials)
            : this()
        {
            this.UriWithCredentials = uriWithCredentials;
        }

        /// <summary>
        /// Creates a new instance of the callback class using a provided URI
        /// </summary>
        /// <param name="uriWithCredentials">URI with inline credentials as provided by the interested Logic App</param>
        /// <param name="configuration">Configuration for the push trigger callback as specified in the card for the push trigger in the Logic App designer</param>
        public Callback(Uri uriWithCredentials, TConfiguration configuration)
            : this(uriWithCredentials)
        {
            Configuration = configuration;
        }

        private async Task<HttpResponseMessage> invokeAsyncImplementation(JObject outputs)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
                client.DefaultRequestHeaders.Add("Authorization", authorizationHeader);



            return await client.PostAsJsonAsync<JObject>(RawUri, outputs);
        }

        /// <summary>
        /// Invokes the callback without providing any specific trigger outputs
        /// </summary>
        /// <returns>Returns the HttpResponseMessage received from the server after invoking the callback</returns>
        public async Task<HttpResponseMessage> InvokeAsync()
        {
            JObject outputs = new JObject(
                new JProperty("outputs")
            );

            return await invokeAsyncImplementation(outputs);
        }

        /// <summary>
        /// Invokes the callback providing a trigger output of the specified type, so that this data can be used throughout the Logic App definition by accessing @triggers.outputs.body
        /// </summary>
        /// <typeparam name="TOutput">Type of the trigger output that will serve as an input to the Logic App</typeparam>
        /// <param name="triggerOutput">Output of the trigger that will serve as an input to the Logic App</param>
        /// <returns>Returns the HttpResponseMessage received from the server after invoking the callback</returns>
        public async Task<HttpResponseMessage> InvokeAsync<TOutput>(TOutput triggerOutput)
        {
            JObject outputs = new JObject(
                new JProperty("outputs",
                    new JObject(
                        new JProperty("body",
                                      JToken.FromObject(triggerOutput)
                        )
                    )
                )
            );

            return await invokeAsyncImplementation(outputs);
        }
    }
}
