﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Raven.Client.Documents.Conventions;
using Raven.Client.Http;
using Raven.Client.Json;
using Sparrow.Json;

namespace Raven.Client.ServerWide.Operations.Certificates
{
    public class PutClientCertificateOperation : IServerOperation
    {
        private readonly X509Certificate2 _certificate;
        private readonly Dictionary<string, DatabaseAccess> _permissions;
        private readonly bool _serverAdmin;

        public PutClientCertificateOperation(X509Certificate2 certificate, Dictionary<string, DatabaseAccess> permissions, bool serverAdmin = false)
        {
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            _serverAdmin = serverAdmin;
        }

        public RavenCommand GetCommand(DocumentConventions conventions, JsonOperationContext context)
        {
            return new PutClientCertificateCommand(_certificate, _permissions, _serverAdmin);
        }

        private class PutClientCertificateCommand : RavenCommand
        {
            private readonly X509Certificate2 _certificate;
            private readonly Dictionary<string, DatabaseAccess> _permissions;
            private readonly bool _serverAdmin;

            public PutClientCertificateCommand(X509Certificate2 certificate, Dictionary<string, DatabaseAccess> permissions, bool serverAdmin = false)
            {
                _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
                _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
                _serverAdmin = serverAdmin;
            }

            public override bool IsReadRequest => false;

            public override HttpRequestMessage CreateRequest(JsonOperationContext ctx, ServerNode node, out string url)
            {
                url = $"{node.Url}/admin/certificates";

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    Content = new BlittableJsonContent(stream =>
                    {
                        using (var writer = new BlittableJsonTextWriter(ctx, stream))
                        {
                            writer.WriteStartObject();

                            writer.WritePropertyName("Certificate");
                            writer.WriteString(Convert.ToBase64String(_certificate.Export(X509ContentType.Cert)));
                            writer.WriteComma();
                            writer.WritePropertyName("ServerAdmin");
                            writer.WriteBool(_serverAdmin);
                            writer.WriteComma();

                            writer.WritePropertyName("Permissions");
                            writer.WriteStartArray();
                            bool first = true;
                            foreach (var kvp in _permissions)
                            {
                                if (first == false)
                                    writer.WriteComma();
                                first = false;

                                writer.WriteStartObject();
                                writer.WritePropertyName("Database");
                                writer.WriteString(kvp.Key);
                                writer.WriteComma();
                                writer.WritePropertyName("Access");
                                writer.WriteString(kvp.Value == DatabaseAccess.ReadWrite ? nameof(DatabaseAccess.ReadWrite) : nameof(DatabaseAccess.Admin));
                                writer.WriteEndObject();
                            }

                            writer.WriteEndArray();
                            writer.WriteEndObject();
                        }
                    })
                };

                return request;
            }
        }
    }
}