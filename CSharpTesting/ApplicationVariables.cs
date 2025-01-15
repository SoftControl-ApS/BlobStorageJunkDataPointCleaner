namespace CSharpTesting;

public class ApplicationVariables
{
    #region Private Fields

    public static string AzureBlobConnectionName { get; } = "sundatatest";

    public static string AzureBlobConnectionKey { get; } =
        "z1CzWXUvl3756GlrguOi/5Iwn7w+ILfAzlxJ/dOdz2UG+8w2vbKXT0rkBllvpCg0IDhAC6RmeEsL+AStzJa0Bw==";

    public static string AzureBlobConnectionString { get; } =
        $"DefaultEndpointsProtocol=https;AccountName={AzureBlobConnectionName};" +
        $"AccountKey={AzureBlobConnectionKey};" +
        $"EndpointSuffix=core.windows.net";

    #endregion
}