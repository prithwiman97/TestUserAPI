using MongoDB.Driver;
using TestUserAPI.Models;
using TestUserAPI.Models.Requests;
using TestUserAPI.Models.Responses;

namespace TestUserAPI.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
    }

    public async Task<UserResponse?> GetByUsernameAsync(string username)
    {
        var user = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        return user == null ? null : MapToResponse(user);
    }

    public async Task<UserResponse?> GetByIdAsync(string id)
    {
        var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        return user == null ? null : MapToResponse(user);
    }

    public async Task<PagedResponse<UserResponse>> GetAllAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var totalCount = await _users.CountDocumentsAsync(_ => true);
        
        var users = await _users
            .Find(_ => true)
            .SortByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        var userResponses = users.Select(MapToResponse).ToList();

        return new PagedResponse<UserResponse>
        {
            Data = userResponses,
            Page = page,
            PageSize = pageSize,
            TotalCount = (int)totalCount
        };
    }

    public async Task<PagedResponse<UserResponse>> SearchByUsernameAsync(string username, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        
        // Case-insensitive partial match
        var filter = Builders<User>.Filter.Regex(
            u => u.Username,
            new MongoDB.Bson.BsonRegularExpression(username, "i")
        );

        var totalCount = await _users.CountDocumentsAsync(filter);
        
        var users = await _users
            .Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        var userResponses = users.Select(MapToResponse).ToList();

        return new PagedResponse<UserResponse>
        {
            Data = userResponses,
            Page = page,
            PageSize = pageSize,
            TotalCount = (int)totalCount
        };
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        // Check if username already exists (case-insensitive)
        var existingFilter = Builders<User>.Filter.Eq(u => u.Username, request.Username);
        var existingUser = await _users.Find(existingFilter).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with the given username already exists.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _users.InsertOneAsync(user);
        return MapToResponse(user);
    }

    public async Task<UserResponse?> UpdateByUsernameAsync(string username, UpdateUserRequest request)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        var update = Builders<User>.Update
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(request.Email))
            update = update.Set(u => u.Email, request.Email);
        
        if (!string.IsNullOrEmpty(request.FirstName))
            update = update.Set(u => u.FirstName, request.FirstName);
        
        if (!string.IsNullOrEmpty(request.LastName))
            update = update.Set(u => u.LastName, request.LastName);

        var options = new FindOneAndUpdateOptions<User>
        {
            ReturnDocument = ReturnDocument.After
        };

        var user = await _users.FindOneAndUpdateAsync(filter, update, options);
        return user == null ? null : MapToResponse(user);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}

