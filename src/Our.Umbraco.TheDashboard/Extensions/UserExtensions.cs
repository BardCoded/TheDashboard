using System.Security.Cryptography;
using System.Text;
using Our.Umbraco.TheDashboard.Models.Frontend;
using Umbraco.Cms.Core.Cache;
using Umbraco.Extensions;

namespace Our.Umbraco.TheDashboard.Extensions;

public static class UserExtensions
{
        
    /// <summary>
    /// Tries to lookup the user's Gravatar to see if the endpoint can be reached, if so it returns the valid URL
    /// </summary>
    /// <returns>
    /// A list of 5 different sized avatar URLs
    /// </returns>
    public static UserAvatarFrontendModel GetUserAvatarUrls(int userId,string userEmail, string userAvatar, IAppCache cache, IHttpClientFactory httpClientFactory, byte[] hmacKey)
    {
        // If FIPS is required, never check the Gravatar service as it only supports MD5 hashing.  
        // Unfortunately, if the FIPS setting is enabled on Windows, using MD5 will throw an exception
        // and the website will not run.
        // Also, check if the user has explicitly removed all avatars including a Gravatar, this will be possible and the value will be "none"
        if (userAvatar == "none" || CryptoConfig.AllowOnlyFipsAlgorithms)
        {
            return new UserAvatarFrontendModel();
        }

        StringBuilder sb = new StringBuilder();

        if (userAvatar.IsNullOrWhiteSpace())
        {
            var gravatarHash = HashEmailForGravatar(userEmail);
            var gravatarUrl = "https://www.gravatar.com/avatar/" + gravatarHash + "?d=404";

            var gravatarAccess = cache.GetCacheItem<bool>("UserAvatar" + userId, () =>
            {
                var httpClient = httpClientFactory.CreateClient();
                // Test if we can reach this URL, will fail when there's network or firewall errors
                httpClient.Timeout = new TimeSpan(0,0,10);
                var fetchTask = httpClient.GetAsync(gravatarUrl);
                    
                try
                {
                    var res = fetchTask.Result;
                    return res.IsSuccessStatusCode;
                }
                catch (Exception)
                {
                    // There was an HTTP or other error, return an null instead
                    return false;
                }
            });

            if (gravatarAccess)
            {
                var gravatarSmallSize = gravatarUrl + "&s=30";

                sb.Append(gravatarSmallSize + " 1x, ");
                sb.AppendLine(gravatarUrl + "&s=60 2x, ");
                sb.AppendLine(gravatarUrl + "&s=90 3x");

                return new UserAvatarFrontendModel(gravatarSmallSize, sb.ToString());

            }

            return new UserAvatarFrontendModel();
        }

        var customAvatarUrl = "/media/" + userAvatar;
        var smallSize = GetAvatarCrop(customAvatarUrl, 30, hmacKey);

        sb.Append(smallSize + " 1x, ");
        sb.AppendLine(GetAvatarCrop(customAvatarUrl, 60, hmacKey) + " 2x, ");
        sb.AppendLine(GetAvatarCrop(customAvatarUrl, 90, hmacKey) + " 3x");

        return new UserAvatarFrontendModel(smallSize, sb.ToString());

    }

    internal static string GetAvatarCrop(string url, int dimensions, byte[] hmacKey)
    {
        var queryString = $"rmode=crop&width={dimensions}&height={dimensions}";
        var result = $"{url}?{queryString}";

        if (hmacKey.Length > 0)
        {
            var valueToSign = result.ToLowerInvariant();
            using var hmac = new HMACSHA256(hmacKey);
            var hash = hmac.ComputeHash(Encoding.ASCII.GetBytes(valueToSign));
            result += $"&hmac={Convert.ToHexString(hash).ToLowerInvariant()}";
        }

        return result;
    }

    internal static string HashEmailForGravatar(string email)
    {
        MD5 md5Hasher = MD5.Create();
        byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(email));

        StringBuilder sBuilder = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        return sBuilder.ToString();
    }


}
