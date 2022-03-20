namespace Oasis.EntityFrameworkCore.Mapper.Exceptions
{
    public class TypeNotProperlyRegisteredException : Exception
    {
        private readonly Type _type;

        public TypeNotProperlyRegisteredException(Type type)
        {
            _type = type;
        }

        public override string Message => $"Unexpected exception: Type {_type} is not property registered.";
    }
}
