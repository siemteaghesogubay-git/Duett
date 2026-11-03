namespace Duett.API.Exceptions;

public class ProfileAlreadyExistsException : Exception
{
    public ProfileAlreadyExistsException()
        : base("Du har redan en profil.")
    {
    }
}