using Microsoft.Net.Http.Headers;

namespace Common.AspNet;

public static class MultipartUtility
{
    public static bool IsMultipartContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);
    }

    // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
    // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
    public static string GetMultipartBoundary(string? contentTypeValue, int lengthLimit = 70)
    {
        if (!MediaTypeHeaderValue.TryParse(contentTypeValue, out MediaTypeHeaderValue? contentType))
        {
            throw new InvalidDataException("Invalid content-type.");
        }

        string? boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary.");
        }
        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Exceeded the content-type boundary length limit {lengthLimit}.");
        }

        return boundary;
    }
}
