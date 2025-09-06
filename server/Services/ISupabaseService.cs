using Microsoft.AspNetCore.Mvc.RazorPages;
using Supabase.Postgrest.Models;

namespace server.Services
{
    public interface IEntityWithId
    {
        int Id { get; set; }
    }

    public interface ISupabaseService
    {
        Supabase.Client GetClient();
        Task<IEnumerable<T>> GetAllAsync<T>() where T : BaseModel, IEntityWithId, new();
        Task<T?> GetByIdAsync<T>(int id) where T : BaseModel, IEntityWithId, new();
        Task<T?> CreateAsync<T>(T entity) where T : BaseModel, IEntityWithId, new();
        Task<T?> UpdateAsync<T>(T entity) where T : BaseModel, IEntityWithId, new();
        Task<bool> DeleteAsync<T>(int id) where T : BaseModel, IEntityWithId, new();
    }
}
