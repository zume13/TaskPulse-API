using Domain.Shared.ValueObjects;
using SharedKernel.Primitives;
using Shared.ResultPattern;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace
{
    public class Workspace : AggregateRoot
    {
        private Workspace() { }
        private Workspace(Guid id, Name name) : base(id)
        {
            WorkspaceName = name;
            CreatedAtUtc = DateTime.UtcNow;
        }
        public Name WorkspaceName { get; private set; } = null!;
        public DateTime? CreatedAtUtc { get; private set; }
        private  readonly List<Guid> _memberIds = new List<Guid>();
        public IReadOnlyCollection<Guid> MemberIds => _memberIds;
        public static ResultT<Workspace> Create(string name)
        {
            var workspaceName = Name.Create(name);

            if (workspaceName.IsFailure)
                return workspaceName.Error; 

            return new Workspace(Guid.NewGuid(), workspaceName.value);
        }
    }
}
