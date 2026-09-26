namespace NetflixClone.Application.Common.Exceptions;
// Translated only for the unique (ProfileId, MovieId) My List constraint.
public sealed class MyListAlreadyExistsException(string message, Exception innerException) : Exception(message, innerException);
