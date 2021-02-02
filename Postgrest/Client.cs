﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Postgrest.Attributes;
using Postgrest.Models;
using Postgrest.Responses;

namespace Postgrest
{
    /// <summary>
    /// A Singleton that represents a single, reusable connection to a Postgrest endpoint. Should be first called with the `Initialize()` method.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// API Base Url for subsequent calls.
        /// </summary>
        public string BaseUrl { get; private set; }

        private ClientOptions options;

        private static Client instance;
        /// <summary>
        /// Returns the Singleton Instance of this Class.
        /// </summary>
        public static Client Instance
        {
            get
            {
                if (instance == null)
                    instance = new Client();

                return instance;
            }
        }

        private Client() { }

        /// <summary>
        /// Should be the first call to this class to initialize a connection with a Postgrest API Server
        /// </summary>
        /// <param name="baseUrl">Api Endpoint (ex: "http://localhost:8000"), no trailing slash required.</param>
        /// <param name="authorization">Authorization Information.</param>
        /// <param name="options">Optional client configuration.</param>
        /// <returns></returns>
        public static Client Initialize(string baseUrl, ClientOptions options = null)
        {
            instance = new Client();
            instance.BaseUrl = baseUrl;

            if (options == null)
                options = new ClientOptions();

            instance.options = options;

            return instance;
        }

        /// <summary>
        /// Custom Serializer resolvers and converters that will be used for encoding and decoding Postgrest JSON responses.
        ///
        /// By default, Postgrest seems to use a date format that C# and Newtonsoft do not like, so this initial
        /// configuration handles that.
        /// </summary>
        public JsonSerializerSettings SerializerSettings
        {
            get
            {
                return new JsonSerializerSettings
                {
                    ContractResolver = new CustomContractResolver(),
                    Converters =
                    {
                        // 2020-08-28T12:01:54.763231
                        new IsoDateTimeConverter
                        {
                            DateTimeStyles = options.DateTimeStyles,
                            DateTimeFormat = options.DateTimeFormat
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Returns a Table Query Builder instance for a defined model - representative of `USE $TABLE`
        /// </summary>
        /// <typeparam name="T">Custom Model derived from `BaseModel`</typeparam>
        /// <returns></returns>
        public Table<T> Table<T>() where T : BaseModel, new() => new Table<T>(BaseUrl, options);

        /// <summary>
        /// Perform a stored procedure call.
        /// </summary>
        /// <param name="procedureName">The function name to call</param>
        /// <param name="parameters">The parameters to pass to the function call</param>
        /// <returns></returns>
        public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters)
        {
            // Build Uri
            var builder = new UriBuilder($"{BaseUrl}/rpc/{procedureName}");

            var canonicalUri = builder.Uri.ToString();

            // Prepare parameters
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(parameters, Client.Instance.SerializerSettings));
            // Prepare headers
            var headers = Helpers.PrepareRequestHeaders(HttpMethod.Post, new Dictionary<string, string>(options.Headers), options);
            // Send request
            var request = Helpers.MakeRequest(HttpMethod.Post, canonicalUri, data, headers);
            return request;
        }
    }
}
