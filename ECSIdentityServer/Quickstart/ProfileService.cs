using IdentityServer4.Models;
using IdentityServer4.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECSWebDashboard.Quickstart
{
    public class ProfileService : IProfileService
    {
        public ProfileService() // Can dependency inject services here
        {
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            Console.WriteLine(context.Caller);
            List<string> claims = new List<string>(context.RequestedClaimTypes);
            claims.AddRange(new string[] {
                "discord:username",
                "discord:discriminator",
                "discord:id",
                "discord:avatar",
                "discord:mfa_enabled"
            });
            context.RequestedClaimTypes = claims;
            
            context.AddRequestedClaims(context.Subject.Claims);

            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            //context.IsActive = user.IsActive;
            return Task.CompletedTask;
        }
    }
}
