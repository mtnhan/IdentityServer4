﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features.Authentication;
using IdentityServer4.Extensions;

namespace IdentityServer4.Services.Default
{
    public class DefaultSessionIdService : ISessionIdService
    {
        private readonly HttpContext _context;

        public DefaultSessionIdService(IHttpContextAccessor context)
        {
            _context = context.HttpContext;
        }

        public void CreateSessionId(SignInContext context)
        {
            var sid = CryptoRandom.CreateUniqueId();
            context.Properties[OidcConstants.EndSessionRequest.Sid] = sid;
            IssueSessionIdCookie(sid);
        }

        public async Task<string> GetCurrentSessionIdAsync()
        {
            var info = await _context.GetIdentityServerUserInfoAsync();
            if (info.Properties.Items.ContainsKey(OidcConstants.EndSessionRequest.Sid))
            {
                var sid = info.Properties.Items[OidcConstants.EndSessionRequest.Sid];
                return sid;
            }
            return null;
        }

        public async Task EnsureSessionCookieAsync()
        {
            var sid = await GetCurrentSessionIdAsync();
            if (sid != null)
            {
                IssueSessionIdCookie(sid);
            }
            else
            {
                // we don't want to delete the session id cookie if the user is
                // no longer authenticated since we might be waiting for the 
                // signout iframe to render -- it's a timing issue between the 
                // logout page removing the authentication cookie and the 
                // signout iframe callback from performing SLO
            }
        }

        public string GetCookieName()
        {
            // TODO: fix from config?
            return "idsvr.session";
        }

        public string GetCookieValue()
        {
            return _context.Request.Cookies[GetCookieName()];
        }

        public void RemoveCookie()
        {
            var name = GetCookieName();
            if (_context.Request.Cookies.ContainsKey(name))
            {
                // only remove it if we have it in the request
                var options = CreateCookieOptions();
                options.Expires = DateTimeHelper.UtcNow.AddYears(-1);

                _context.Response.Cookies.Append(name, ".", options);
            }
        }

        void IssueSessionIdCookie(string sid)
        {
            if (GetCookieValue() != sid)
            {
                _context.Response.Cookies.Append(
                    GetCookieName(),
                    sid,
                    CreateCookieOptions());
            }
        }

        CookieOptions CreateCookieOptions()
        {
            var secure = _context.Request.IsHttps;
            var path = _context.GetBasePath().CleanUrlPath();

            var options = new CookieOptions
            {
                HttpOnly = false,
                Secure = secure,
                Path = path
            };

            return options;
        }
    }
}