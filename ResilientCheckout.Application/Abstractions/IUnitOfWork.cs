using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Application.Abstractions
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        
    }
}
