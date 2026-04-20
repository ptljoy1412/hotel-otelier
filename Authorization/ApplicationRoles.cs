namespace OtelierBackend.Authorization;

public static class ApplicationRoles
{
    public const string Guest = "guest";
    public const string Staff = "staff";
    public const string Reception = "reception";
    public const string StaffOrReception = Staff + "," + Reception;
}
