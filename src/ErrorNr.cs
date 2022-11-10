namespace Example;

[Flags]
public enum ErrorNr
{
    UserNotFound     = 1,
    NoUsersAtAddress = 2,

    ValidationError = 1024,
    InvalidZipCode = 2 | ValidationError,
    InvalidHouseNr = 3 | ValidationError,
}
