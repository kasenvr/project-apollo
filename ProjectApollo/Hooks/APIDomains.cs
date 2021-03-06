﻿//   Copyright 2020 Vircadia
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;

using Project_Apollo.Entities;
using Project_Apollo.Registry;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RandomNameGeneratorLibrary;
using System.Linq;

namespace Project_Apollo.Hooks
{
    public class APIDomains
    {
        public static readonly string _logHeader = "[APIDomains]";

        // ===GET /api/v1/domains =================================================
        public struct bodyDomainsReply
        {
            public bodyDomainInfo[] domains;
        }
        public struct bodyDomainInfo
        {
            public string domainid;
            public string place_name;
            public string public_key;
            public string sponser_accountid;
            public string ice_server;
            public string version;
            public string protocol_version;
            public string network_addr;
            public string networking_mode;
            public bool restricted;
            public int num_users;
            public int anon_users;
            public int total_users;
            public int capacity;
            public string description;
            public string maturity;
            public string restriction;
            public string[] hosts;
            public string[] tags;
            public string time_of_last_heartbeat;
            public string last_sender_key;
            public string addr_of_first_contact;
            public string when_domain_entry_created;

            public bodyDomainInfo(DomainEntity pDomain)
            {
                domainid = pDomain.DomainID;
                place_name = pDomain.PlaceName;
                public_key = pDomain.Public_Key;
                sponser_accountid = pDomain.SponserAccountID;
                ice_server = pDomain.IceServerAddr;
                version = pDomain.Version;
                protocol_version = pDomain.Protocol;
                network_addr = pDomain.NetworkAddr;
                networking_mode = pDomain.NetworkingMode;
                restricted = pDomain.Restricted;
                num_users = pDomain.NumUsers;
                anon_users = pDomain.AnonUsers;
                total_users = pDomain.TotalUsers;
                capacity = pDomain.Capacity;
                description = pDomain.Description;
                maturity = pDomain.Maturity;
                restriction = pDomain.Restriction;
                hosts = pDomain.Hosts;
                tags = pDomain.Tags;
                time_of_last_heartbeat = XmlConvert.ToString(pDomain.TimeOfLastHeartbeat, XmlDateTimeSerializationMode.Utc);
                last_sender_key = pDomain.LastSenderKey;
                addr_of_first_contact = pDomain.IPAddrOfFirstContact;
                when_domain_entry_created = XmlConvert.ToString(pDomain.WhenDomainEntryCreated, XmlDateTimeSerializationMode.Utc);
            }
        }
        [APIPath("/api/v1/domains", "GET", true)]
        public RESTReplyData get_domains(RESTRequestData pReq, List<string> pArgs)
        {
            RESTReplyData replyData = new RESTReplyData();  // The HTTP response info
            ResponseBody respBody = new ResponseBody();     // The request's "data" response info

            if (Accounts.Instance.TryGetAccountWithAuthToken(pReq.AuthToken, out AccountEntity aAccount))
            {
                PaginationInfo pagination = new PaginationInfo(pReq);

                respBody.Data = new bodyDomainsReply()
                {
                    domains = Domains.Instance.Enumerate(pagination).Select(dom =>
                    {
                        return new bodyDomainInfo(dom);
                    }).ToArray()
                };
            }
            else
            {
                respBody.RespondFailure("Unauthorized");
                replyData.Status = (int)HttpStatusCode.Unauthorized;
            }

            replyData.SetBody(respBody, pReq);
            return replyData;
        }

        // ===GET /api/v1/domains/% =================================================

        // The 'domain' response body can contain different items depending
        //    on the request. If it's not set, don't retun that field.
        public struct bodyDomainReplyData
        {
            public string id;
            public string name;
            public string ice_server_address;
            public string api_key;
        }
        public struct bodyDomainResponse
        {
            public bodyDomainReplyData domain;
        }
        [APIPath("/api/v1/domains/%", "GET", true)]
        public RESTReplyData get_domain(RESTRequestData pReq, List<string> pArgs)
        {
            RESTReplyData replyData = new RESTReplyData();  // The HTTP response info
            ResponseBody respBody = new ResponseBody();     // The request's "data" response info

            string domainID = pArgs.Count == 1 ? pArgs[0] : null;
            if (Domains.Instance.TryGetDomainWithID(domainID, out DomainEntity dobj))
            {
                respBody.Data = new bodyDomainResponse()
                {
                    domain = new bodyDomainReplyData()
                    {
                        id = domainID,
                        ice_server_address = dobj.GetIceServerAddr(),
                        name = dobj.PlaceName
                    }
                };
            }
            else
            {
                // return nothing!
                respBody.RespondFailure("No domain with that ID");
                respBody.ErrorData("domain", "there is no domain with that ID");

                replyData.Status = (int)HttpStatusCode.NotFound;
            }

            replyData.SetBody(respBody, pReq);
            return replyData;
        }

        // ===PUT /api/v1/domains/% ==========================================
        // Used by domains as a "heartbeat" to update status information

        /* Not using structures because fields are optional and cannot differentiate non-specified and null.
        public struct domainMetadataUsers
        {
            // User domain metadata
            public int num_users;
            public int num_anon_users;
            public Dictionary<string, int> user_hostnames;
        }
        public struct domainMetadataDescriptors
        {
            public string description;
            public int capacity;
            public string restriction;  // one of "open", "hifi", "acl"
            public string maturity;
            public string[] hosts;
            public string[] tags;
        }
        public struct bodyDomainPutData
        {
            public string version;
            public string protocol;
            public string network_address;
            public string automatic_networking;
            public bool restricted;
            public string api_key;

            public domainMetadataUsers heartbeat;
        }
        // Domains call has differing bodies but they all start with a 'Domain' top level
        public struct bodyHeartbeatRequest
        {
            public bodyDomainPutData domain;
        }
        */
        [APIPath("/api/v1/domains/%", "PUT", true)]
        public RESTReplyData domain_heartbeat(RESTRequestData pReq, List<string> pArgs)
        {
            RESTReplyData replyData = new RESTReplyData();  // The HTTP response info
            ResponseBody respBody = new ResponseBody();     // The request's "data" response info

            SessionEntity aSession = Sessions.Instance.UpdateSession(pReq.SenderKey);

            string domainID = pArgs.Count == 1 ? pArgs[0] : null;
            if (Domains.Instance.TryGetDomainWithID(domainID, out DomainEntity aDomain))
            {
                try
                {
                    // Since the body has a lot of optional fields, we need to pick
                    //    through what's sent so we only set what was sent.
                    JObject requestData = pReq.RequestBodyJSON();
                    JObject domainStuff = (JObject)requestData["domain"];
                    string apiKey = (string)domainStuff["api_key"];

                    // Context.Log.Debug("{0} domain_heartbeat. Received {1}", _logHeader, pReq.RequestBody);
                    if (VerifyDomainAccess(aDomain, pReq, apiKey, out string oFailureReason))
                    {
                        Tools.SetIfSpecified<string>(domainStuff, "version", ref aDomain.Version);
                        Tools.SetIfSpecified<string>(domainStuff, "protocol", ref aDomain.Protocol);
                        Tools.SetIfSpecified<string>(domainStuff, "network_address", ref aDomain.NetworkAddr);
                        Tools.SetIfSpecified<string>(domainStuff, "automatic_networking", ref aDomain.NetworkingMode);
                        Tools.SetIfSpecified<bool>(domainStuff, "restricted", ref aDomain.Restricted);

                        Tools.SetIfSpecified<int>(domainStuff, "capacity", ref aDomain.Capacity);
                        Tools.SetIfSpecified<string>(domainStuff, "description", ref aDomain.Description);
                        Tools.SetIfSpecified<string>(domainStuff, "maturity", ref aDomain.Maturity);
                        Tools.SetIfSpecified<string>(domainStuff, "restriction", ref aDomain.Restriction);
                        JArray hosts = (JArray)domainStuff["hosts"];
                        if (hosts != null)
                        {
                            aDomain.Hosts = domainStuff["hosts"].ToObject<string[]>();
                        }
                        JArray tags = (JArray)domainStuff["tags"];
                        if (tags != null)
                        {
                            aDomain.Tags = domainStuff["tags"].ToObject<string[]>();
                        }

                        JObject heartbeat = (JObject)domainStuff["heartbeat"];
                        if (heartbeat != null)
                        {
                            Tools.SetIfSpecified<int>(heartbeat, "num_users", ref aDomain.NumUsers);
                            Tools.SetIfSpecified<int>(heartbeat, "num_anon_users", ref aDomain.AnonUsers);
                            aDomain.TotalUsers = aDomain.NumUsers + aDomain.AnonUsers;
                            // TODO: what to do with user_hostnames
                        }

                        aDomain.TimeOfLastHeartbeat = DateTime.UtcNow;
                        aDomain.LastSenderKey = pReq.SenderKey;

                        aDomain.Updated();
                    }
                    else
                    {
                        Context.Log.Error("{0} domain_heartbeat with bad authorization. domainID={1}",
                                                _logHeader, domainID);
                        respBody.RespondFailure(oFailureReason);
                        replyData.Status = (int)HttpStatusCode.Unauthorized;
                    }
                }
                catch (Exception e)
                {
                    Context.Log.Error("{0} domain_heartbeat: Exception processing body: {1}", _logHeader, e.ToString());
                    respBody.RespondFailure("exception processing body", e.ToString());
                    replyData.Status = (int)HttpStatusCode.BadRequest;
                }
            }
            else
            {
                Context.Log.Error("{0} domain_heartbeat: unknown domain. Returning 404", _logHeader);
                respBody.RespondFailure("no such domain");
                replyData.Status = (int)HttpStatusCode.NotFound; // this will trigger a new temporary domain name
            }

            replyData.SetBody(respBody, pReq);
            return replyData;
        }

        /// <summary>
        /// A domain is either using an apikey (if not registered with a user account) or is
        /// using a user authtoken (if registered). This routine checks whether this
        /// domain request includes the right stuff for now.
        /// </summary>
        /// <param name="pDomain"></param>
        /// <param name="pReq"></param>
        /// <returns></returns>
        private bool VerifyDomainAccess(DomainEntity pDomain, RESTRequestData pReq, string pPossibleApiKey, out string oFailureReason)
        {
            bool ret = false;
            string failure = null;
            if (String.IsNullOrEmpty(pReq.AuthToken))
            {
                // No auth token. See if there is an APIKey and use that 
                if (!String.IsNullOrEmpty(pDomain.API_Key))
                {
                    ret = pDomain.API_Key.Equals(pPossibleApiKey);
                    if (!ret)
                    {
                        failure = "API key does not match";
                    }
                }
            }
            else
            {
                // If the domain passed an auth token, get access with that
                if (Accounts.Instance.TryGetAccountWithAuthToken(pReq.AuthToken, out AccountEntity oAccount, AuthTokenInfo.ScopeCode.domain))
                {
                    // Make sure this domain and this account are working together
                    if (String.IsNullOrEmpty(pDomain.SponserAccountID))
                    {
                        // The domain doesn't have a sponser so assign
                        pDomain.SponserAccountID = oAccount.AccountID;
                        ret = true;
                    }
                    else
                    {
                        // There is a sponser for this domain. Make sure it stays the same
                        if (pDomain.SponserAccountID.Equals(oAccount.AccountID))
                        {
                            ret = true;
                        }
                        else
                        {
                            Context.Log.Error("{0} Domain used authtoken from different acct. DomainID={1}, SponserID={2}, newAcct={3}",
                                    _logHeader, pDomain.DomainID, pDomain.SponserAccountID, oAccount.AccountID);
                            ret = false;
                            failure = "Domain auth token does not match with sponsering account";
                        };
                    };
                };
            };
            if (!ret)
            {
                Context.Log.Debug("{0} VerifyDomainAccess: failed auth. DomainID={1}, domain.apikey={2}, AuthToken={3}, apikey={4}",
                                    _logHeader, pDomain.DomainID, pDomain.API_Key, pReq.AuthToken, pPossibleApiKey);
                if (String.IsNullOrEmpty(failure))
                {
                    failure = "Unauthorized";
                }
            }
            oFailureReason = failure;
            return ret;
        }

        // == DELETE /api/v1/domains/% ===========================================
        [APIPath("/api/v1/domains/%", "DELETE", true)]
        public RESTReplyData admin_domain_delete(RESTRequestData pReq, List<string> pArgs)
        {
            RESTReplyData replyData = new RESTReplyData();  // The HTTP response info
            ResponseBody respBody = new ResponseBody();     // The request's "data" response info

            if (Accounts.Instance.TryGetAccountWithAuthToken(pReq.AuthToken, out AccountEntity aAccount))
            {
                if (aAccount.IsAdmin)
                {
                    string domainID = pArgs.Count == 1 ? pArgs[0] : null;
                    if (Domains.Instance.TryGetDomainWithID(domainID, out DomainEntity aDomain))
                    {
                        Domains.Instance.RemoveDomain(aDomain);
                    }
                    else
                    {
                        respBody.RespondFailure("No such domain");
                    }
                }
                else
                {
                    respBody.RespondFailure("Requesting account is not an administrator");
                    replyData.Status = (int)HttpStatusCode.Unauthorized;
                }

            }
            else
            {
                respBody.RespondFailure("Authorization token not found");
                replyData.Status = (int)HttpStatusCode.Unauthorized;
            }
            replyData.SetBody(respBody, pReq);
            return replyData;
        }

        // == PUT /api/v1/domains/%/ice_server_address ===========================
        public struct bodyIceServerPutData
        {
            public string ice_server_address;
            public string api_key;
        }
        public struct bodyIceServerPut
        {
            public bodyIceServerPutData domain;
        }
        public struct bodyIceServerPutResponse
        {
            public string id;           // returning the domainID
            public string ice_server_address;   // returning the address set
            public string name;         // place name
        }

        [APIPath("/api/v1/domains/%/ice_server_address", "PUT", true)]
        public RESTReplyData put_ice_address(RESTRequestData pReq, List<string> pArgs)
        {
            RESTReplyData replyData = new RESTReplyData();  // The HTTP response info
            ResponseBody respBody = new ResponseBody();     // The request's "data" response info

            string domainID = pArgs.Count == 1 ? pArgs[0] : null;
            if (Domains.Instance.TryGetDomainWithID(domainID, out DomainEntity aDomain))
            {
                // Context.Log.Debug("{0} domains/ice_server_addr PUT. Body={1}", _logHeader, pReq.RequestBody);
                bodyIceServerPut isr = pReq.RequestBodyObject<bodyIceServerPut>();
                string includeAPIKey = isr.domain.api_key;
                if (VerifyDomainAccess(aDomain, pReq, includeAPIKey, out string oFailureReason))
                {
                    aDomain.IceServerAddr = isr.domain.ice_server_address;
                }
                else
                {
                    Context.Log.Error("{0} PUT domains/%/ice_server_address not authorized. DomainID={1}",
                                        _logHeader, domainID);
                    respBody.RespondFailure(oFailureReason);
                    replyData.Status = (int)HttpStatusCode.Unauthorized;
                }
            }
            else
            {
                respBody.RespondFailure("No such domain");
                replyData.Status = (int)HttpStatusCode.NotFound;
            }
            return replyData;
        }

        // == POST /api/v1/domains/temporary =====================================
        [APIPath("/api/v1/domains/temporary", "POST", true)]
        public RESTReplyData get_temporary_name(RESTRequestData pReq, List<string> pArgs)
        {
            RESTReplyData replyData = new RESTReplyData();
            ResponseBody respBody = new ResponseBody();

            SessionEntity aSession = Sessions.Instance.UpdateSession(pReq.SenderKey);

            PersonNameGenerator png = new PersonNameGenerator();
            PlaceNameGenerator plng = new PlaceNameGenerator();

            DomainEntity newDomain = new DomainEntity()
            {
                PlaceName = png.GenerateRandomFirstName() + "-" + plng.GenerateRandomPlaceName() + "-" + new Random().Next(500, 9000).ToString(),
                DomainID = Guid.NewGuid().ToString(),
                IPAddrOfFirstContact = pReq.RemoteUser.ToString()
            };
            newDomain.API_Key = Tools.MD5Hash($":{newDomain.PlaceName}::{newDomain.DomainID}:{newDomain.IceServerAddr}");
            Domains.Instance.AddDomain(newDomain.DomainID, newDomain);

            respBody.Data = new bodyDomainResponse()
            {
                domain = new bodyDomainReplyData()
                {
                    id = newDomain.DomainID,
                    name = newDomain.PlaceName,
                    api_key = newDomain.API_Key
                }
            };
            replyData.SetBody(respBody, pReq);
            Context.Log.Debug("{0} Returning temporary domain: id={1}, name={2}",
                        _logHeader, newDomain.DomainID, newDomain.PlaceName);
            
            replyData.CustomOutputHeaders.Add("X-Rack-CORS", "miss; no-origin");
            replyData.CustomOutputHeaders.Add("Access-Control-Allow-Origin", "*");
            return replyData;
        }

        // == PUT /api/v1/domains/%/public_key ===================================
        [APIPath("/api/v1/domains/%/public_key", "PUT", true)]

        public RESTReplyData set_public_key(RESTRequestData pReq, List<string> pArgs)
        {
            RESTReplyData replyData = new RESTReplyData();  // The HTTP response info
            ResponseBody respBody = new ResponseBody();     // The request's "data" response info

            SessionEntity aSession = Sessions.Instance.UpdateSession(pReq.SenderKey);

            string domainID = pArgs.Count == 1 ? pArgs[0] : null;
            if (Domains.Instance.TryGetDomainWithID(domainID, out DomainEntity aDomain))
            {
                try
                {
                    string includedAPIKey = pReq.RequestBodyMultipart("api_key");
                    if (VerifyDomainAccess(aDomain, pReq, includedAPIKey, out string oFailureReason))
                    {
                        // The PUT sends the key as binary but it is later sent around
                        //    and processed as a Base64 string.
                        Stream byteStream = pReq.RequestBodyMultipartStream("public_key");
                        if (byteStream != null)
                        {
                            aDomain.Public_Key = Tools.ConvertPublicKeyStreamToBase64(byteStream);
                            aDomain.Updated();
                            // Context.Log.Debug("{0} successful set of public_key for {1}", _logHeader, domainID);
                        }
                        else
                        {
                            Context.Log.Error("{0} could not extract public key from request body: domain {1}",
                                                _logHeader, domainID);
                            replyData.Status = (int)HttpStatusCode.Unauthorized;
                            respBody.RespondFailure("Could not extract public key from request body");
                        }
                    }
                    else
                    {
                        Context.Log.Error("{0} attempt to set public_key when no authorized: domain {1}",
                                            _logHeader, domainID);
                        replyData.Status = (int)HttpStatusCode.Unauthorized;
                        respBody.RespondFailure(oFailureReason);
                    }
                }
                catch (Exception e)
                {
                    Context.Log.Error("{0} exception parsing multi-part http: {1}", _logHeader, e.ToString());
                    respBody.RespondFailure("Exception parsing multi-part http", e.ToString());
                }

            }
            else
            {
                Context.Log.Error("{0} attempt to set public_key for unknown domain {1}", _logHeader, domainID);
                respBody.RespondFailure("unknown domain");
            }

            replyData.SetBody(respBody, pReq);
            return replyData;
        }

        // == GET /api/v1/domains/%/public_key ===================================
        public struct bodyDomainPublicKeyGetResponse
        {
            public string public_key;
        }
        [APIPath("/api/v1/domains/%/public_key", "GET", true)]
        public RESTReplyData get_public_key(RESTRequestData pReq, List<string> pArgs)
        {
            RESTReplyData replyData = new RESTReplyData();  // The HTTP response info
            ResponseBody respBody = new ResponseBody();     // The request's "data" response info

            string domainID = pArgs.Count == 1 ? pArgs[0] : null;
            if (Domains.Instance.TryGetDomainWithID(domainID, out DomainEntity aDomain))
            {
                respBody.Data = new bodyDomainPublicKeyGetResponse()
                {
                    public_key = aDomain.Public_Key
                };
            }
            else
            {
                // return nothing!
                respBody.RespondFailure("unknown domain");
                respBody.ErrorData("domain", "there is no domain with that ID");    // legacy HiFi compatibility
                replyData.Status = (int)HttpStatusCode.NotFound;
                // Context.Log.Debug("{0} get_domain GET: no domain! Returning: {1}", _logHeader, replyData.Body);
            }

            replyData.SetBody(respBody, pReq);
            return replyData;
        }
    }
}
