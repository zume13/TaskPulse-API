using Application.Abstractions.Base;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Primitives;

namespace Infrastructure.Persistence.Repositories;

public abstract class Repository<TAggregate> : IRepository<TAggregate> where TAggregate : AggregateRoot
{
    protected readonly TaskPulseDbContext _dbContext;

    protected Repository(TaskPulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TAggregate>().FindAsync([id], cancellationToken);
    }

    public async Task<List<TAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TAggregate>().AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TAggregate>().AddAsync(aggregate, cancellationToken);
    }

    public void Update(TAggregate aggregate)
    {
        _dbContext.Set<TAggregate>().Update(aggregate);
    }

    public void Remove(TAggregate aggregate)
    {
        _dbContext.Set<TAggregate>().Remove(aggregate);
    }
}