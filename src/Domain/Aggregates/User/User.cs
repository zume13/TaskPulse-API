using Domain.Aggregates.User.Enum;
using Domain.Aggregates.User.ValueObjects;
using Domain.Shared.ValueObjects;
using SharedKernel.Primitives;
using Shared.ResultPattern;

namespace Domain.Aggregates.User
{
    public class User : AggregateRoot
    {
        private User() { }
        private User(Guid id, Email email, HashedPassword hashedPassword, Name firstName, Name lastName, Role role) : base(id)
        {
            Email = email;
            PasswordHash = hashedPassword;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            RegisteredAtUtc = DateTime.UtcNow;
        }
        public Email Email { get; private set; }
        public HashedPassword PasswordHash { get; private set; }
        public Name FirstName { get; private set; }
        public Name LastName { get; private set; }
        public DateTime RegisteredAtUtc { get; private set; }
        public Role Role { get; private set; }

        public ResultT<User> Create(string email, string hashedPassword, string firstName, string lastName, Role role)
        {
            var newEmail = Email.Create(email);
            var hashedPass = HashedPassword.Create(hashedPassword);
            var fName = Name.Create(firstName);
            var lName = Name.Create(lastName);

            if(newEmail.IsFailure)
                return newEmail.Error;  

            if(hashedPass.IsFailure)
                return hashedPass.Error;

            if(fName.IsFailure)
                return fName.Error;

            if(lName.IsFailure)
                return lName.Error;

            return new User(Guid.NewGuid(), newEmail.value, hashedPass.value, fName.value, lName.value, role);
        }
        // add methods for changing email, password, first name, last name, etc. with validation
    }
}
