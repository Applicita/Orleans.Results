namespace Example;

[Flags]
public enum ErrorCode
{
    UserNotFound     = 1,
    NoUsersAtAddress = 2,

    ValidationError = 1024,
    InvalidZipCode = 2 | ValidationError,
    InvalidHouseNr = 3 | ValidationError,
}
