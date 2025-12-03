using TestUserAPI.Models;
using TestUserAPI.Models.Requests;
using TestUserAPI.Models.Responses;

namespace TestUserAPI.Repositories;

public interface IUserRepository
{
    Task<UserResponse?> GetByIdAsync(string id);
    Task<UserResponse?> GetByUsernameAsync(string username);
    Task<PagedResponse<UserResponse>> GetAllAsync(int page, int pageSize);
    Task<PagedResponse<UserResponse>> SearchByUsernameAsync(string username, int page, int pageSize);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<UserResponse?> UpdateByUsernameAsync(string username, UpdateUserRequest request);
    Task<bool> DeleteAsync(string id);
}

