﻿// -----------------------------------------------------------------------
//  <copyright file="AdminDatabases.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Raven.Server.Json;
using Raven.Server.Routing;

namespace Raven.Server.Web.System
{
    public unsafe class AdminDatabases : RequestHandler
    {
        private readonly RequestHandlerContext _requestHandlerContext;

        public AdminDatabases(RequestHandlerContext requestHandlerContext)
        {
            _requestHandlerContext = requestHandlerContext;
        }

        [Route("/admin/databases/$", "GET")]
        public Task Get()
        {
            RavenOperationContext context;
            using (_requestHandlerContext.ServerStore.ContextPool.AllocateOperationContext(out context))
            {
                context.Transaction = context.Environment.ReadTransaction();

                var id = _requestHandlerContext.RouteMatch.Url.Substring(_requestHandlerContext.RouteMatch.MatchLength);
                var dbId = "db/" + id;
                var dbDoc = _requestHandlerContext.ServerStore.Read(context, dbId);
                if (dbDoc == null)
                {
                    _requestHandlerContext.HttpContext.Response.StatusCode = 404;
                    return _requestHandlerContext.HttpContext.Response.WriteAsync("Database " + id + " wasn't found");
                }

                UnprotectSecuredSettingsOfDatabaseDocument(dbDoc);

                _requestHandlerContext.HttpContext.Response.StatusCode = 200;
                _requestHandlerContext.HttpContext.Response.Headers["ETag"] = "TODO: Please implement this: " + Guid.NewGuid(); // TODO (fitzchak)
                dbDoc.WriteTo(_requestHandlerContext.HttpContext.Response.Body);
                return Task.CompletedTask;
            }
        }

        private void UnprotectSecuredSettingsOfDatabaseDocument(BlittableJsonReaderObject obj)
        {
            object securedSettings;
            if (obj.TryGetMember("SecuredSettings", out securedSettings) == false)
            {
                
            }
        }

        [Route("/admin/databases/$", "PUT")]
        public Task Put()
        {
            var id = _requestHandlerContext.RouteMatch.Url.Substring(_requestHandlerContext.RouteMatch.MatchLength);

            string errorMessage;
            if (ResourceNameValidator.IsValidResourceName(id, _requestHandlerContext.ServerStore.Configuration.Core.DataDirectory, out errorMessage) == false)
            {
                _requestHandlerContext.HttpContext.Response.StatusCode = 400;
                return _requestHandlerContext.HttpContext.Response.WriteAsync(errorMessage);
            }

            RavenOperationContext context;
            using (_requestHandlerContext.ServerStore.ContextPool.AllocateOperationContext(out context))
            {
                context.Transaction = context.Environment.WriteTransaction();
                var dbId = "db/" + id;

                var etag = _requestHandlerContext.HttpContext.Request.Headers["ETag"];
                if (CheckExistingDatabaseName(context, id, dbId, etag, out errorMessage) == false)
                {
                    _requestHandlerContext.HttpContext.Response.StatusCode = 400;
                    return _requestHandlerContext.HttpContext.Response.WriteAsync(errorMessage);
                }

                var dbDoc = context.Read(_requestHandlerContext.HttpContext.Request.Body, dbId);
                //int size;
                //var buffer = context.GetNativeTempBuffer(dbDoc.SizeInBytes, out size);
                //dbDoc.CopyTo(buffer);

                //var reader = new BlittableJsonReaderObject(buffer, dbDoc.SizeInBytes, context);
                //object result;
                //if (reader.TryGetMember("SecureSettings", out result))
                //{
                //    var secureSettings = (BlittableJsonReaderObject) result;
                //    secureSettings.Modifications = new DynamicJsonValue(secureSettings);
                //    foreach (var propertyName in secureSettings.GetPropertyNames())
                //    {
                //        secureSettings.TryGetMember(propertyName, out result);
                //        // protect
                //        secureSettings.Modifications[propertyName] = "fooo";
                //    }
                //}


                _requestHandlerContext.ServerStore.Write(dbId, dbDoc);
                _requestHandlerContext.HttpContext.Response.StatusCode = 201;
                return Task.CompletedTask;
            }
        }

        private bool CheckExistingDatabaseName(RavenOperationContext context, string id, string dbId, string etag, out string errorMessage)
        {
            var database = _requestHandlerContext.ServerStore.Read(context, dbId);
            var isExistingDatabase = database != null;

            if (isExistingDatabase && etag == null)
            {
                errorMessage = $"Database with the name '{id}' already exists";
                return false;
            }
            if (!isExistingDatabase && etag != null)
            {
                errorMessage = $"Database with the name '{id}' doesn't exist";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}