using ResilientCheckout.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Infraestructure.Persistence
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _appDbContext;

        public EFUnitOfWork(AppDbContext appDbContext) => _appDbContext = appDbContext;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
