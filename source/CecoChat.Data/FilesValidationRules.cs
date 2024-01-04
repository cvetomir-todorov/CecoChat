using FluentValidation;

namespace CecoChat.Data;

public static class FilesValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidBucketName<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            // e.g. a valid bucket is 'files-2024-01-18'
            .Matches("^([a-z0-9\\-]{4,64})$");
    }

    public static IRuleBuilderOptions<T, string> ValidPath<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            // a valid path is '1234/af58438d-6eb8-4d9d-990c-c674a82a1b08.png' where:
            // '1234' is the user ID
            // '/' is the path separator between the user ID and the file name with extension
            // 'af58438d-6eb8-4d9d-990c-c674a82a1b08' is the file name
            // '.png' is the file extension
            .Matches("^([0-9]+\\/[a-z0-9\\-]{20,40}\\.[a-z]{3,8})$");
    }
}
